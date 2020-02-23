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

        public ComicDatabase(Realm realm)
        {
            this.realm = realm;
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
            using (var trans = realm.BeginWrite())
            {
                realm.Add(comicData, true);
                trans.Commit();
            }
        }

        public void DeleteComic(ComicData comicData)
        {
            using (var trans = realm.BeginWrite())
            {
                // Delete pages and then comic (so comic data isn't invalidated before it's pages can be delete)
                realm.RemoveRange(comicData.Pages);
                realm.Remove(comicData);

                trans.Commit();
            }
        }

        public List<ComicData> GetComics()
        {
            return realm.All<ComicData>()
                .ToList();
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
