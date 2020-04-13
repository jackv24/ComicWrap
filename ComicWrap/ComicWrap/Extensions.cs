using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ComicWrap
{
    public static class Extensions
    {
        public static string ToRelative(this Uri uri)
        {
            return uri.IsAbsoluteUri ? uri.PathAndQuery : uri.OriginalString;
        }

        public static void CancelAndDispose(this CancellationTokenSource tokenSource)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }
}
