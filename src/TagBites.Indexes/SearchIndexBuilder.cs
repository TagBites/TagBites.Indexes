using System;
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;

namespace TagBites.Indexes
{
    public class SearchIndexBuilder : IDisposable
    {
        private SearchIndexContainer _container;
        private IndexWriter _writer;

        public SearchIndexBuilder(string fileName)
            : this(fileName, null)
        { }
        public SearchIndexBuilder(string fileName, SearchIndexBuilderConfig config)
            : this(File.Create(fileName), true, config)
        { }
        public SearchIndexBuilder(Stream stream, bool ownStream, SearchIndexBuilderConfig config)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (config == null)
                config = new SearchIndexBuilderConfig();

            var container = new SearchIndexContainer(stream, ownStream);
            var writerConfig = new IndexWriterConfig(config.LuceneVersion, config.AnalyzerInternal);
            var writer = new IndexWriter(container, writerConfig);

            _container = container;
            _writer = writer;
        }


        public void Index(string url, string title, string content)
        {
            var doc = new Document();
            doc.AddStringField("url", url, Field.Store.YES);
            doc.AddTextField("title", title, Field.Store.YES);
            doc.AddTextField("content", content, Field.Store.YES);

            _writer.AddDocument(doc);
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Commit();
                _writer.Dispose();
                _writer = null;
            }

            if (_container != null)
            {
                _container.Dispose();
                _container = null;
            }
        }
    }
}
