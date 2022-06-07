using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaDownloader.IOUtilities
{
    public static class MDBinFormat
    {
        /// <summary>
        /// 0x7768667378 = MDBIN
        /// </summary>
        public const long MAGIC_NUM = 0x7768667378;

        public static void Serialize(string path, List<Manga> mangas)
        {
            var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            var binWriter = new BinaryWriter(fs);

            binWriter.Write(MAGIC_NUM);

            SerializeMangas(binWriter, mangas);

            binWriter.Close();
        }

        private static void SerializeMangas(BinaryWriter binWriter, List<Manga> mangas)
        {
            binWriter.Write(mangas.Count);
            foreach (var manga in mangas)
            {
                binWriter.Write(manga.Title);
                binWriter.Write(manga.BookFormat.ToString());
                binWriter.Write(manga.RetrieverName ?? "");


                binWriter.Write(manga.Cover?.Type.ToString() ?? "None");
                if (manga.Cover?.Type != ImageTypes.None)
                {
                    binWriter.Write(manga.Cover.Data.Length);
                    binWriter.Write(manga.Cover.Data);
                    binWriter.Write(manga.Cover.Address);
                }

                if (manga.OtherProps != null)
                {
                    binWriter.Write(manga.OtherProps.Count);
                    foreach (var otherProp in manga.OtherProps)
                    {
                        binWriter.Write(otherProp.Key ?? "");
                        binWriter.Write(otherProp.Value ?? "");
                    }
                }
                else
                    binWriter.Write(0);

                binWriter.Write(manga.ChaptersCount);

                SerializeChapters(binWriter, manga.Chapters);

                binWriter.Write(manga.Description ?? "");

                binWriter.Write(manga.Genres.Length);
                foreach (var genre in manga.Genres)
                {
                    binWriter.Write(genre ?? "");
                }

                binWriter.Write(manga.Address ?? "");
            }
        }

        private static void SerializeChapters(BinaryWriter binWriter, List<Chapter> chapters)
        {
            binWriter.Write(chapters.Count);
            foreach (var chapter in chapters)
            {
                binWriter.Write(chapter.Id);
                binWriter.Write(chapter.Title);

                SerializePages(binWriter, chapter.Pages);

                binWriter.Write(chapter.Address);
            }
        }

        private static void SerializePages(BinaryWriter binWriter, List<Page> pages)
        {
            binWriter.Write(pages.Count);
            foreach (var page in pages)
            {
                binWriter.Write(page.Saved);
                binWriter.Write(page.Address);
            }
        }

        public static List<Manga> Deserialize(string path)
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var binReader = new BinaryReader(fs);
            List<Manga> mangas = new List<Manga>();
            if (binReader.BaseStream.CanRead && binReader.BaseStream.Length >= 12)
            {
                var magicNum = binReader.ReadInt64();
                if (magicNum != MAGIC_NUM)
                    throw new InvalidDataException("Invalid MDBIN File");
                DeserializeMangas(binReader, ref mangas);
            }

            binReader.Close();
            return new List<Manga>(mangas);
        }

        private static void DeserializeMangas(BinaryReader binReader, ref List<Manga> mangas)
        {
            mangas = new List<Manga>();
            int mangasCount = binReader.ReadInt32();

            for (int i = 0; i < mangasCount; i++)
            {
                var manga = new Manga();
                manga.Title = binReader.ReadString();
                var enumStr = binReader.ReadString();
                if (!Enum.TryParse(enumStr, out BookFormats bookFormat))
                    throw new InvalidDataException("Unexpected token");
                manga.BookFormat = bookFormat;
                manga.RetrieverName = binReader.ReadString();

                var coverTypeEnumStr = binReader.ReadString();
                if (!Enum.TryParse(coverTypeEnumStr, out ImageTypes coverType))
                    throw new InvalidDataException("Unexpected token");
                if (coverType != ImageTypes.None)
                {
                    var imgDataLength = binReader.ReadInt32();
                    var imgData = binReader.ReadBytes(imgDataLength);
                    var imgAddress = binReader.ReadString();
                    manga.Cover = new CoverImage()
                    {
                        Type = coverType,
                        Data = imgData,
                        Address = imgAddress,
                    };
                }
                else
                {
                    manga.Cover = null;
                }

                manga.OtherProps = new Dictionary<string, string>();
                int otherPropsCount = binReader.ReadInt32();

                for (int j = 0; j < otherPropsCount; j++)
                {
                    var propKey = binReader.ReadString();
                    var propValue = binReader.ReadString();
                    manga.OtherProps.Add(propKey, propValue);
                }
                manga.ChaptersCount = binReader.ReadInt32();

                DeserializeChapters(binReader, manga);

                manga.Description = binReader.ReadString();

                var genres = new List<string>();
                int genresCount = binReader.ReadInt32();

                for (int k = 0; k < genresCount; k++)
                {
                    genres.Add(binReader.ReadString());
                }
                manga.Genres = genres.ToArray();

                manga.Address = binReader.ReadString();
                mangas.Add(manga);
            }
        }

        private static void DeserializeChapters(BinaryReader binReader, Manga parent)
        {
            parent.Chapters = new List<Chapter>();
            int chaptersCount = binReader.ReadInt32();

            for (int i = 0; i < chaptersCount; i++)
            {
                var chapter = new Chapter(parent);
                chapter.Id = binReader.ReadSingle();
                chapter.Title = binReader.ReadString();

                DeserializePages(binReader, chapter);

                chapter.Address = binReader.ReadString();
                chapter.IsComplete = chapter.Pages.All(item => item.Saved);
                var savedPages = chapter.Pages.FindAll(item => item.Saved).Count();
                chapter.Progress = (byte)Math.Floor((savedPages / (float)chapter.Pages.Count) * 100d);
                parent.Chapters.Add(chapter);
            }
        }

        private static void DeserializePages(BinaryReader binReader, Chapter parent)
        {
            parent.Pages = new List<Page>();
            int pagesCount = binReader.ReadInt32();

            for (int i = 0; i < pagesCount; i++)
            {
                var page = new Page(parent);
                page.Saved = binReader.ReadBoolean();
                page.Address = binReader.ReadString();
                parent.Pages.Add(page);
            }
        }
    }
}
