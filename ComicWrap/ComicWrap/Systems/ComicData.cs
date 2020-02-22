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
    }
}
