using System;
using System.Linq;
using Realms;

namespace ComicWrap.Systems
{
    public class ComicData : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string ArchiveUrl { get; set; }
        public string CurrentPageUrl { get; set; }

        [Backlink(nameof(ComicPageData.Comic))]
        public IQueryable<ComicPageData> Pages { get; }

        public ComicPageData LastReadPage
        {
            get
            {
                if (Pages.Count() == 0)
                    return null;

                ComicPageData lastReadPage = null;
                foreach (var page in Pages)
                {
                    if (page.IsRead)
                        lastReadPage = page;
                }

                return lastReadPage ?? Pages.ElementAt(0);
            }
        }

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

        public int DaysSinceLastUpdated
        {
            // TODO: Actually implement
            get { return 0; }
        }
    }
}
