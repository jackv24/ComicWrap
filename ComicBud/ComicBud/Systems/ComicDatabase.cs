using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Xamarin.Essentials;

using SQLite;

namespace ComicBud.Systems
{
    public class ComicDatabase
    {
        public ComicDatabase()
        {
            string dbFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string dbFileName = "Comics.db3";
            string dbPath = Path.Combine(dbFolderPath, dbFileName);

            database = new SQLiteConnection(dbPath);
            database.CreateTable<Comic>();

            const string isCreatedKey = "ComicDatabase_isCreated";
            bool isCreated = Preferences.Get(isCreatedKey, false);
            if (!isCreated)
            {
                Preferences.Set(isCreatedKey, true);
                UpdateComic(new Comic
                {
                    Name = "Example Comic"
                });
            }
        }

        private SQLiteConnection database;

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

        public IEnumerable<Comic> GetComics(Func<Comic, bool> predicate = null)
        {
            var table = database.Table<Comic>();
            if (predicate == null)
                return table;
            else
                return table.Where(predicate);
        }

        public void UpdateComic(Comic comic)
        {
            if (comic.ID != 0)
                database.Update(comic);
            else
                database.Insert(comic);
        }

        public void DeleteComic(Comic comic)
        {
            database.Delete(comic);
        }
    }
}
