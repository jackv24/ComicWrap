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
    public class ComicDatabase
    {
        public ComicDatabase()
        {
            realm = Realm.GetInstance(new RealmConfiguration
            {
                SchemaVersion = 0,
                MigrationCallback = OnRealmMigration
            });
        }

        private readonly Realm realm;

        private static ComicDatabase _instance;
        public static ComicDatabase Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ComicDatabase();

                return _instance;
            }
        }

        public void AddComic(ComicData comicData)
        {
            realm.Add(comicData, true);
        }

        public async Task DeleteComic(ComicData comicData)
        {
            await WriteAsync(realm =>
            {
                realm.Remove(comicData);

                // TODO: Test if removing comic also removes pages
            });
        }

        public List<ComicData> GetComics()
        {
            return realm.All<ComicData>()
                .ToList();
        }

        public async Task WriteAsync(Action<Realm> action)
        {
            if (action == null)
                return;

            await realm.WriteAsync(realm => action(realm));
        }

        public void Write(Action action)
        {
            if (action == null)
                return;

            using (var trans = realm.BeginWrite())
            {
                action();
                trans.Commit();
            }
        }

        public void Write(Action<Realm> action)
        {
            if (action == null)
                return;

            using (var trans = realm.BeginWrite())
            {
                action(realm);
                trans.Commit();
            }
        }

        private static void OnRealmMigration(Migration migration, ulong oldSchemaVersion)
        {
            // Handle realm migration when needed
        }
    }
}
