using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;
using AsyncAwaitBestPractices;
using Acr.UserDialogs;

using Res = ComicWrap.Resources.AppResources;

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

        public void StartImportComic(string pageUrl)
        {
            // TODO: Run in background as service (with notification and everything)
            
            // Alert popup should display even if this page isn't visible anymore
            ImportComic(pageUrl).SafeFireAndForget();
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

            await UpdateComic(comic);
            
            // If comic was never added to database it failed to import
            if (!comic.IsManaged)
            {
                bool continueImport = await UserDialogs.Instance.ConfirmAsync(
                    Res.AddComic_Error_ImportFailed,
                    Res.Alert_Error_Title,
                    Res.Alert_Generic_Confirm,
                    Res.Alert_Generic_Cancel);

                // If desired, add comic to database anyway
                if (continueImport)
                    database.AddComic(comic);
            }

            // First item of history so navigation can be started even if no pages found
            comic.RecordHistory(comic.ArchiveUrl);
            
            importingComics.Remove(comic);
            ImportComicFinished?.Invoke(comic);
            
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

            try
            {
                List<ComicPageData> tempPages;
                if (isComicImporting)
                {
                    string setArchiveUrl = null;

                    void UpdateComicInfo(IDocument document)
                    {
                        setArchiveUrl = document.Url;
                        comicData.Name = document.Title;
                    }

                    tempPages = await DiscoverPages(
                        comicData.ArchiveUrl,
                        UpdateComicInfo,
                        UpdateComicInfo,
                        cancelToken);

                    // Update archive url for for quicker updating next time if we didn't start on the archive page
                    if (!string.IsNullOrEmpty(setArchiveUrl))
                        comicData.ArchiveUrl = setArchiveUrl;
                }
                else
                {
                    // Comic can change display state to signify it's updating
                    comicData.IsUpdating = true;
                    comicData.ReportUpdated();

                    tempPages = await DiscoverPages(comicData.ArchiveUrl, cancelToken: cancelToken);
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

                    // Any existing pgaes that aren't in the new pages mark for deletion (create new list so we can modify existingPages collection)
                    var deletePages = existingPages
                            .Where(existingPage => !tempPages.Any(tempPage => tempPage.Url == existingPage.Url))
                            .ToList();

                    // Delete pages
                    foreach (var page in deletePages)
                    {
                        existingPages.Remove(page);
                        realm.Remove(page);
                    }

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
            }
            finally
            {
                // Comic may have just been deleted, so don't fire of anything that may attempt to access it
                if (comicData.IsValid)
                {
                    // Report that comic has updated so UI can refresh, etc.
                    comicData.IsUpdating = false;
                    comicData.ReportUpdated();
                }
            }

            return comicData.Pages;
        }

        public async Task<List<ComicPageData>> DiscoverPages(
            string pageUrl,
            Action<IDocument> onFoundArchivePage = null,
            Action<IDocument> onNotFoundArchivePage = null,
            CancellationToken cancelToken = default)
        {
            cancelToken.ThrowIfCancellationRequested();

            if (!IsUrlValid(pageUrl))
                return null;

            IDocument document = await pageLoader.OpenDocument(pageUrl);
            cancelToken.ThrowIfCancellationRequested();

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
                pages = await DiscoverPages(
                    archivePageUrl,
                    onFoundArchivePage,
                    cancelToken: cancelToken);
            }
            
            // Isn't passed in to recursive call above, so should be called for first page
            onNotFoundArchivePage?.Invoke(document);
            
            return pages;
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
                .Where(a => (a.TextContent?.ToUpperInvariant().Contains("ARCHIVE") ?? false)
                    || (a.GetAttribute("title")?.ToUpperInvariant().Contains("ARCHIVE") ?? false))
                .Select(a => a.GetAttribute("href"))
                .Where(link => !string.IsNullOrEmpty(link))
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(archiveLink))
                return archiveLink;

            // All attempts failed
            return null;
        }

        public static string GetAbsoluteUri(string sourceUri, string link)
        {
            // Link may be relative
            if (IsUrlValid(link))
                return link;

            // Handle if url starts with "http://" or "file:///"
            int splitSlash = new Uri(sourceUri).IsFile ? 4 : 3;

            string baseUrl = null;
            int slashCounter = 0;
            for (int i = 0; i < sourceUri.Length; i++)
            {
                if (sourceUri[i] == '/')
                {
                    slashCounter++;
                    if (slashCounter == splitSlash)
                    {
                        baseUrl = sourceUri.Substring(0, i);
                        break;
                    }
                }
            }

            // Couldn't extract base url
            if (baseUrl == null)
                return null;

            // Convert page url to absolute
            if (!string.IsNullOrEmpty(baseUrl))
            {
                if (link[0] == '/')
                    link = baseUrl + link;
                else
                    link = $"{baseUrl}/{link}";
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
