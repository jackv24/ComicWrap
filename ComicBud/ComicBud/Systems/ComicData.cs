using SQLite;

namespace ComicBud.Systems
{
    [Table("comics")]
    public class ComicData : IComicData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public string ArchiveUrl { get; set; }
    }
}
