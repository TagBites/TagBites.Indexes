using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TagBites.Indexes.Tests
{
    [TestClass]
    public class SearchIndexUnitTest
    {
        [TestMethod]
        public void SearchTest()
        {
            var config = new SearchIndexConfig();

            using (var searchIndex = new SearchIndex(CreateDefaultSearchIndex, config))
            {
                var result = searchIndex.Search("file content");
                Assert.AreEqual(2, result.Total);

                result = searchIndex.Search("*ile");
                Assert.AreEqual(2, result.Total);

                result = searchIndex.Search("ile");
                Assert.AreEqual(2, result.Total);

                result = searchIndex.Search("tte");
                Assert.AreEqual(2, result.Total);

                result = searchIndex.Search("reft");
                Assert.AreEqual(1, result.Total);

                result = searchIndex.Search("stile");
                Assert.AreEqual(1, result.Total);
            }
        }

        [TestMethod]
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
                Assert.AreEqual(1, result.Total);

                result = searchIndex.Search("stol");
                Assert.AreEqual(1, result.Total);
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
