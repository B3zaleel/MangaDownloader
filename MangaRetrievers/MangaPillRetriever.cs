using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace MangaDownloader.MangaRetrievers
{
    public class MangaPillRetriever : IMangaRetriever
    {
        public string RetrieverName => "MangaPill";
        public string HomePage => "https://mangapill.com";


        public void FetchManga(List<Manga> mangas, string mangaURL)
        {
            var manga = new Manga();
            var webClient = new WebClient();
            var htmlDoc = new XmlDocument();
            string xml;
            try
            {
                xml = webClient.DownloadString(mangaURL);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No Internet Access: \n {ex.Message}");
                return;
            }
            htmlDoc.LoadXml(XMLHelper.SanitizeHTML(xml));
            var container = htmlDoc.DocumentElement.GetElementsByClassName("container")[1];
            var mangaInfoImg = container.GetElementsByTagName("div").OfType<XmlElement>().ToList()[0];
            var coverImgURL = mangaInfoImg.GetElementsByTagName("img").OfType<XmlElement>().ToList()[0].GetAttribute("data-src");
            var coverImgType = (ImageTypes)Enum.Parse(typeof(ImageTypes), coverImgURL.Split('.').Last().ToUpper());
            byte[] coverImgData = null;
            try
            {
                coverImgData = webClient.DownloadData(coverImgURL);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download cover image: \n {ex.Message}");
            }
            manga.Cover = coverImgData != null
                ? new CoverImage()
                {
                    Type = coverImgType,
                    Data = coverImgData,
                    Address = coverImgURL
                }
                : new CoverImage()
                {
                    Type = ImageTypes.None,
                    Data = coverImgData,
                    Address = coverImgURL
                };
            manga.Title = container.GetElementsByTagName("div").OfType<XmlElement>().ToList()[0].GetElementsByTagName("div").OfType<XmlElement>().ToList()[1].GetElementsByTagName("h1")[0].InnerText;
            manga.Description = MangaRetrieverHelper.CleanUpText(container.GetElementsByTagName("div").OfType<XmlElement>().ToList()[0].GetElementsByTagName("div").OfType<XmlElement>().ToList()[1].GetElementsByTagName("p")[0].InnerText);
            var genreElements = container.GetElementsByTagName("div").OfType<XmlElement>().ToList()[0].GetElementsByTagName("div").OfType<XmlElement>().ToList()[1].GetElementsByTagName("a");
            manga.Genres = new string[genreElements.Count];
            for (int i = 0; i < genreElements.Count; i++)
            {
                manga.Genres[i] = genreElements[i].InnerText;
            }
            // Other props
            var otherPropsElements = container.GetNthElement(1).ChildNodes.OfType<XmlElement>().ToList();
            manga.OtherProps = new Dictionary<string, string>(otherPropsElements.Count);
            foreach (var otherPropsElement in otherPropsElements)
            {
                var headerList = otherPropsElement.GetElementsByTagName("h5").OfType<XmlElement>();
                var valueList = otherPropsElement.GetElementsByTagName("div").OfType<XmlElement>();

                if (headerList.Count() > 0 && valueList.Count() > 0)
                {
                    manga.OtherProps.Add(headerList.First().InnerText.Trim(), valueList.First().InnerText.Trim());
                }
            }
            manga.Chapters = new List<Chapter>();
            manga.RetrieverName = RetrieverName;
            manga.Address = mangaURL;

            var chapterElements = container.GetNthElement(3).GetElementsByTagName("a").OfType<XmlElement>().ToList();
            manga.ChaptersCount = chapterElements.Count;
            for (int i = 0; i < Math.Min(chapterElements.Count, MainWindow.MinChaptersToGet); i++)
            {
                var chapter = FetchChapter(manga, $"{HomePage}{chapterElements[i].GetAttribute("href")}");
                manga.Chapters.Add(chapter);
            }

            mangas.Add(manga);
        }

        public Chapter FetchChapter(Manga parent, string chapterURL)
        {
            var chapter = new Chapter(parent);
            var webClient = new WebClient();
            var htmlDoc = new XmlDocument();
            string xml;
            try
            {
                xml = webClient.DownloadString(chapterURL);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No Internet Access: \n {ex.Message}");
                return null;
            }
            htmlDoc.LoadXml(XMLHelper.SanitizeHTML(xml));
            var container = htmlDoc.DocumentElement.GetElementsByClassName("container")[1];
            var selectedOption = container.GetElementsByTagName("select").OfType<XmlElement>().ToList()[0].GetElementsByTagName("option").OfType<XmlElement>().ToList().Find(option => option.HasAttribute("selected"));

            var titleExpression = new Regex("[0-9]+");
            var numsArr = titleExpression.Matches(selectedOption.InnerText);

            var urlParts = chapterURL.Split('/');
            chapter.Id = float.Parse(urlParts[urlParts.Length - 2].Split('-')[1]);
            chapter.Title = selectedOption.InnerText;
            var pageElements = container.GetElementsByClassName("text-color-alert-warn-text").Count > 1
                ? container.GetNthElement(4).GetElementsByTagName("img").OfType<XmlElement>().ToList()
                : container.GetNthElement(3).GetElementsByTagName("img").OfType<XmlElement>().ToList();
            chapter.Pages = new List<Page>();

            for (int i = 0; i < pageElements.Count; i++)
            {
                chapter.Pages.Add(new Page(chapter)
                {
                    Address = pageElements[i].GetAttribute("data-src"),
                    Saved = false,
                });
            }

            chapter.Address = chapterURL;
            chapter.IsComplete = chapter.Pages.All(item => item.Saved);
            //Debug.WriteLine($">> {chapter.Id}, {chapter.Pages.FindAll(item => item.Saved).Count()}, {chapter.Pages.Count}");
            chapter.Progress = chapter.Pages.Count == 0 ? (byte)100 : (byte)Math.Floor((double)chapter.Pages.FindAll(item => item.Saved).Count() / chapter.Pages.Count);

            return chapter;
        }

        public void UpdateChapters(Manga parent)
        {
            var updatedChapters = new List<Chapter>();
            var webClient = new WebClient();
            var htmlDoc = new XmlDocument();
            string xml;
            try
            {
                xml = webClient.DownloadString(parent.Address);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No Internet Access: \n {ex.Message}");
                return;
            }
            htmlDoc.LoadXml(XMLHelper.SanitizeHTML(xml));
            Debug.WriteLine("Update Started");
            var container = htmlDoc.DocumentElement.GetElementsByClassName("container")[1];
            var chapterElements = container.GetNthElement(3).GetElementsByTagName("a").OfType<XmlElement>().ToList();
            var previousChapterTitles = parent.Chapters.Select(item => item.Title).ToList();
            //chapterElements.Count
            for (int i = 0; i < chapterElements.Count; i++)
            {
                var chapter = FetchChapter(parent, $"{HomePage}{chapterElements[i].GetAttribute("href")}");
                var isOld = previousChapterTitles.Contains(chapter.Title);
                if (!isOld)
                {
                    parent.MergeChapter(chapter);
                }
            }
            Debug.WriteLine("Update Ended");
        }

    }
}
