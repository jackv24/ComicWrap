using SQLite;

namespace ComicBud.Systems
{
    public class Comic
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public string Name { get; set; }
        public string ArchiveUrl { get; set; }
    }
}
