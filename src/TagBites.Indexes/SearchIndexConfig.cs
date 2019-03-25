using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TagBites.Indexes
{
    public class SearchIndexConfig : SearchIndexBuilderConfig
    {
        private CultureInfo _locale;

        public virtual CultureInfo Locale
        {
            get => _locale ?? CultureInfo.CurrentCulture;
            set => _locale = value;
        }

        public int ResultFragments { get; set; } = 2;
        public int FragmentLength { get; set; } = 128;
        public string FragmentSeparator { get; set; } = "...";

        public bool TryFullWildcardSearchOnTitle { get; set; } = true;
        public bool TryContainsSearch { get; set; } = true;
    }
}
