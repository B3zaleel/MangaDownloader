using MangaDownloader.Events;
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


        public void FetchManga(List<Manga> mangas, string url)
        {
            var manga = new Manga();
            var webClient = new WebClient();
            var htmlDoc = new XmlDocument();
            string xml;
            try
            {
                xml = webClient.DownloadString(url);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var evt = new NotificationEventArgs($"No Internet Access: \n {ex.Message}", NotificationType.Error);
                    Application.Current.MainWindow.RaiseEvent(evt);
                });
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var evt = new NotificationEventArgs($"Failed to download cover image: \n\t{ex.Message}", NotificationType.Error);
                    Application.Current.MainWindow.RaiseEvent(evt);
                });
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
            manga.Address = url;

            var chapterElements = container.GetNthElement(1).GetElementsByTagName("a").OfType<XmlElement>().ToList();
            manga.ChaptersCount = chapterElements.Count;
            for (int i = 0; i < Math.Min(chapterElements.Count, MainWindow.MinChaptersToGet); i++)
            {
                var chapter = FetchChapter(
                    manga, 
                    $"{HomePage}{chapterElements[i].GetAttribute("href")}",
                    chapterElements[i].InnerText.Trim()
                );
                manga.Chapters.Add(chapter);
            }

            mangas.Add(manga);
        }

        public Chapter FetchChapter(Manga parent, string url, string name)
        {
            var chapter = new Chapter(parent);
            var webClient = new WebClient();
            var htmlDoc = new XmlDocument();
            string xml;
            try
            {
                xml = webClient.DownloadString(url);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var evt = new NotificationEventArgs($"No Internet Access: \n\t{ex.Message}", NotificationType.Error);
                    Application.Current.MainWindow.RaiseEvent(evt);
                });
                return null;
            }
            htmlDoc.LoadXml(XMLHelper.SanitizeHTML(xml));
            var urlParts = url.Split('/');
            chapter.Id = float.Parse(urlParts[urlParts.Length - 2].Split('-')[1]);
            chapter.Title = name;
            var chapterPages = htmlDoc.DocumentElement.GetElementsByTagName("chapter-page");
            chapter.Pages = new List<Page>();
            for (int i = 0; i < chapterPages.Count; i++)
            {
                var pageURLSrc = ((XmlElement)((XmlElement)chapterPages[i]).GetElementsByTagName("img")[0]).GetAttribute("data-src");
                chapter.Pages.Add(new Page(chapter)
                {
                    Address = pageURLSrc,
                    Saved = false,
                });
            }
            chapter.Address = url;
            chapter.IsComplete = chapter.Pages.All(item => item.Saved);
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var evt = new NotificationEventArgs($"No Internet Access: \n\t{ex.Message}", NotificationType.Error);
                    Application.Current.MainWindow.RaiseEvent(evt);
                });
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
                var chapter = FetchChapter(
                    parent, 
                    $"{HomePage}{chapterElements[i].GetAttribute("href")}", 
                    chapterElements[i].InnerText.Trim()
                );
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
