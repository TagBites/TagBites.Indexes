using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Support;

namespace TagBites.Indexes
{
    public class SearchResult
    {
        public static readonly SearchResult Empty = new SearchResult(0, new SearchResultItem[0]);

        public int Total { get; }
        public IList<SearchResultItem> Items { get; }

        internal SearchResult(int total, IList<SearchResultItem> items)
        {
            Total = total;
            Items = items;
        }


        internal SearchResult Combine(SearchResult result)
        {
            if (result.Total == 0)
                return this;
            if (Total == 0)
                return result;

            return new SearchResult(Math.Max(Total, result.Total), Items.Concat(result.Items).Distinct(new ItemComparer()).ToList());
        }

        private class ItemComparer : IEqualityComparer<SearchResultItem>
        {
            public bool Equals(SearchResultItem x, SearchResultItem y) => Equals(x?.Url, y?.Url);
            public int GetHashCode(SearchResultItem obj) => obj.Url?.GetHashCode() ?? 0;
        }
    }
}
