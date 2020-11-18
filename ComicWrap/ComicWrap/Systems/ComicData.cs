using System;
using System.Collections.Generic;
using System.Linq;
using Realms;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace ComicWrap.Systems
{
    public class ComicData : RealmObject
    {
        public event Action Updated;

        [PrimaryKey]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string ArchiveUrl { get; set; }

        [Backlink(nameof(ComicPageData.Comic))]
        public IQueryable<ComicPageData> Pages { get; }

        public IList<string> RecentHistory { get; }
        
        public float ReadProgress
        {
            get
            {
                // ElementAt is always returning the first element, so we need this workaround
                int pageCount = 0;
                int lastReadPage = 0;
                foreach (var page in Pages)
                {
                    pageCount++;

                    if (page.IsRead)
                        lastReadPage = pageCount;
                }

                return (float)lastReadPage / pageCount;
            }
        }

        public ComicPageData LastReadPage
        {
            get
            {
                var page = Pages
                    .LastOrDefault(p => p.IsRead);

                if (page != null)
                    return page;

                // Default to first page if none have been read yet
                return Pages.Any() ? Pages.ElementAt(0) : null;
            }
        }

        public ComicPageData LatestNewPage => Pages.LastOrDefault(page => page.IsNew);

        public DateTimeOffset? LastReadDate { get; set; }
        public DateTimeOffset? LastUpdatedDate { get; set; }

        [Ignored]
        public bool IsUpdating { get; set; }

        public void ReportUpdated()
        {
            Updated?.Invoke();
        }

        public void RecordHistory(string url)
        {
            ComicDatabase.Instance.Write(
                realm =>
                {
                    // Need to get an instance local to realm of write transaction
                    var comic = realm.Find<ComicData>(Id);

                    comic.RecentHistory.Add(url);

                    // Remove oldest history
                    int historyCount = comic.RecentHistory.Count;
                    while (historyCount > 20)
                    {
                        comic.RecentHistory.RemoveAt(0);
                        historyCount--;
                    }
                });
        }
    }
}
