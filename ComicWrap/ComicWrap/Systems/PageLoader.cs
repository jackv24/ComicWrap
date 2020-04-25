using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;

namespace ComicWrap.Systems
{
    public interface IPageLoader
    {
        Task<IDocument> OpenDocument(string pageUrl);
    }

    public class PageLoader : IPageLoader
    {
        public Task<IDocument> OpenDocument(string pageUrl)
        {
            return GetBrowsingContext().OpenAsync(pageUrl);
        }

        public static IBrowsingContext GetBrowsingContext()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            return context;
        }
    }
}
