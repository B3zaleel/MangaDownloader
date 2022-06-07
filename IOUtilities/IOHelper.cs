using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace MangaDownloader.IOUtilities
{
    public class IOHelper
    {
        public static string ValidateFileName(string fileName)
        {
            var fileValidation = new Regex("[\\/*:?\"<>|]");

            return fileValidation.Replace(fileName, "_");
        }

        public static void DownloadChapter(string rootDir, Chapter chapter)
        {
            var webClient = new WebClient();
            var dir = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var chapterDir = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{ValidateFileName(chapter.Title)}";
            if (!Directory.Exists(chapterDir))
                Directory.CreateDirectory(chapterDir);

            var errorBuilder = new StringBuilder();
            int pageNo = 1;
            int pagesNotDownloaded = 0;
            Debug.WriteLine("Starting new Download");
            foreach (var page in chapter.Pages)
            {
                var pageFile = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{ValidateFileName(chapter.Title)}\\{page.Address.Split('/').Last()}";
                if (!File.Exists(pageFile))
                {
                    try
                    {
                        webClient.DownloadFile(page.Address, pageFile);
                        page.Saved = true;
                    }
                    catch (Exception)
                    {
                        pagesNotDownloaded++;
                        page.Saved = false;
                    }
                }
                else
                    page.Saved = true;
                chapter.IsComplete = chapter.Pages.All(item => item.Saved);
                var savedPages = chapter.Pages.FindAll(item => item.Saved).Count();
                chapter.Progress = (byte)Math.Floor((savedPages / (float)chapter.Pages.Count) * 100d);
                pageNo++;
            }

            if (pagesNotDownloaded > 0)
            {
                throw new NotificationError($"{pagesNotDownloaded} / {chapter.Pages.Count} downloads failed.", NotificationType.Error);
            }
        }

        public static void ArchiveChapter(string rootDir, Chapter chapter)
        {
            var chapterDir = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{ValidateFileName(chapter.Title)}";
            if (!Directory.Exists(chapterDir))
                return;
            var chapterFile = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{ValidateFileName(chapter.Title)}.{chapter.Parent.BookFormat}";

            switch (chapter.Parent.BookFormat)
            {
                case BookFormats.cbz:
                    {
                        //Process.Start("C:\\Program Files\\7-Zip\\7z.exe", $" a -tzip \"{chapterFile}\" \"{chapterDir}\\*\"");
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "C:\\Program Files\\7-Zip\\7z.exe",
                            Arguments = $" a -tzip \"{chapterFile}\" \"{chapterDir}\\*\"",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        });
                        break;
                    }
                case BookFormats.cb7:
                    {
                        //Process.Start("C:\\Program Files\\7-Zip\\7z.exe", $" a -t7z -bb3 -mx9 \"{chapterFile}\" \"{chapterDir}\\*\"");
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "C:\\Program Files\\7-Zip\\7z.exe",
                            Arguments = $" a -t7z -bb3 -mx9 \"{chapterFile}\" \"{chapterDir}\\*\"",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        });
                        break;
                    }
                case BookFormats.none:
                    break;
                default:
                    MessageBox.Show("The format is currently unavailable.");
                    break;
            }
        }

        public static void UnarchiveChapter(string rootDir, Chapter chapter)
        {
            var chapterDir = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{chapter.Id}";
            var chapterFile = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{chapter.Id}.{chapter.Parent.BookFormat}";
            if (!File.Exists(chapterFile))
                return;
            switch (chapter.Parent.BookFormat)
            {
                case BookFormats.cbz:
                    {
                        //Process.Start("C:\\Program Files\\7-Zip\\7z.exe", $" e -tzip \"{chapterFile}\" \"{chapterDir}\\*\"");
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "C:/Program Files/7-Zip/7z.exe",
                            Arguments = $" e -tzip \"{chapterFile}\" \"{chapterDir}\\*\"",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        });
                        break;
                    }
                case BookFormats.cb7:
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "C:/Program Files/7-Zip/7z.exe",
                            Arguments = $" e -t7z \"{chapterFile}\" \"{chapterDir}\\*\"",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        });
                        break;
                    }
                case BookFormats.none:
                    break;
                default:
                    MessageBox.Show("The format is currently unavailable.");
                    break;
            }
        }

        public static void DownloadImage(CoverImage coverImage)
        {
            if (coverImage != null)
            {
                var webClient = new WebClient();
                try
                {
                    coverImage.Data = webClient.DownloadData(coverImage.Address);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

    }
}
