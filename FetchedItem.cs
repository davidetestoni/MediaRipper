using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaRipper
{
    /// <summary>
    /// A remote resource information that was fetched from a webpage.
    /// </summary>
    public struct FetchedItem
    {
        /// <summary>The remote resource's uri (can be relative or absolute).</summary>
        public string uri;

        /// <summary>The resource's name for subfolder creation (will be made valid first).</summary>
        public string name;

        /// <summary>The resource's base folder on disk.</summary>
        public string path;

        /// <summary>
        /// Initializes a fetched item.
        /// </summary>
        /// <param name="uri">The remote resource's uri (can be relative or absolute)</param>
        /// <param name="name">The resource's name for subfolder creation (will be made valid first)</param>
        /// <param name="basePath">The resource's base folder on disk</param>
        public FetchedItem(string uri, string name, string basePath)
        {
            this.uri = uri;
            this.name = name;
            this.path = Path.Combine(basePath, Utils.MakeValidFileName(name));
        }
    }
}
