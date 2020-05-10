#pragma warning disable AsyncFixer02 // Long running or blocking operations under an async method

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using NUnit.Framework;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;

using ComicWrap.Systems;
using System.Runtime.Serialization;

namespace ComicWrap.Tests
{
    public class MockComicState
    {
        public string ArchiveUrl { get; set; }
        public string PageUrl { get; set; }
        public int PageCount { get; set; }
    }

    public class MockComic
    {
        public MockComicState State1;
        public MockComicState State2;
    }

    public class MockPageLoader : IPageLoader
    {
        private IEnumerable<MockComic> comics;

        public Func<MockComic, MockComicState> StateSelector { get; set; }

        public MockPageLoader(IEnumerable<MockComic> comics)
        {
            this.comics = comics;
        }

        public Task<IDocument> OpenDocument(string url)
        {
            if (StateSelector != null)
            {
                // Flip out requested url for new state (assumes previous state was State1)
                foreach (var comic in comics)
                {
                    if (url == comic.State1.ArchiveUrl)
                    {
                        url = StateSelector(comic).ArchiveUrl;
                        break;
                    }
                    else if (url == comic.State1.PageUrl)
                    {
                        url = StateSelector(comic).PageUrl;
                        break;
                    }
                }
            }

            return PageLoader.GetBrowsingContext().OpenAsync(res =>
            {
                res.Content(new FileStream(new Uri(url).LocalPath, FileMode.Open), shouldDispose: true)
                   .Address(url);
            });
        }
    }

    public static class ComicUpdaterTests
    {
        private static ComicDatabase database;
        private static ComicUpdater comicUpdater;
        private static MockPageLoader pageLoader;

        [SetUp]
        public static void Setup()
        {
            // Should have a blank database for each test
            database = ComicDatabaseTests.GetNewDatabase();
            pageLoader = new MockPageLoader(EnumerateKnownComicTypes());
            comicUpdater = new ComicUpdater(database, pageLoader);
        }

        [Test]
        public static void IsUrlValidReturnsTrueWhenValid()
        {
            bool isValid = ComicUpdater.IsUrlValid("https://testurl.com/");
            Assert.AreEqual(true, isValid);
        }

        [Test]
        public static void IsUrlValidReturnsFalseWhenInvalid()
        {
            bool isValid = ComicUpdater.IsUrlValid("garbage");
            Assert.AreEqual(false, isValid);
        }

        public static IEnumerable<MockComic> EnumerateKnownComicTypes()
        {
            // Use files instead of hardcoding HTML so they behave like actual websites, and can be browsed manually
            string mockComicsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "MockWebsites");

            string[] mockComicFolders = Directory.GetDirectories(mockComicsFolderPath);
            foreach (var rootFolder in mockComicFolders)
            {
                // Put subfolder paths into dictionary for easily picking the ones we care about
                Dictionary<string, string> subfolders = Directory.GetDirectories(rootFolder)
                    .ToDictionary(path => new DirectoryInfo(path).Name);

                yield return new MockComic
                {
                    State1 = GetComicState(subfolders["State1"], 2),
                    State2 = GetComicState(subfolders["State2"], 3),
                };
            }
        }

        private static MockComicState GetComicState(string directoryPath, int expectedPageCount)
        {
            string archivePath = Path.Combine(directoryPath, "index.html");
            string knownPagePath = Path.Combine(directoryPath, "page-1.html");

            return new MockComicState
            {
                ArchiveUrl = new Uri(archivePath).AbsoluteUri,
                PageUrl = new Uri(knownPagePath).AbsoluteUri,
                PageCount = expectedPageCount
            };
        }

        [Test]
        public static async Task DiscoversPagesWorksForKnownTypesFromArchiveUrl(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            var pages = await comicUpdater.DiscoverPages(comic.State1.ArchiveUrl);

            // Found expected amount of pages
            Assert.AreEqual(
                expected: comic.State1.PageCount,
                actual: pages.Count,
                message: "Wrong page count");

            // Known page was in page list
            Assert.AreEqual(
                expected: comic.State1.PageUrl,
                actual: pages[0].Url,
                message: "Found page URL wrong");
        }

        [Test]
        public static async Task DiscoversPagesWorksForKnownTypesFromPageUrl(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            var pages = await comicUpdater.DiscoverPages(comic.State1.PageUrl);

            // Found expected amount of pages
            Assert.AreEqual(
                expected: comic.State1.PageCount,
                actual: pages.Count,
                message: "Wrong page count");

            // Known page was in page list
            Assert.AreEqual(
                expected: comic.State1.PageUrl,
                actual: pages[0].Url,
                message: "Found page URL wrong");
        }

        [Test]
        public static async Task ImportComicFailedReturnsNull()
        {
            ComicData savedComic = await comicUpdater.ImportComic("garbage url");

            Assert.IsNull(savedComic);
        }

        [Test]
        public static async Task PageIsNewIsFalseWhenImported(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            ComicData savedComic = await comicUpdater.ImportComic(comic.State1.ArchiveUrl);

            Assert.Multiple(() =>
            {
                // No pages should be new since comic was just imported
                foreach (var page in savedComic.Pages)
                    Assert.IsFalse(page.IsNew);
            });
        }

        [Test]
        public static async Task PageIsNewIsTrueWhenUpdated(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            // Initial comic import
            ComicData savedComic = await comicUpdater.ImportComic(comic.State1.ArchiveUrl);

            // Switch selector to next stage
            pageLoader.StateSelector = c => c.State2;

            // Update comic now that selector has been switched
            IEnumerable<ComicPageData> pages = await comicUpdater.UpdateComic(savedComic);

            Assert.GreaterOrEqual(pages.Count(p => p.IsNew), 1);

            Assert.Fail();
        }

        [Test]
        public static async Task ComicNameChangePersistsAfterUpdating(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            // Initial comic import
            ComicData savedComic = await comicUpdater.ImportComic(comic.State1.ArchiveUrl);

            database.Write(realm => savedComic.Name = "New Name");

            // Update comic now that selector has been switched
            IEnumerable<ComicPageData> pages = await comicUpdater.UpdateComic(savedComic);

            Assert.AreEqual("New Name", savedComic.Name);
        }

        [Test, Category(nameof(ComicUpdater.GetAbsoluteUri))]
        public static void GetAbsoluteUriHttpsSimple()
        {
            string url = ComicUpdater.GetAbsoluteUri("https://www.example.com/archive", "page-1");

            Assert.AreEqual("https://www.example.com/page-1", url);
        }

        [Test, Category(nameof(ComicUpdater.GetAbsoluteUri))]
        public static void GetAbsoluteUriHttpsComplex()
        {
            string url = ComicUpdater.GetAbsoluteUri("https://www.example.com/comic/archive", "comic/page-1");

            Assert.AreEqual("https://www.example.com/comic/page-1", url);
        }

        [Test, Category(nameof(ComicUpdater.GetAbsoluteUri))]
        public static void GetAbsoluteUriFileSimple()
        {
            string url = ComicUpdater.GetAbsoluteUri("file:///C:/archive.html", "page-1.html");

            Assert.AreEqual("file:///C:/page-1.html", url);
        }

        [Test, Category(nameof(ComicUpdater.GetAbsoluteUri))]
        public static void GetAbsoluteUriFileComplex()
        {
            string url = ComicUpdater.GetAbsoluteUri("file:///C:/comic/archive.html", "comic/page-1.html");

            Assert.AreEqual("file:///C:/comic/page-1.html", url);
        }
    }
}
