using Xunit;

namespace TagBites.Indexes.Tests
{
    public class SearchIndexUnitTest
    {
        [Fact]
        public void SearchTest()
        {
            var config = new SearchIndexConfig();

            using (var searchIndex = new SearchIndex(CreateDefaultSearchIndex, config))
            {
                var result = searchIndex.Search("file content");
                Assert.Equal(2, result.Total);

                result = searchIndex.Search("*ile");
                Assert.Equal(2, result.Total);

                result = searchIndex.Search("ile");
                Assert.Equal(2, result.Total);

                result = searchIndex.Search("tte");
                Assert.Equal(2, result.Total);

                result = searchIndex.Search("reft");
                Assert.Equal(1, result.Total);

                result = searchIndex.Search("stile");
                Assert.Equal(1, result.Total);
            }
        }

        [Fact]
        public void AsciiFoldingTestTest()
        {
            var config = new SearchIndexConfig()
            {
                AnalyzerName = "polish",
                //AsciiFolding = true
            };

            using (var searchIndex = new SearchIndex(CreateSearchIndex, config))
            {
                var result = searchIndex.Search("stół");
                Assert.Equal(1, result.Total);

                result = searchIndex.Search("stol");
                Assert.Equal(1, result.Total);
            }

            void CreateSearchIndex(SearchIndexBuilder searchIndex)
            {
                searchIndex.Index("words", "Words", "Stół z powyłamywanymi nogami.");
            }
        }

        private static void CreateDefaultSearchIndex(SearchIndexBuilder searchIndex)
        {
            searchIndex.Index("/docs/readme.md", "ReadmeFileTitle - Readme File Title", "This is an example of readme text file content.");
            searchIndex.Index("/docs/start.md", "StartFileTitle - Start File Title", "This is an example of start text file content.");
        }
    }
}
