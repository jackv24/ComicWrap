using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AngleSharp;

using ComicBud.Systems;

namespace ComicBud.Systems
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

        public static async Task<IEnumerable<ComicPageData>> GetPages(string archiveUrl)
        {
            // TODO: Expand to work with more sites

            var uri = new Uri(archiveUrl);
            string baseUrl = uri.GetLeftPart(UriPartial.Authority);

            var browsingContext = GetBrowsingContext();
            var document = await browsingContext.OpenAsync(archiveUrl);
            var elements = document.GetElementsByName("comic");
            if (elements.Length == 0)
                return Enumerable.Empty<ComicPageData>();
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
                    });
            }
        }

        public static IBrowsingContext GetBrowsingContext()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            return context;
        }
    }
}
