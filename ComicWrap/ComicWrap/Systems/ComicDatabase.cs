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
            Realm = Realm.GetInstance(new RealmConfiguration
            {
                SchemaVersion = 0,
                MigrationCallback = OnRealmMigration
            });
        }

        public ComicDatabase(Realm realm)
        {
            Realm = realm;
        }

        public Realm Realm { get; private set; }

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

        private static void OnRealmMigration(Migration migration, ulong oldSchemaVersion)
        {
            // Handle realm migration when needed
        }
    }
}
