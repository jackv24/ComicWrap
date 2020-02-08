using System;
using System.Collections.Generic;
using System.Linq;
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

            var pages = await UpdateComic(comic);

            // TODO: Mark all as read up to current page url

            // TODO: Save to database
        }

        public static async Task<IEnumerable<ComicPageData>> UpdateComic(ComicData comic)
        {
            // Load comic archive page
            var document = await GetBrowsingContext().OpenAsync(comic.ArchiveUrl);

            // Update comic name
            comic.Name = document.Title;

            var newPages = await DiscoverPages(document);
            var oldPages = await ComicDatabase.Instance.GetComicPages(comic);

            // Remove old data from database
            await ComicDatabase.Instance.DeleteComic(comic);

            // Reset comic ID so UpdateComic uses Insert
            comic.Id = 0;

            // Add new data back into database before setting up pages, so comic Id is set
            await ComicDatabase.Instance.UpdateComic(comic);

            // New page data is bare, so fill out missing data
            foreach (var newPage in newPages)
            {
                newPage.ComicId = comic.Id;

                foreach (var oldPage in oldPages)
                {
                    // Transfer persistent data to new page data
                    if (newPage.Url == oldPage.Url)
                        newPage.IsRead = oldPage.IsRead;
                }
            }

            await ComicDatabase.Instance.UpdateComicPages(newPages);

            return newPages;
        }

        private static async Task<List<ComicPageData>> DiscoverPages(IDocument document)
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
