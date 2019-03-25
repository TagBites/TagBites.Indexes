using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagBites.Indexes
{
    public class SearchResultItem
    {
        public string Url { get; }
        public string Title { get; }
        public string Preview { get; }

        public SearchResultItem(string url, string title, string preview)
        {
            Url = url;
            Title = title;
            Preview = preview;
        }
    }
}
