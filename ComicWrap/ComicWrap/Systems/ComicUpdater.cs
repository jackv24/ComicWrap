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

        public async Task<ComicData> ImportComic(string pageUrl)
        {
            var comic = new ComicData
            {
                Name = pageUrl,
                // Page might not be the archive page, this will be updated when the archive page is found
                ArchiveUrl = pageUrl
            };

            importingComics.Add(comic);
            ImportComicBegun?.Invoke(comic);

            IEnumerable<ComicPageData> pages = await UpdateComic(comic);

            importingComics.Remove(comic);
            ImportComicFinished?.Invoke(comic);

            // If comic was never added to database it failed to import
            if (!comic.IsManaged)
                return null;

            return comic;
        }

        public async Task<IEnumerable<ComicPageData>> UpdateComic(
            ComicData comicData,
            CancellationToken cancelToken = default)
        {
            bool isComicImporting = !comicData.IsManaged;

            // Mark up to current page if comic is being imported
            string readPageUrl = isComicImporting ? comicData.ArchiveUrl : null;

            cancelToken.ThrowIfCancellationRequested();

            List<ComicPageData> tempPages;
            if (isComicImporting)
            {
                string setArchiveUrl = null;

                tempPages = await DiscoverPages(
                    comicData.ArchiveUrl,
                    onFoundArchivePage: (document) =>
                    {
                        setArchiveUrl = document.Url;
                        comicData.Name = document.Title;
                    });

                // Update archive url for for quicker updating next time if we didn't start on the archive page
                if (!string.IsNullOrEmpty(setArchiveUrl))
                    comicData.ArchiveUrl = setArchiveUrl;
            }
            else
            {
                tempPages = await DiscoverPages(comicData.ArchiveUrl);
            }

            cancelToken.ThrowIfCancellationRequested();

            // Cancel early if no pages were found
            if (tempPages == null || tempPages.Count == 0)
                return null;

            if (isComicImporting)
            {
                bool reachedReadPage = false;

                // New page data is bare, so fill out missing data
                // Loop backwards so we can mark previously read pages
                for (int i = tempPages.Count - 1; i >= 0; i--)
                {
                    ComicPageData tempPage = tempPages[i];

                    // Mark all pages before current page as read
                    if (tempPage.Url == readPageUrl)
                        reachedReadPage = true;

                    if (reachedReadPage)
                    {
                        // We can edit tempPage fields outside of a write transaction since it hasn't been added to a Realm yet
                        tempPage.IsRead = true;
                    }
                }
            }
            else
            {

                foreach (ComicPageData tempPage in tempPages)
                {
                    // We can edit tempPage fields outside of a write transaction since it hasn't been added to a Realm yet
                    tempPage.IsNew = true;
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

                // Make sure comic is in database first
                realm.Add(comic, update: true);

                var existingPages = comic.Pages.ToList();

                // Delete any existing pages that aren't in the new pages
                var deletePages = existingPages.Where(existingPage =>
                    !tempPages.Any(tempPage => tempPage.Url == existingPage.Url));
                foreach (var page in deletePages)
                    realm.Remove(page);

                bool anyNewPages = false;

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

                        anyNewPages = true;
                    }
                }

                // Record date if any new pages were added
                if (anyNewPages)
                    comic.LastUpdatedDate = DateTimeOffset.UtcNow;

                // Throwing at end of write transaction should cancel transaction
                cancelToken.ThrowIfCancellationRequested();
            });
            cancelToken.ThrowIfCancellationRequested();

            // Report that comic has updated so UI can refresh, etc.
            comicData.ReportUpdated();

            return comicData.Pages;
        }

        public async Task<List<ComicPageData>> DiscoverPages(string pageUrl, Action<IDocument> onFoundArchivePage = null)
        {
            if (!IsUrlValid(pageUrl))
                return null;

            IDocument document = await pageLoader.OpenDocument(pageUrl);
            List<ComicPageData> pages = FindPagesFromArchiveList(document);
            if (pages != null)
            {
                onFoundArchivePage?.Invoke(document);
                return pages;
            }

            string archivePageUrl = FindArchivePageLink(document);
            if (!string.IsNullOrEmpty(archivePageUrl) && archivePageUrl != pageUrl)
            {
                archivePageUrl = GetAbsoluteUri(pageUrl, archivePageUrl);
                return await DiscoverPages(archivePageUrl, onFoundArchivePage);
            }

            return null;
        }

        private List<ComicPageData> FindPagesFromArchiveList(IDocument document)
        {
            // TODO: Expand to work with more websites

            var elements = document.GetElementsByName("comic");
            if (elements.Length == 0)
                return null;
            else
            {
                var options = elements[0].Children
                    .Select(element => new { Text = element.TextContent, Nav = element.GetAttribute("value") })
                    // Ignore empty values (usually 1 empty value is the "Select..." prompt
                    .Where(a => !string.IsNullOrEmpty(a.Nav));

                string pageUrl = document.Url;

                return options
                    .Select(a => new ComicPageData
                    {
                        Name = a.Text,
                        Url = GetAbsoluteUri(pageUrl, a.Nav)
                    })
                    .ToList();
            }
        }

        private string FindArchivePageLink(IDocument document)
        {
            // TODO: Expand to work with more websites

            // Attempt to find archive link in element tagged "archive"
            string archiveLink = document.GetElementById("archive")?.GetAttribute("href");
            if (!string.IsNullOrEmpty(archiveLink))
                return archiveLink;

            // Attempt to find elements whose text content is "archive"
            archiveLink = document.GetElementsByTagName("a")
                .Where(a => a.TextContent.ToUpperInvariant().Contains("ARCHIVE"))
                .FirstOrDefault()?.GetAttribute("href");
            if (!string.IsNullOrEmpty(archiveLink))
                return archiveLink;

            // All attempts failed
            return null;
        }

        private string GetAbsoluteUri(string sourceUri, string link)
        {
            // Link may be relative
            if (IsUrlValid(link))
                return link;

            // Search string from back to front for / character
            string relativeBaseUrl = null;
            for (int i = sourceUri.Length - 1; i >= 0; i--)
            {
                if (sourceUri[i] == '/')
                {
                    relativeBaseUrl = sourceUri.Substring(0, i);
                    break;
                }
            }

            // Convert page url to absolute
            if (!string.IsNullOrEmpty(relativeBaseUrl))
            {
                if (link[0] == '/')
                    link = relativeBaseUrl + link;
                else
                    link = $"{relativeBaseUrl}/{link}";
            }

            return link;
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
