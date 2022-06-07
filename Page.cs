namespace MangaDownloader
{
    public class Page
    {
        public Page(Chapter parent)
        {
            Parent = parent;
        }

        public bool Saved { get; set; }
        public string Address { get; set; }
        public Chapter Parent { get; }
    }
}
