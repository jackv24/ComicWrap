﻿using System;
using System.Linq;
using Realms;

namespace ComicWrap.Systems
{
    public class ComicData : RealmObject
    {
        public event Action Updated;

        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string ArchiveUrl { get; set; }

        [Backlink(nameof(ComicPageData.Comic))]
        public IQueryable<ComicPageData> Pages { get; }

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

        public ComicPageData LastReadPage => Pages.LastOrDefault(page => page.IsRead);
        public ComicPageData LatestNewPage => Pages.LastOrDefault(page => page.IsNew);

        public DateTimeOffset? LastReadDate { get; set; }
        public DateTimeOffset? LastUpdatedDate { get; set; }

        public void ReportUpdated()
        {
            Updated?.Invoke();
        }
    }
}
