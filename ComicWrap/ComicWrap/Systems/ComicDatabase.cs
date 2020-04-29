using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Threading.Tasks;

using Xamarin.Essentials;

using Realms;

namespace ComicWrap.Systems
{
    public class ComicDatabase : SingletonBase<ComicDatabase>
    {
        public ComicDatabase()
        {
            Open();
        }

        public ComicDatabase(Realm realm)
        {
            Realm = realm;
            IsOpen = true;
        }

        public Realm Realm { get; private set; }
        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen)
                return;

            Realm = Realm.GetInstance(new RealmConfiguration
            {
                SchemaVersion = 2,
                MigrationCallback = OnRealmMigration
            });

            IsOpen = true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            Realm.Dispose();
            IsOpen = false;
        }

        public void AddComic(ComicData comicData)
        {
            using (var trans = Realm.BeginWrite())
            {
                Realm.Add(comicData, true);
                trans.Commit();
            }
        }

        public void DeleteComic(ComicData comicData)
        {
            using (var trans = Realm.BeginWrite())
            {
                // Delete pages and then comic (so comic data isn't invalidated before it's pages can be delete)
                Realm.RemoveRange(comicData.Pages);
                Realm.Remove(comicData);

                trans.Commit();
            }
        }

        public IQueryable<ComicData> GetComicsQuery()
        {
            return Realm.All<ComicData>();
        }

        public List<ComicData> GetComics()
        {
            return Realm.All<ComicData>()
                .ToList();
        }

        public void Write(Action action)
        {
            if (action == null)
                return;

            using (var trans = Realm.BeginWrite())
            {
                action();
                trans.Commit();
            }
        }

        public void Write(Action<Realm> action)
        {
            if (action == null)
                return;

            using (var trans = Realm.BeginWrite())
            {
                action(Realm);
                trans.Commit();
            }
        }

        public void MarkRead(ComicPageData page)
        {
            Write(realm =>
            {
                page.IsRead = true;
                page.IsNew = false;

                page.Comic.LastReadDate = DateTimeOffset.UtcNow;
            });
        }

        private static void OnRealmMigration(Migration migration, ulong oldSchemaVersion)
        {
            var newComicPages = migration.NewRealm.All<ComicPageData>();

            for (int i = 0; i < newComicPages.Count(); i++)
            {
                var newPage = newComicPages.ElementAt(i);

                if (oldSchemaVersion < 1)
                    newPage.IsNew = false;
            }

            var newComics = migration.NewRealm.All<ComicData>();

            for (int i = 0; i < newComics.Count(); i++)
            {
                var newComic = newComics.ElementAt(i);

                if (oldSchemaVersion < 2)
                {
                    newComic.LastUpdatedDate = null;
                    newComic.LastReadDate = null;
                }
            }
        }
    }
}
