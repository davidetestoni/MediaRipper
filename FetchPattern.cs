using System;
using System.Collections.Generic;
using System.Text;

namespace MediaRipper
{
    public struct FetchPattern
    {
        public string pattern;
        public string nameFormat;
        public string uriFormat;

        public FetchPattern(string pattern, string nameFormat = "[1]", string uriFormat = "[0]")
        {
            this.pattern = pattern;
            this.nameFormat = nameFormat;
            this.uriFormat = uriFormat;
        }
    }
}
