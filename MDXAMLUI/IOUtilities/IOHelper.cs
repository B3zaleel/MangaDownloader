using MDXAMLUI.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace MDXAMLUI.IOUtilities;

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
        var archiverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Libs", "7za.exe");
        if (!File.Exists(archiverPath))
            archiverPath = "C:\\Program Files\\7-Zip\\7z.exe";
        if (!File.Exists(archiverPath))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var evt = new NotificationEventArgs("Archiver doesn't exist", NotificationType.Error);
                Application.Current.MainWindow.RaiseEvent(evt);
            });
            return;
        }
        switch (chapter.Parent.BookFormat)
        {
            case BookFormats.cbz:
                {

                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = archiverPath,
                        Arguments = $" a -tzip \"{chapterFile}\" \"{chapterDir}\\*\"",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    });
                    break;
                }
            case BookFormats.cb7:
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = archiverPath,
                        Arguments = $" a -t7z -bb3 -mx9 \"{chapterFile}\" \"{chapterDir}\\*\"",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    });
                    break;
                }
            case BookFormats.none:
                break;
            default:
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var evt = new NotificationEventArgs("The selected format is currently unavailable.", NotificationType.Error);
                        Application.Current.MainWindow.RaiseEvent(evt);
                    });
                    break;
                }
        }
    }

    public static void UnarchiveChapter(string rootDir, Chapter chapter)
    {
        var chapterDir = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{chapter.Id}";
        var chapterFile = $"{rootDir}\\{ValidateFileName(chapter.Parent.Title)}\\{chapter.Id}.{chapter.Parent.BookFormat}";
        if (!File.Exists(chapterFile))
            return;
        var archiverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Libs", "7za.exe");
        if (!File.Exists(archiverPath))
            archiverPath = "C:\\Program Files\\7-Zip\\7z.exe";
        if (!File.Exists(archiverPath))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var evt1 = new NotificationEventArgs("Archiver doesn't exist", NotificationType.Error);
                Application.Current.MainWindow.RaiseEvent(evt1);
            });
            return;
        }
        switch (chapter.Parent.BookFormat)
        {
            case BookFormats.cbz:
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = archiverPath,
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
                        FileName = archiverPath,
                        Arguments = $" e -t7z \"{chapterFile}\" \"{chapterDir}\\*\"",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    });
                    break;
                }
            case BookFormats.none:
                break;
            default:
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var evt = new NotificationEventArgs("The selected format is currently unavailable.", NotificationType.Error);
                        Application.Current.MainWindow.RaiseEvent(evt);
                    });
                    break;
                }
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
