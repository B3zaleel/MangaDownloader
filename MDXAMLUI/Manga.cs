using System.Collections.Generic;
using System.ComponentModel;

namespace MDXAMLUI
{
    public enum BookFormats
    {
        none,
        cbz,
        cb7,
    }

    public enum ImageTypes
    {
        None,
        BMP,
        GIF,
        JPEG,
        JPG,
        PNG,
        TIFF,
    }

    public class Manga : INotifyPropertyChanged
    {
        public Manga()
        {
            IsAvailable = true;
            OtherProps = new Dictionary<string, string>();
            BookFormat = BookFormats.none;
        }

        public string Title { get; set; }
        public BookFormats BookFormat { get; set; }
        public string RetrieverName { get; set; }
        public CoverImage Cover
        {
            get { return cover; }
            set
            {
                cover = value;
                OnPropertyChanged("Cover");
            }
        }
        public Dictionary<string, string> OtherProps { get; set; }
        public List<Chapter> Chapters { get; set; }
        public string Description { get; set; }
        public int ChaptersCount
        {
            get { return chaptersCount; }
            set
            {
                chaptersCount = value;
                OnPropertyChanged("ChaptersCount");
            }
        }
        public string[] Genres { get; set; }
        public string Address { get; set; }

        public bool IsAvailable { get; set; }
        public bool IsUpdatingChapters { get; set; }

        private CoverImage cover;
        private int chaptersCount;

        public void MergeChapters(List<Chapter> chapters)
        {
            Chapters.AddRange(chapters);
            Chapter.SortChapters(Chapters);
            Chapters.Reverse();
            ChaptersCount += chapters.Count;
            OnPropertyChanged("Chapters");
        }

        public void MergeChapter(Chapter chapter)
        {
            Chapters.Add(chapter);
            Chapter.SortChapters(Chapters);
            Chapters.Reverse();
            ChaptersCount++;
            OnPropertyChanged("Chapters");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
