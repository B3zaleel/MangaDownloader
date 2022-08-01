using System.Collections.Generic;

namespace MDXAMLUI.MangaRetrievers
{
    public interface IMangaRetriever
    {
        string RetrieverName { get; }
        string HomePage { get; }
        void FetchManga(List<Manga> mangas, string url);
        Chapter FetchChapter(Manga parent, string url, string name);
        void UpdateChapters(Manga parent);
    }
}
