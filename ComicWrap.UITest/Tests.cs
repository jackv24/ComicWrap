using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace ComicWrap.UITest
{
    [TestFixture(Platform.Android)]
    [TestFixture(Platform.iOS)]
    public class Tests
    {
        IApp app;
        Platform platform;

        public Tests(Platform platform)
        {
            this.platform = platform;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            app = AppInitializer.StartApp(platform);
        }

        [Test]
        public void HomePageIsDisplayed()
        {
            AppResult[] results = app.WaitForElement(c => c.Marked("HomePage"));
            app.Screenshot("Home Page");

            Assert.IsTrue(results.Any());
        }

        [Test]
        public void AddComicPageIsDisplayed()
        {
            app.Tap("AddComicButton");
            AppResult[] results = app.WaitForElement("AddComicPage");

            app.Screenshot("Add Comic Page");

            Assert.IsTrue(results.Any());
        }

        [Test]
        public void AddComicPageAddEmptyDisplaysError ()
        {
            app.Tap("AddComicButton");
            app.Tap("SubmitButton");

            AppResult[] results;
            switch (platform)
            {
                case Platform.Android:
                    results = app.Query(c => c.Class("AlertDialogLayout"));
                    break;

                case Platform.iOS:
                    results = app.Query(c => c.ClassFull("_UIAlertControllerView"));
                    break;

                default:
                    throw new NotImplementedException();
            }

            app.Screenshot("Add Comic Page Error");

            Assert.IsTrue(results.Any());
        }
    }
}
