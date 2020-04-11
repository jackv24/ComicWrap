using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;

using ComicWrap.Systems;

namespace ComicWrap.Systems
{
    public static class ComicUpdater
    {
        public static bool IsUrlValid(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
            }
            catch (UriFormatException)
            {
                return false;
            }

            return true;
        }

        public static IBrowsingContext GetBrowsingContext()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            return context;
        }

        public static async Task ImportComic(string archiveUrl, string currentPageUrl)
        {
            var document = await GetBrowsingContext().OpenAsync(archiveUrl);
            
            var comic = new ComicData
            {
                Name = document.Title,
                ArchiveUrl = archiveUrl,
                CurrentPageUrl = currentPageUrl
            };

            await UpdateComic(
                comic,
                markReadUpToUrl: currentPageUrl,
                markNewPagesAsNew: false);
        }

        public static async Task<IEnumerable<ComicPageData>> UpdateComic(
            ComicData comic,
            string markReadUpToUrl = null,
            bool markNewPagesAsNew = false,
            CancellationToken cancelToken = default)
        {
            // Load comic archive page
            cancelToken.ThrowIfCancellationRequested();
            var document = await GetBrowsingContext().OpenAsync(comic.ArchiveUrl);

            var newPages = DiscoverPages(document, comic.CurrentPageUrl);
            cancelToken.ThrowIfCancellationRequested();
            var oldPages = comic.Pages.ToList();
            cancelToken.ThrowIfCancellationRequested();

            bool doMarkReadUpTo = !string.IsNullOrEmpty(markReadUpToUrl);
            bool reachedReadPage = false;

            // New page data is bare, so fill out missing data
            // Loop backwards so we can mark previously read pages
            for (int i = newPages.Count - 1; i >= 0; i--)
            {
                ComicPageData newPage = newPages[i];

                // Mark all pages before current page as read
                if (doMarkReadUpTo && newPage.Url == markReadUpToUrl)
                    reachedReadPage = true;

                if (reachedReadPage)
                {
                    newPage.IsRead = true;

                    // Page already marked read, no need to check old pages
                    continue;
                }

                bool foundOldPage = false;
                foreach (var oldPage in oldPages)
                {
                    // Transfer persistent data to new page data
                    if (newPage.Url == oldPage.Url)
                    {
                        foundOldPage = true;
                        newPage.IsRead = oldPage.IsRead;
                        newPage.IsNew = oldPage.IsNew;
                    }
                }

                if (!foundOldPage && markNewPagesAsNew)
                    newPage.IsNew = true;
            }

            // NOTE: After this point we can no longer cancel as database is being written to
            cancelToken.ThrowIfCancellationRequested();

            // Run database operations as one transaction to prevent issues
            ComicDatabase.Instance.Write(realm =>
            {
                comic.Name = document.Title;
                realm.Add(comic, update: true);

                // Delete any existing pages that aren't in the new pages
                var deletePages = oldPages.Where(page => !newPages.Contains(page));
                foreach (var page in deletePages)
                    realm.Remove(page);

                // Add pages to database after Comic.Id is set so their ComicId is correct
                foreach (var page in newPages)
                {
                    page.Comic = comic;
                    realm.Add(page);
                }
            });

            return newPages;
        }

        public static List<ComicPageData> DiscoverPages(IDocument document, string knownPageUrl)
        {
            // TODO: Expand to work with more sites

            var elements = document.GetElementsByName("comic");
            if (elements.Length == 0)
                return new List<ComicPageData>();
            else
            {
                var options = elements[0].Children
                    .Select(element => new { Text = element.TextContent, Nav = element.GetAttribute("value") })
                    // Ignore empty values (usually 1 empty value is the "Select..." prompt
                    .Where(a => !string.IsNullOrEmpty(a.Nav));

                // Extract base url using know page url
                string baseUrl = null;
                foreach (var opt in options)
                {
                    if (knownPageUrl.EndsWith(opt.Nav))
                    {
                        baseUrl = knownPageUrl.Substring(0, knownPageUrl.Length - opt.Nav.Length - 1);
                        break;
                    }
                }

                // base url couldn't be extracted
                if (baseUrl == null)
                    return new List<ComicPageData>();

                return options
                    .Select(a => new ComicPageData
                    {
                        Name = a.Text,
                        Url = $"{baseUrl}/{a.Nav}"
                    })
                    .ToList();
            }
        }
    }
}
