using System;
using Realms;

namespace ComicWrap.Systems
{
    public class ComicPageData : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public ComicData Comic { get; set; }

        public string Name { get; set; }
        public string Url { get; set; }

        public bool IsRead { get; set; }
        public bool IsNew { get; set; }
    }
}
