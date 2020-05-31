using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using NUnit.Framework;
using AngleSharp;
using Realms;

using ComicWrap.Systems;
using System.IO;

namespace ComicWrap.Tests
{
    public static class ComicDatabaseTests
    {
        private static ComicDatabase database;

        [SetUp]
        public static void Setup()
        {
            string identifier = Guid.NewGuid().ToString();
            database = new ComicDatabase(new InMemoryConfiguration(identifier));
        }

        [TearDown]
        public static void Teardown()
        {
            database.Close();
        }

        [Test]
        public static void AddNewComic()
        {
            database.AddComic(new ComicData());

            var comics = database.GetComics();
            var comicCount = comics.Count;
            Assert.AreEqual(1, comicCount);
        }

        [Test]
        public static void DeleteComic()
        {
            var comic = new ComicData();
            database.AddComic(comic);

            database.DeleteComic(comic);

            var comics = database.GetComics();
            var comicCount = comics.Count;
            Assert.AreEqual(0, comicCount);
        }

        [Test]
        public static void DeletingComicDeletesPages()
        {
            var comic = new ComicData();
            database.Write(realm =>
            {
                // Add 1 comic with 1 page
                realm.Add(comic);
                var page = new ComicPageData
                {
                    Comic = comic
                };
                realm.Add(page);
            });

            // Delete comic
            database.DeleteComic(comic);

            // Check that page was also deleted (should be no pages in database)
            var pages = database.Realm.All<ComicPageData>();
            var pageCount = pages.Count();
            Assert.AreEqual(0, pageCount);
        }

        [Test]
        public static void DeletingComicDeletesCoverImage()
        {
            var comic = new ComicData();

            database.AddComic(comic);
            string imagePath = LocalImageService.WriteImage(comic.Id, new byte[0]);

            database.DeleteComic(comic);

            Assert.IsFalse(File.Exists(imagePath), "Comic cover image was not deleted.");
        }

        [Test]
        public static void ComicPageMarkReadClearsIsNew()
        {
            var comic = new ComicData();
            var page = new ComicPageData
            {
                Comic = comic,
                IsRead = false,
                IsNew = true
            };
            database.Write(realm =>
            {
                // Add 1 comic with 1 page
                realm.Add(comic);
                realm.Add(page);
            });

            database.MarkRead(page);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(page.IsRead);
                Assert.IsFalse(page.IsNew);
            });
        }
    }
}