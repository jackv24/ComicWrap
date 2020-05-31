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
            RealmConfiguration = DefaultRealmConfiguration;
            Open();
        }

        public ComicDatabase(RealmConfigurationBase realmConfig)
        {
            RealmConfiguration = realmConfig;
            Open();
        }

        public static readonly RealmConfigurationBase DefaultRealmConfiguration = new RealmConfiguration
        {
            SchemaVersion = 3,
            MigrationCallback = OnRealmMigration
        };

        public RealmConfigurationBase RealmConfiguration { get; private set; }
        public Realm Realm { get; private set; }
        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen)
                return;

            Realm = Realm.GetInstance(RealmConfiguration);

            IsOpen = true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            Realm.Dispose();
            IsOpen = false;

            // Clean up management files for in memory realms (unit tests were filling desktop documents folder when unit testing)
            if (RealmConfiguration is InMemoryConfiguration)
            {
                string path = RealmConfiguration.DatabasePath;
                DirectoryInfo directory = Directory.GetParent(path);
                string fileName = Path.GetFileNameWithoutExtension(path);

                foreach (DirectoryInfo info in directory.GetDirectories())
                {
                    if (info.Name.Contains(fileName))
                        info.Delete(true);
                }

                foreach (FileInfo info in directory.GetFiles())
                {
                    if (info.Name.Contains(fileName))
                        info.Delete();
                }
            }
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
            LocalImageService.DeleteImage(comicData.Id);

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

        public Task WriteAsync(Action<Realm> action)
        {
            if (action == null)
                return null;

            return Realm.WriteAsync(action);
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

                // Schema 3: Removed CurrentPageUrl from ComicData
            }
        }
    }
}
