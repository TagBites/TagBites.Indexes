using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
