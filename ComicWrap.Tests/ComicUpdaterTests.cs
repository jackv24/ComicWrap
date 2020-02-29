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
    public static class ComicUpdaterTests
    {
        public class MockComic
        {
            public string ArchivePageHtml;
            public string KnownPageUrl;
            public int PageCount;
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

        private static async Task<IDocument> GetDocumentFromHtml(string html)
        {
            var context = ComicUpdater.GetBrowsingContext();
            return await context.OpenAsync(req =>
            {
                req.Content(html);
            });
        }

        public static IEnumerable<MockComic> EnumerateKnownComicTypes()
        {
            yield return new MockComic
            {
                ArchivePageHtml = @"
                    <!DOCTYPE html>
                    <html>
                    <body>
                        <div id=""wrapper"">
                            <main>
                                <div class=""main-content"">
                                    <section class=""text-block"">
                                        <script>
                                            function changePage(slug)
                                            {
                                                window.location.href = 'http://localhost/' + slug;
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
                KnownPageUrl = "http://localhost/comic/page-2",
                PageCount = 3
            };
            
            yield return new MockComic
            {
                ArchivePageHtml = @"
                    <!DOCTYPE html>
                    <html>
                    <body>
                        <div id=""comicwrapouter"">
                            <div id = ""comicwrapinner"">
                                <script>
                                    function changePage(sub, slug)
                                    {
                                        window.location.href = 'http://localhost/' + sub + '/' + slug;
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
                KnownPageUrl = "http://localhost/comic/page-2",
                PageCount = 3
            };
        }

        [Test]
        public static async Task DiscoversPagesWorksForKnownTypes(
            [ValueSource(nameof(EnumerateKnownComicTypes))] MockComic comic)
        {
            var document = await GetDocumentFromHtml(comic.ArchivePageHtml);
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
    }
}