using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaRipper
{
    public struct FetchedItem
    {
        public string uri;
        public string name;
        public string path;

        public FetchedItem(string uri, string name, string basePath)
        {
            this.uri = uri;
            this.name = name;
            this.path = Path.Combine(basePath, Utils.MakeValidFileName(name));
        }
    }
}
