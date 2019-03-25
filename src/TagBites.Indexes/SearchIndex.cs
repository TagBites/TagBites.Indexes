using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Util;
using FileMode = System.IO.FileMode;

namespace TagBites.Indexes
{
    public class SearchIndex : IDisposable
    {
        private readonly string[] FieldsAll = { "title", "content" };
        private readonly string[] FieldsTitle = { "title" };

        private SearchIndexContainer _container;
        private DirectoryReader _reader;

        private Analyzer Analyzer { get; }
        private IndexSearcher Searcher { get; }
        private SearchIndexConfig Config { get; }

        public SearchIndex(string fileName)
            : this(fileName, null)
        { }
        public SearchIndex(string fileName, SearchIndexConfig config)
            : this(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read), true, config)
        { }
        public SearchIndex(Action<SearchIndexBuilder> inMemoryIndexBuilder, SearchIndexConfig config)
            : this(CreateInMemoryIndex(inMemoryIndexBuilder, config), true, config)
        { }
        public SearchIndex(Stream stream, bool ownStream, SearchIndexConfig config)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            Config = config;

            if (config == null)
                config = new SearchIndexConfig();

            var container = new SearchIndexContainer(stream, ownStream);
            var reader = DirectoryReader.Open(container);

            _container = container;
            _reader = reader;

            Searcher = new IndexSearcher(reader);
            Analyzer = config.AnalyzerInternal;
        }


        public SearchResult Search(string query, int topResultCount = 10)
        {
            if (string.IsNullOrEmpty(query))
                return SearchResult.Empty;

            var result = SearchCore(query, FieldsAll, topResultCount);

            if (result.Total < topResultCount && (Config.TryContainsSearch || Config.TryFullWildcardSearchOnTitle))
            {
                if (query.Contains('"')
                    || query.Contains('*')
                    || query.Contains('?')
                    || query.Contains('~')
                    || query.Contains(" OR ")
                    || query.Contains(" AND "))
                    return result;

                if (Config.TryContainsSearch)
                {
                    var q = query + "*";
                    result = result.Combine(SearchCore(q, FieldsAll, topResultCount));
                }

                if (result.Total < topResultCount && Config.TryContainsSearch)
                {
                    var q = "*" + query + "*";
                    result = result.Combine(SearchCore(q, FieldsAll, topResultCount));
                }

                if (result.Total < topResultCount && Config.TryFullWildcardSearchOnTitle)
                {
                    var words = Split(query);
                    if (words.Length == 1)
                    {
                        var q = "*" + string.Join("*", words[0].ToArray()) + "*";
                        result = result.Combine(SearchCore(q, FieldsTitle, topResultCount));
                    }
                }
            }

            return result;
        }
        private SearchResult SearchCore(string query, string[] fields, int topResultCount)
        {
            // Search
            var parser = new MultiFieldQueryParser(Config.LuceneVersion, fields, Analyzer);
            parser.AllowLeadingWildcard = true;
            parser.DefaultOperator = Operator.AND;
            parser.Locale = Config.Locale;
            parser.AnalyzeRangeTerms = true;

            var q = parser.Parse(query);

            var results = Searcher.Search(q, topResultCount);
            var hits = results.ScoreDocs;

            if (results.TotalHits == 0)
                return SearchResult.Empty;

            // Format
            var items = new List<SearchResultItem>();

            var scorer = new QueryScorer(q);
            var formatter = new SimpleHTMLFormatter("<mark>", "</mark>");
            var highlighter = new Highlighter(formatter, scorer) { TextFragmenter = new SimpleFragmenter(Config.FragmentLength) };

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < hits.Length; i++)
            {
                var doc = Searcher.Doc(hits[i].Doc);
                var url = doc.Get("url");
                var title = doc.Get("title");
                var content = doc.Get("content");

                using (var stream = Analyzer.GetTokenStream(url, new StringReader(content)))
                {
                    var preview = highlighter.GetBestFragments(stream, content, Config.ResultFragments, Config.FragmentSeparator);

                    var item = new SearchResultItem(url, ToWbrWrapName(title), preview);
                    items.Add(item);
                }
            }

            return new SearchResult(results.TotalHits, items);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_container != null)
            {
                _container.Dispose();
                _container = null;
            }
        }

        private static string ToWbrWrapName(string name)
        {
            var sb = new StringBuilder(name.Length * 2);

            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                    sb.Append("<wbr>");

                sb.Append(name[i]);
            }

            return sb.ToString();
        }
        private static string[] Split(string query)
        {
            return query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
        private static Stream CreateInMemoryIndex(Action<SearchIndexBuilder> inMemoryIndexBuilder, SearchIndexBuilderConfig config)
        {
            var ms = new MemoryStream();

            using (var index = new SearchIndexBuilder(ms, false, config))
                inMemoryIndexBuilder(index);

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
