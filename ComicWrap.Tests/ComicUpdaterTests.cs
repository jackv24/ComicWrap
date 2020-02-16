using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using AngleSharp;

using ComicWrap.Systems;

namespace ComicWrap.Tests
{
    public class ComicUpdaterTests
    {
        public class ComicWebsiteSkeleton
        {
            public string ArchivePageUrl;
            public string CurrentPageUrl;
            public List<string> ComicPageUrls;
        }

        public static IEnumerable EnumerateTestComicData()
        {
            // Goodbye to Halos
            // Archive page has a dropdown list
            yield return new ComicWebsiteSkeleton
            {
                ArchivePageUrl = "https://goodbyetohalos.com/comic/archive",
                CurrentPageUrl = "https://goodbyetohalos.com/comic/02199",
                ComicPageUrls = new List<string>
                {
                    "https://goodbyetohalos.com/comic/02197",
                    "https://goodbyetohalos.com/comic/02198",
                    "https://goodbyetohalos.com/comic/02199",
                    "https://goodbyetohalos.com/comic/021100",
                    "https://goodbyetohalos.com/comic/021101",
                    "https://goodbyetohalos.com/comic/021102103",
                }
            };

            // Litterbox Comics
            // Archive page has comic pages as seperate links
            yield return new ComicWebsiteSkeleton
            {
                ArchivePageUrl = "https://www.litterboxcomics.com/archive/",
                CurrentPageUrl = "https://www.litterboxcomics.com/solidarity/",
                ComicPageUrls = new List<string>
                {
                    "https://www.litterboxcomics.com/little-jerk/",
                    "https://www.litterboxcomics.com/shoes/",
                    "https://www.litterboxcomics.com/solidarity/",
                    "https://www.litterboxcomics.com/scream/",
                    "https://www.litterboxcomics.com/in-the-mood/"
                }
            };

            // xkcd
            // Archive page has comic pages as seperate links
            yield return new ComicWebsiteSkeleton
            {
                ArchivePageUrl = "https://www.xkcd.com/archive/",
                CurrentPageUrl = "https://www.xkcd.com/2266/",
                ComicPageUrls = new List<string>
                {
                    "https://www.xkcd.com/2268/",
                    "https://www.xkcd.com/2267/",
                    "https://www.xkcd.com/2266/",
                    "https://www.xkcd.com/2265/",
                    "https://www.xkcd.com/2264/"
                }
            };
        }

        [Test]
        public async Task DiscoverPageFindsAllPageUrls([ValueSource(nameof(EnumerateTestComicData))] ComicWebsiteSkeleton comicData)
        {
            var browsingContext = ComicUpdater.GetBrowsingContext();
            var document = await browsingContext.OpenAsync(comicData.ArchivePageUrl);

            List<string> discoveredPages = ComicUpdater.DiscoverPages(document, comicData.CurrentPageUrl)
                .Select(page => page.Url)
                .ToList();

            foreach (var pageUrl in comicData.ComicPageUrls)
                Assert.Contains(pageUrl, discoveredPages);
        }
    }
}