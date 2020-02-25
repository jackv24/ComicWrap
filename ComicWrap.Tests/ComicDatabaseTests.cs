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

namespace ComicWrap.Tests
{
    public class ComicDatabaseTests
    {
        private ComicDatabase GetNewDatabase()
        {
            string identifier = Guid.NewGuid().ToString();
            var realm = Realm.GetInstance(new InMemoryConfiguration(identifier));
            return new ComicDatabase(realm);
        }

        [Test]
        public void AddNewComic()
        {
            var database = GetNewDatabase();

            database.AddComic(new ComicData());

            var comics = database.GetComics();
            var comicCount = comics.Count;
            Assert.AreEqual(1, comicCount);
        }

        [Test]
        public void DeleteComic()
        {
            var database = GetNewDatabase();
            var comic = new ComicData();
            database.AddComic(comic);

            database.DeleteComic(comic);

            var comics = database.GetComics();
            var comicCount = comics.Count;
            Assert.AreEqual(0, comicCount);
        }

        [Test]
        public void DeletingComicDeletesPages()
        {
            var database = GetNewDatabase();
            var comic = new ComicData();
            database.Write(realm =>
            {
                // Add 1 comic with 1 page
                realm.Add(comic);
                var page = new ComicPageData();
                page.Comic = comic;
                realm.Add(page);
            });

            // Delete comic
            database.DeleteComic(comic);

            // Check that page was also deleted (should be no pages in database)
            var pages = database.Realm.All<ComicPageData>();
            var pageCount = pages.Count();
            Assert.AreEqual(0, pageCount);
        }
    }
}