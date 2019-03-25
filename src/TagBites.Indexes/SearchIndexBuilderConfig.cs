using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Pl;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace TagBites.Indexes
{
    public class SearchIndexBuilderConfig
    {
        private Analyzer _analyzer;
        private string _analyzerName;
        private Type _analyzerType;

        internal LuceneVersion LuceneVersion => LuceneVersion.LUCENE_48;

        internal Analyzer AnalyzerInternal
        {
            get
            {
                if (_analyzer == null)
                    _analyzer = CreateAnalyzer(_analyzerType);

                return _analyzer;
            }
        }

        public Analyzer Analyzer
        {
            get => _analyzer;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _analyzer = value;
            }
        }
        public string AnalyzerName
        {
            get => _analyzerName;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var name = value + "Analyzer";

                foreach (var analyzerType in GetAnalyzersTypes())
                    if (string.Equals(value, analyzerType.Name, StringComparison.OrdinalIgnoreCase) || string.Equals(name, analyzerType.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _analyzerName = value;
                        _analyzerType = analyzerType;
                        return;
                    }

                throw new ArgumentException("Unknown analyzer name.");
            }
        }
        internal bool AsciiFolding { get; set; } = false;


        public IEnumerable<string> GetSupportedAnalyzersNames()
        {
            var suffix = typeof(Analyzer).Name;
            return GetAnalyzersTypes().Select(x => x.Name.EndsWith(suffix) ? x.Name.Substring(0, x.Name.Length - suffix.Length) : x.Name);
        }
        protected virtual IEnumerable<Type> GetAnalyzersTypes()
        {
            var assemblies = new[] {
                Assembly.GetAssembly(typeof(StandardAnalyzer)),
                Assembly.GetAssembly(typeof(Lucene.Net.Analysis.Pl.PolishAnalyzer))
            };
            return assemblies.SelectMany(x => x.GetTypes())
                .Where(x => typeof(Analyzer).IsAssignableFrom(x) && !x.IsAbstract && x.IsPublic);
        }

        protected virtual Analyzer CreateAnalyzer(Type analyzerType)
        {
            var analyzer = analyzerType == null
                ? new StandardAnalyzer(LuceneVersion)
                : (Analyzer)Activator.CreateInstance(analyzerType, LuceneVersion);

            if (AsciiFolding)
                analyzer = new AsciiFoldingAnalyzer(analyzer);

            return analyzer;
        }

        private class AsciiFoldingAnalyzer : Analyzer
        {
            private readonly Analyzer _analyzer;

            public AsciiFoldingAnalyzer(Analyzer analyzer)
                : base(analyzer.Strategy)
            {
                _analyzer = analyzer;
            }


            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                // ReSharper disable once PossibleNullReferenceException
                var components = (TokenStreamComponents)_analyzer.GetType().GetMethod(nameof(CreateComponents), BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_analyzer, new object[] { fieldName, reader });

                var tokenizer = components.Tokenizer;
                var stream = components.TokenStream;
                stream = new ASCIIFoldingFilter(stream);

                return new TokenStreamComponents(tokenizer, stream);
            }
        }
    }
}
