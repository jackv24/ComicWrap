#pragma warning disable AsyncFixer02 // Long running or blocking operations under an async method

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using AngleSharp;
using AngleSharp.Dom;

using ComicWrap.Systems;

namespace ComicWrap.Tests
{
    public class MockComic
    {
        public string ArchivePageHtmlStage1 { get; set; }
        public string ArchivePageHtmlStage2 { get; set; }
        public string ArchivePageUrl { get; set; }
        public string KnownPageUrl { get; set; }
        public int PageCount { get; set; }
    }

    public class MockPageLoader : IPageLoader
    {
        private IEnumerable<MockComic> comics;

        public static readonly MockComic KnownFailingPage =  new MockComic
        {
            ArchivePageHtmlStage1 = @"
                <!DOCTYPE html>
                <html>
                <body>
                        
                </body>
                </html>",
            ArchivePageUrl = "http://FAIL/comic/archive",
            KnownPageUrl = "http://FAIL/comic/page-2"
        };

        public Func<MockComic, string> HtmlSelector { get; set; }

        public MockPageLoader(IEnumerable<MockComic> comics)
        {
            this.comics = comics;
        }

        public Task<IDocument> OpenDocument(string pageUrl)
        {
            // Special case
            if (pageUrl.Contains("FAIL"))
                return ComicUpdaterTests.GetDocumentFromHtml(KnownFailingPage.ArchivePageHtmlStage1);

            // Search comics for matching archive url
            var html = string.Empty;
            foreach (var comic in comics)
            {
                if (comic.ArchivePageUrl == pageUrl)
                {
                    html = HtmlSelector(comic);
                    break;
                }
            }

            return ComicUpdaterTests.GetDocumentFromHtml(html);
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
            var knownComicTypes = EnumerateKnownComicTypes();

            // Should have a blank database for each test
            database = ComicDatabaseTests.GetNewDatabase();
            pageLoader = new MockPageLoader(knownComicTypes)
            {
                HtmlSelector = comic => comic.ArchivePageHtmlStage1
            };
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

        public static Task<IDocument> GetDocumentFromHtml(string html)
        {
            var context = PageLoader.GetBrowsingContext();
            return context.OpenAsync(req =>
            {
                req.Content(html);
            });
        }

        public static IEnumerable<MockComic> EnumerateKnownComicTypes()
        {
            // http://www.comicctrl.com/ Type 1
            yield return new MockComic
            {
                ArchivePageHtmlStage1 = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Type 1 Stage 1</title>
                    </head>
                    <body>
                        <div id=""wrapper"">
                            <main>
                                <div class=""main-content"">
                                    <section class=""text-block"">
                                        <script>
                                            function changePage(slug)
                                            {
                                                window.location.href = 'http://localhost1/' + slug;
                                            }
                                        </script>
                                        <select name = ""comic"" onChange=""changePage(this.value)"">
                                            <option value = """" > Select a comic...</option>
                                            <option value = ""comic/page-1"" > Page 1/option>
                                            <option value = ""comic/page-2"" > Page 2</option>
                                            <option value = ""comic/page-3"" > Page 3</option>
                                        </select>
                                    </section>
                                </div>
                            </main>
                        </div>
                    </body>
                    </html>",
                ArchivePageHtmlStage2 = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Type 1 Stage 2</title>
                    </head>
                    <body>
                        <div id=""wrapper"">
                            <main>
                                <div class=""main-content"">
                                    <section class=""text-block"">
                                        <script>
                                            function changePage(slug)
                                            {
                                                window.location.href = 'http://localhost1/' + slug;
                                            }
                                        </script>
                                        <select name = ""comic"" onChange=""changePage(this.value)"">
                                            <option value = """" > Select a comic...</option>
                                            <option value = ""comic/page-1"" > Page 1/option>
                                            <option value = ""comic/page-2"" > Page 2</option>
                                            <option value = ""comic/page-3"" > Page 3</option>
                                            <option value = ""comic/page-4"" > Page 3</option>
                                        </select>
                                    </section>
                                </div>
                            </main>
                        </div>
                    </body>
                    </html>",
                ArchivePageUrl = "http://localhost1/comic/archive",
                KnownPageUrl = "http://localhost1/comic/page-2",
                PageCount = 3
            };

            // http://www.comicctrl.com/ Type 2
            yield return new MockComic
            {
                ArchivePageHtmlStage1 = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Type 2 Stage 1</title>
                    </head>
                    <body>
                        <div id=""comicwrapouter"">
                            <div id = ""comicwrapinner"">
                                <script>
                                    function changePage(sub, slug)
                                    {
                                        window.location.href = 'http://localhost2/' + sub + '/' + slug;
                                    }
                                </script>
                                <select name = ""comic"" onChange=""changePage('comic',this.value)"" width=""100"">
                                    <option value = """" > Select a comic...</option>
                                    <option value = ""page-1"" > Page 1</option>
                                    <option value = ""page-2"" > Page 2</option>
                                    <option value = ""page-3"" > Page 3</option>
                                </select>
                            </div>
                        </div>
                    </body>
                    </html>",
                ArchivePageHtmlStage2 = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Type 2 Stage 2</title>
                    </head>
                    <body>
                        <div id=""comicwrapouter"">
                            <div id = ""comicwrapinner"">
                                <script>
                                    function changePage(sub, slug)
                                    {
                                        window.location.href = 'http://localhost2/' + sub + '/' + slug;
                                    }
                                </script>
                                <select name = ""comic"" onChange=""changePage('comic',this.value)"" width=""100"">
                                    <option value = """" > Select a comic...</option>
                                    <option value = ""page-1"" > Page 1</option>
                                    <option value = ""page-2"" > Page 2</option>
                                    <option value = ""page-3"" > Page 3</option>
                                    <option value = ""page-4"" > Page 3</option>
                                </select>
                            </div>
                        </div>
                    </body>
                    </html>",
                ArchivePageUrl = "http://localhost2/comic/archive",
                KnownPageUrl = "http://localhost2/comic/page-2",
                PageCount = 3
            };
        }

        [Test]
        public static async Task DiscoversPagesWorksForKnownTypes(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            var document = await GetDocumentFromHtml(comic.ArchivePageHtmlStage1);
            var pages = ComicUpdater.DiscoverPages(document, comic.KnownPageUrl);

            // Found expected amount of pages
            Assert.AreEqual(
                expected: comic.PageCount,
                actual: pages.Count,
                message: "Wrong page count");

            // Known page was in page list
            Assert.AreEqual(
                expected: comic.KnownPageUrl,
                actual: pages[1].Url,
                message: "Found page URL wrong");
        }

        [Test]
        public static async Task ImportComicFailedReturnsNull()
        {
            MockComic mockComic = MockPageLoader.KnownFailingPage;
            ComicData savedComic = await comicUpdater.ImportComic(mockComic.ArchivePageHtmlStage1, mockComic.KnownPageUrl);

            Assert.IsNull(savedComic);
        }

        [Test]
        public static async Task PageIsNewIsFalseWhenImported(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            ComicData savedComic = await comicUpdater.ImportComic(comic.ArchivePageUrl, comic.KnownPageUrl);

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
            ComicData savedComic = await comicUpdater.ImportComic(comic.ArchivePageUrl, comic.KnownPageUrl);

            // Switch selector to next stage
            pageLoader.HtmlSelector = c => c.ArchivePageHtmlStage2;

            // Update comic now that selector has been switched
            IEnumerable<ComicPageData> pages = await comicUpdater.UpdateComic(savedComic);

            Assert.GreaterOrEqual(pages.Count(p => p.IsNew), 1);
        }

        [Test]
        public static async Task ComicNameChangePersistsAfterUpdating(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            // Initial comic import
            ComicData savedComic = await comicUpdater.ImportComic(comic.ArchivePageUrl, comic.KnownPageUrl);

            database.Write(realm => savedComic.Name = "New Name");

            // Update comic now that selector has been switched
            IEnumerable<ComicPageData> pages = await comicUpdater.UpdateComic(savedComic);

            Assert.AreEqual("New Name", savedComic.Name);
        }
    }
}