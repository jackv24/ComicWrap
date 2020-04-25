using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;

namespace ComicWrap.Systems
{
    public class ComicUpdater : SingletonBase<ComicUpdater>
    {
        public ComicUpdater()
        {
            database = ComicDatabase.Instance;
            pageLoader = new PageLoader();

            importingComics = new List<ComicData>();
        }

        public ComicUpdater(ComicDatabase database, IPageLoader pageLoader)
        {
            this.database = database;
            this.pageLoader = pageLoader;

            importingComics = new List<ComicData>();
        }

        public event Action<ComicData> ImportComicBegun;
        public event Action<ComicData> ImportComicProgressed;
        public event Action<ComicData> ImportComicFinished;

        private readonly ComicDatabase database;
        private readonly IPageLoader pageLoader;

        private List<ComicData> importingComics;
        public IEnumerable<ComicData> ImportingComics { get => importingComics; }

        public static bool IsUrlValid(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (UriFormatException)
            {
                return false;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return true;
        }

        public async Task<ComicData> ImportComic(string archiveUrl, string currentPageUrl)
        {
            var comic = new ComicData
            {
                Name = "!Importing Comic!",
                ArchiveUrl = archiveUrl,
                CurrentPageUrl = currentPageUrl
            };

            importingComics.Add(comic);
            ImportComicBegun?.Invoke(comic);

            var document = await pageLoader.OpenDocument(archiveUrl);

            // TODO: Process title to remove " - Archive", etc.
            comic.Name = document.Title;

            ImportComicProgressed?.Invoke(comic);

            await UpdateComic(
                comic,
                markReadUpToUrl: currentPageUrl,
                markNewPagesAsNew: false);

            importingComics.Remove(comic);
            ImportComicFinished?.Invoke(comic);

            return comic;
        }

        public async Task<IEnumerable<ComicPageData>> UpdateComic(
            ComicData comic,
            string markReadUpToUrl = null,
            bool markNewPagesAsNew = true,
            CancellationToken cancelToken = default)
        {
            // Load comic archive page
            cancelToken.ThrowIfCancellationRequested();
            var document = await pageLoader.OpenDocument(comic.ArchiveUrl);

            var newPages = DiscoverPages(document, comic.CurrentPageUrl);
            cancelToken.ThrowIfCancellationRequested();
            var oldPages = comic.Pages.ToList();
            cancelToken.ThrowIfCancellationRequested();

            bool doMarkReadUpTo = !string.IsNullOrEmpty(markReadUpToUrl);
            bool reachedReadPage = false;
            bool anyNewPages = false;

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
                {
                    newPage.IsNew = true;
                    anyNewPages = true;
                }
            }

            // NOTE: After this point we can no longer cancel as database is being written to
            cancelToken.ThrowIfCancellationRequested();

            // Run database operations as one transaction to prevent issues
            database.Write(realm =>
            {
                comic.Name = document.Title;

                // Record date if any new pages were added
                if (anyNewPages)
                    comic.LastUpdatedDate = DateTimeOffset.UtcNow;

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

            // Report that comic has updated so UI can refresh, etc.
            comic.ReportUpdated();

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

        public async Task<string> GetComicImageUrl(ComicPageData page, CancellationToken cancelToken = default)
        {
            // TODO: Expand to work with more sites

            if (cancelToken.IsCancellationRequested)
                return null;

            var document = await pageLoader.OpenDocument(page.Url);

            return document.GetElementById("cc-comic")?.GetAttribute("src");
        }
    }
}
