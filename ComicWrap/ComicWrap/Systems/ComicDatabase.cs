using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Threading.Tasks;

using Xamarin.Essentials;

using SQLite;

namespace ComicWrap.Systems
{
    public class ComicDatabase
    {
        public ComicDatabase()
        {
            string dbFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string dbFileName = "Comics.db3";
            string dbPath = Path.Combine(dbFolderPath, dbFileName);

            // Use synchonous connection on startup so we know database
            // is setup correctly before anything else accesses it
            var syncDatabase = new SQLiteConnection(dbPath);
            syncDatabase.CreateTables(CreateFlags.None,
                typeof(ComicData),
                typeof(ComicPageData)
                );

            // Use async connection at runtime for responsiveness
            database = new SQLiteAsyncConnection(dbPath);
        }

        private SQLiteAsyncConnection database;

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

        public async Task UpdateComic(ComicData comicData)
        {
            if (comicData.Id == 0)
                await database.InsertAsync(comicData);
            else
                await database.UpdateAsync(comicData);
        }

        public async Task UpdateComicPages(IEnumerable<ComicPageData> comicPages)
        {
            // Update comic pages in a transaction for efficiency (there may be lots)
            await database.RunInTransactionAsync((database) =>
            {
                foreach (var pageData in comicPages)
                {
                    if (pageData.Id == 0)
                        database.Insert(pageData);
                    else
                        database.Update(pageData);
                }
            });
        }

        public async Task DeleteComic(ComicData comicData)
        {
            await database.RunInTransactionAsync((database) =>
            {
                // Delete comic pages that are linked to the comic
                var table = database.Table<ComicPageData>();
                table.Delete((pageData) => pageData.ComicId == comicData.Id);

                // Delete comic
                database.Delete(comicData);
            });
        }

        public async Task<List<ComicData>> GetComics()
        {
            var table = database.Table<ComicData>();
            return await table.ToListAsync();
        }

        public async Task<List<ComicPageData>> GetComicPages(ComicData comic)
        {
            var table = database.Table<ComicPageData>();
            return await table
                .Where(page => page.ComicId == comic.Id)
                .ToListAsync();
        }
    }
}
