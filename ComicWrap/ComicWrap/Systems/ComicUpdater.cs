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
                ArchiveUrl = archiveUrl,
                CurrentPageUrl = currentPageUrl
            };

            importingComics.Add(comic);
            ImportComicBegun?.Invoke(comic);

            var document = await pageLoader.OpenDocument(archiveUrl);

            // TODO: Process title to remove " - Archive", etc.
            comic.Name = document.Title;

            ImportComicProgressed?.Invoke(comic);

            IEnumerable<ComicPageData> pages = await UpdateComic(
                comic,
                markReadUpToUrl: currentPageUrl,
                markNewPagesAsNew: false);

            importingComics.Remove(comic);
            ImportComicFinished?.Invoke(comic);

            // If comic was never added to database it failed to import
            if (!comic.IsManaged)
                return null;

            return comic;
        }

        public async Task<IEnumerable<ComicPageData>> UpdateComic(
            ComicData comicData,
            string markReadUpToUrl = null,
            bool markNewPagesAsNew = true,
            CancellationToken cancelToken = default)
        {
            // Load comic archive page
            cancelToken.ThrowIfCancellationRequested();
            var document = await pageLoader.OpenDocument(comicData.ArchiveUrl);
            cancelToken.ThrowIfCancellationRequested();

            var tempPages = DiscoverPages(document, comicData.CurrentPageUrl);

            // Cancel early if no pages were found
            if (tempPages.Count == 0)
                return null;

            bool doMarkReadUpTo = !string.IsNullOrEmpty(markReadUpToUrl);
            bool reachedReadPage = false;
            bool anyNewPages = false;

            // New page data is bare, so fill out missing data
            // Loop backwards so we can mark previously read pages
            for (int i = tempPages.Count - 1; i >= 0; i--)
            {
                ComicPageData tempPage = tempPages[i];

                // Mark all pages before current page as read
                if (doMarkReadUpTo && tempPage.Url == markReadUpToUrl)
                    reachedReadPage = true;

                if (reachedReadPage)
                {
                    // We can edit tempPage fields outside of a write transaction since it hasn't been added to a Realm yet
                    tempPage.IsRead = true;
                    continue;
                }

                if (markNewPagesAsNew)
                {
                    tempPage.IsNew = true;
                    anyNewPages = true;
                }
            }

            if (!comicData.IsManaged)
                database.AddComic(comicData);

            string comicId = comicData.Id;

            // Run expensive operations on background thread
            cancelToken.ThrowIfCancellationRequested();
            await database.WriteAsync(realm =>
            {
                // We can't pass realm objects between threads, so we need to find comic by ID again
                ComicData comic = realm.All<ComicData>()
                    .First(c => c.Id == comicId);

                comic.Name = document.Title;

                // Record date if any new pages were added
                if (anyNewPages)
                    comic.LastUpdatedDate = DateTimeOffset.UtcNow;

                // Make sure comic is in database first
                realm.Add(comic, update: true);

                var existingPages = comic.Pages.ToList();

                // Delete any existing pages that aren't in the new pages
                var deletePages = existingPages.Where(existingPage =>
                    !tempPages.Any(tempPage => tempPage.Url == existingPage.Url));
                foreach (var page in deletePages)
                    realm.Remove(page);

                foreach (var page in tempPages)
                {
                    // We need to update existing pages when we can instead of replacing them all with new ones
                    var existingPage = existingPages.FirstOrDefault(p => p.Url == page.Url);
                    if (existingPage != null)
                    {
                        // Update existing pages
                        existingPage.Name = page.Name;
                        // URL, IsRead, etc. still the same
                    }
                    else
                    {
                        // Add new pages
                        page.Comic = comic;
                        realm.Add(page);
                    }
                }

                // Throwing at end of write transaction should cancel transaction
                cancelToken.ThrowIfCancellationRequested();
            });
            cancelToken.ThrowIfCancellationRequested();

            // Report that comic has updated so UI can refresh, etc.
            comicData.ReportUpdated();

            return comicData.Pages;
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
