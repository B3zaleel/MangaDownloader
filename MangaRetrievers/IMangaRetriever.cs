using System.Collections.Generic;

namespace MangaDownloader.MangaRetrievers
{
    public interface IMangaRetriever
    {
        string RetrieverName { get; }
        string HomePage { get; }
        void FetchManga(List<Manga> mangas, string mangaURL);
        Chapter FetchChapter(Manga parent, string chapterURL);
        void UpdateChapters(Manga parent);
    }
}
