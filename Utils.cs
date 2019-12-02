using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaRipper
{
    public static class Utils
    {
        public static string MakeValidFileName(string name)
        {
            StringBuilder sb = new StringBuilder(name);
            foreach(var c in Path.GetInvalidFileNameChars())
            {
                sb.Replace(c.ToString(), "");
            }

            return sb.ToString();
        }
    }
}
