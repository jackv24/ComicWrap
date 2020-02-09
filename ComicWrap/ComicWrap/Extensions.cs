using System;
using System.Collections.Generic;
using System.Text;

namespace ComicWrap
{
    public static class Extensions
    {
        public static string ToRelative(this Uri uri)
        {
            return uri.IsAbsoluteUri ? uri.PathAndQuery : uri.OriginalString;
        }
    }
}
