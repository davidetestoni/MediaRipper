using System;
using System.Collections.Generic;
using System.Text;

namespace MediaRipper
{
    /// <summary>
    /// A regex pattern and output format for parsing resources from a webpage.
    /// </summary>
    public struct FetchPattern
    {
        /// <summary>The regex pattern.</summary>
        public string pattern;

        /// <summary>The format of the output name, where [i] will be replaced with the i-th captured group.</summary>
        public string nameFormat;

        /// <summary>The format of the output uri, where [i] will be replaced with the i-th captured group.</summary>
        public string uriFormat;

        /// <summary>
        /// Initializes a fetch pattern.
        /// </summary>
        /// <param name="pattern">The regex pattern</param>
        /// <param name="nameFormat">The format of the output name, where [i] will be replaced with the i-th captured group</param>
        /// <param name="uriFormat">The format of the output uri, where [i] will be replaced with the i-th captured group</param>
        public FetchPattern(string pattern, string nameFormat = "[1]", string uriFormat = "[0]")
        {
            this.pattern = pattern;
            this.nameFormat = nameFormat;
            this.uriFormat = uriFormat;
        }
    }
}
