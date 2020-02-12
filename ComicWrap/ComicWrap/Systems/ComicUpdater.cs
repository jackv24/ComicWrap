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
                ArchiveUrl = archiveUrl
            };

            await UpdateComic(comic, currentPageUrl);
        }

        public static async Task<IEnumerable<ComicPageData>> UpdateComic(
            ComicData comic,
            string markReadUpToUrl = null,
            CancellationToken cancelToken = default)
        {

            // Load comic archive page
            cancelToken.ThrowIfCancellationRequested();
            var document = await GetBrowsingContext().OpenAsync(comic.ArchiveUrl);

            // Update comic name
            comic.Name = document.Title;

            var newPages = DiscoverPages(document);
            cancelToken.ThrowIfCancellationRequested();
            var oldPages = await ComicDatabase.Instance.GetComicPages(comic);
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

                foreach (var oldPage in oldPages)
                {
                    // Transfer persistent data to new page data
                    if (newPage.Url == oldPage.Url)
                        newPage.IsRead = oldPage.IsRead;
                }
            }

            // NOTE: After this point we can no longer cancel as database is being written to
            cancelToken.ThrowIfCancellationRequested();

            // Run database operations as one transaction to prevent issues
            await ComicDatabase.Instance.RunInTransactionAsync(database =>
            {
                // Delete any existing pages that aren't in the new pages
                var deletePages = oldPages.Where(page => !newPages.Contains(page));
                foreach (var page in deletePages)
                    database.Delete(page);

                // Add comic to database first so ID is set
                if (comic.Id == 0)
                    database.Insert(comic);
                else
                    database.Update(comic);

                // Add pages to database after Comic.Id is set so their ComicId is correct
                foreach (var page in newPages)
                {
                    page.ComicId = comic.Id;

                    if (page.Id == 0)
                        database.Insert(page);
                    else
                        database.Update(page);
                }
            });

            return newPages;
        }

        private static List<ComicPageData> DiscoverPages(IDocument document)
        {
            // TODO: Expand to work with more sites

            var uri = new Uri(document.BaseUri);
            string baseUrl = uri.GetLeftPart(UriPartial.Authority);

            var elements = document.GetElementsByName("comic");
            if (elements.Length == 0)
                return new List<ComicPageData>();
            else
            {
                return elements[0].Children
                    .Select(element => new { Text = element.TextContent, Nav = element.GetAttribute("value") })
                    // Ignore empty values (usually 1 empty value is the "Select..." prompt
                    .Where(a => !string.IsNullOrEmpty(a.Nav))
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
