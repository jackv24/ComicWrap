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
                int pageCount = Pages.Count();
                if (pageCount == 0)
                    return 0;

                int lastReadPageIndex = -1;
                for (int i = 0; i < pageCount; i++)
                {
                    var page = Pages.ElementAt(i);
                    if (page.IsRead)
                        lastReadPageIndex = i;
                }

                return (float)(lastReadPageIndex + 1) / pageCount;
            }
        }

        public int DaysSinceLastUpdated
        {
            // TODO: Actually implement
            get { return 0; }
        }
    }
}
