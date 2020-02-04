using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Xamarin.Essentials;

using SQLite;

namespace ComicWrap.Systems
{
    public interface IComicData
    {
        int Id { get; set; }
    }

    public class ComicDatabase
    {
        public ComicDatabase()
        {
            string dbFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string dbFileName = "Comics.db3";
            string dbPath = Path.Combine(dbFolderPath, dbFileName);

            database = new SQLiteConnection(dbPath);
            database.CreateTables(CreateFlags.None,
                typeof(ComicData),
                typeof(ComicPageData)
                );

            const string isCreatedKey = "ComicDatabase_isCreated";
            bool isCreated = Preferences.Get(isCreatedKey, false);
            if (!isCreated)
            {
                Preferences.Set(isCreatedKey, true);
                SetData(new ComicData
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

        public IEnumerable<T> GetData<T>(Func<T, bool> predicate = null)
            where T : IComicData, new()
        {
            var table = database.Table<T>();
            if (predicate == null)
                return table;
            else
                return table.Where(predicate);
        }

        public void SetData<T>(T comic)
            where T : IComicData, new()
        {
            if (comic.Id != 0)
                database.Update(comic);
            else
                database.Insert(comic);
        }

        public void DeleteData<T>(T comic)
            where T : IComicData, new()
        {
            database.Delete(comic);
        }
    }
}
