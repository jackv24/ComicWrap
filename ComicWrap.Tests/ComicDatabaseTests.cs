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
        private ComicDatabase GetNewDatabase([CallerMemberName] string identifier = "")
        {
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
    }
}