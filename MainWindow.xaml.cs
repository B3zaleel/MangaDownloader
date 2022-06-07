using MangaDownloader.IOUtilities;
using MangaDownloader.MangaRetrievers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Shell;
using System.Text.RegularExpressions;
using System.Net;
using System.Timers;

namespace MangaDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            //Single instance functionality implemented below
            var currentProccess = Process.GetCurrentProcess();

            var procByName = Process.GetProcessesByName(currentProccess.ProcessName);
            if (procByName.Length > 1)
            {
                App.Current.Shutdown();
            }

            BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            SettingsFilePath = "";
            Mangas = new List<Manga>();
            IOOperationOngoing = false;
            MangaRetrievers = new IMangaRetriever[]
            {
                new MangaPillRetriever(),
                new MangakaKalotRetriever(),
                new MangakaKalotRetriever("MangaNelo", "https://manganelo.com/"),
                new MangaEnRetriever(),
            };

            MangaRetrieverNames = (from MangaRetriever in MangaRetrievers select MangaRetriever.RetrieverName).ToList();
            InitializeComponent();
            DataContext = this;
            bookFormatComboBox.ItemsSource = Enum.GetValues(typeof(BookFormats));
            bookFormatComboBox.SelectedIndex = 0;
            changeFolderBtn.ToolTip = BaseDir;
            var networkStatusTimer = new System.Timers.Timer(3000)
            {
                Enabled = true,
                AutoReset = true,
            };
            networkStatusTimer.Elapsed += UpdateNetworkStatus_Event;
            statusNotifSnackbar.MessageQueue = new MaterialDesignThemes.Wpf.SnackbarMessageQueue();
        }

        public string BaseDir { get; set; }
        public string SettingsFilePath { get; set; }

        public List<Manga> Mangas { get; set; }

        public List<Chapter> MangaChapters
        {
            get
            {
                if (mangaListBox.SelectedIndex > -1 && mangaListBox.SelectedIndex < mangaListBox.Items.Count)
                {

                    return Mangas[mangaListBox.SelectedIndex].Chapters;
                }
                else
                {
                    return new List<Chapter>();
                }
            }
        }

        public int BookFormatSelectedIndex
        {
            get
            {
                if (mangaListBox.SelectedIndex > -1 && mangaListBox.SelectedIndex < mangaListBox.Items.Count)
                {
                    var selectedManga = Mangas[mangaListBox.SelectedIndex];
                    if (selectedManga.IsAvailable)
                    {
                        return ((BookFormats[])bookFormatComboBox.ItemsSource).ToList().FindIndex(item => item == selectedManga.BookFormat);
                    }
                    else
                        return 0;
                }
                else
                    return 0;
            }
            set
            {
                if (bookFormatComboBox.SelectedIndex > -1 && bookFormatComboBox.SelectedIndex < bookFormatComboBox.Items.Count && value > -1)
                {
                    var selectedManga = Mangas[mangaListBox.SelectedIndex];
                    selectedManga.BookFormat = (BookFormats)Enum.GetValues(typeof(BookFormats)).GetValue(value);
                }
            }
        }

        private bool IOOperationOngoing { get; set; }
        public bool IsUpdateEnabled
        {
            get
            {
                if (mangaListBox.SelectedIndex > -1 && mangaListBox.SelectedIndex < mangaListBox.Items.Count)
                {
                    var selectedManga = Mangas[mangaListBox.SelectedIndex];
                    return !selectedManga.IsUpdatingChapters && !IOOperationOngoing;
                }
                else
                    return true && !IOOperationOngoing;
            }
        }

        public static readonly byte MinChaptersToGet = 10;
        public static readonly TimeSpan DayOpening = new TimeSpan(6, 30, 0);
        public static readonly TimeSpan DayClosing = new TimeSpan(19, 15, 0);

        public IMangaRetriever[] MangaRetrievers { get; }
        private List<string> MangaRetrieverNames { get; }


        #region Event Handlers

        #region Manga Events

        private void MangaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnPropertyChanged("MangaChapters");
            OnPropertyChanged("BookFormatSelectedIndex");
            OnPropertyChanged("IsUpdateEnabled");
            OnPropertyChanged("MaxChaptersWinSize");
        }

        private void ViewMangaInfo_Click(object sender, RoutedEventArgs args)
        {
            var mangaInfoWindow = new MangaInfoWindow
            {
                Manga = (Manga)mangaListBox.SelectedItem,
                Owner = this
            };
            mangaInfoWindow.ShowDialog();
        }

        private async void AddManga_Click(object sender, RoutedEventArgs args)
        {
            var searchMangaDialog = new SearchMangaDialog
            {
                URL = "",
                RetrieverNames = MangaRetrieverNames.ToArray(),
                SelectedRetrieverIndex = 0,
                Owner = this
            };
            searchMangaDialog.ShowDialog();

            if (searchMangaDialog.DialogResult == true)
            {
                var txt = searchMangaDialog.URL;
                var isMangaNew = Mangas.All(item => !item.Address.Equals(txt));

                Debug.WriteLine($"New Manga: {isMangaNew}");
                if (isMangaNew)
                {
                    var loadingManga = new Manga()
                    {
                        IsAvailable = false,
                        Address = txt,
                    };
                    Mangas.Add(loadingManga);
                    OnPropertyChanged("Mangas");
                    mangaListBox.Items.Refresh();
                    var retriever = MangaRetrievers[searchMangaDialog.SelectedRetrieverIndex];
                    await Task.Factory.StartNew(() => retriever.FetchManga(Mangas, txt), TaskCreationOptions.AttachedToParent);
                    Mangas.Remove(loadingManga);
                    OnPropertyChanged("Mangas");
                    mangaListBox.Items.Refresh();
                }
            }
            else
                Debug.WriteLine("cancelled request");
        }

        private void MoveMangaUp_Click(object sender, RoutedEventArgs args)
        {
            var manga = ((Manga)((Grid)((Grid)((Button)args.Source).Parent).Parent).DataContext);
            MoveMangaUp(manga);
        }

        private void MoveMangaUp_KeyDown(object sender, KeyEventArgs args)
        {
            var manga = (Manga)mangaListBox.SelectedItem;

            if (manga != null)
            {
                if (args.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    MoveMangaUp(manga);
                    Debug.WriteLine("KEY repositioning up");
                }
                else if (args.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    MoveMangaDown(manga);
                    Debug.WriteLine("KEY repositioning down");
                }

                //args.Handled = true;
            }
        }

        private void MoveMangaDown_Click(object sender, RoutedEventArgs args)
        {
            var manga = ((Manga)((Grid)((Grid)((Button)args.Source).Parent).Parent).DataContext);
            MoveMangaDown(manga);
        }

        private void MoveMangaUp(Manga manga)
        {
            int selectedIdx = mangaListBox.SelectedIndex;
            int previousMangaIdx = -1;
            int mangaIdx = 0;

            for (int i = 0; i < Mangas.Count; i++)
            {
                if (Mangas[i].Title.Equals(manga.Title))
                {
                    mangaIdx = i;
                    previousMangaIdx = i - 1;
                }
            }
            if (mangaIdx > 0)
            {
                // move up
                var previousManga = Mangas[previousMangaIdx];
                Mangas[previousMangaIdx] = manga;
                Mangas[mangaIdx] = previousManga;
                OnPropertyChanged("Mangas");
                mangaListBox.Items.Refresh();
                if (mangaIdx == selectedIdx)
                {
                    mangaListBox.SelectedIndex = previousMangaIdx;
                }
            }
        }

        private void MoveMangaDown(Manga manga)
        {
            int selectedIdx = mangaListBox.SelectedIndex;
            int nextMangaIdx = 1;
            int mangaIdx = 0;

            for (int i = 0; i < Mangas.Count; i++)
            {
                if (Mangas[i].Title.Equals(manga.Title))
                {
                    mangaIdx = i;
                    nextMangaIdx = i + 1;
                }
            }
            if (mangaIdx < Mangas.Count - 2)
            {
                // move down
                var nextManga = Mangas[nextMangaIdx];
                Mangas[nextMangaIdx] = manga;
                Mangas[mangaIdx] = nextManga;
                OnPropertyChanged("Mangas");
                mangaListBox.Items.Refresh();
                if (mangaIdx == selectedIdx)
                {
                    mangaListBox.SelectedIndex = nextMangaIdx;
                }
            }
        }

        private void RemoveManga_Click(object sender, RoutedEventArgs args)
        {
            if (mangaListBox.SelectedIndex > -1 && mangaListBox.SelectedIndex < mangaListBox.Items.Count)
            {
                Mangas.RemoveAt(mangaListBox.SelectedIndex);
                OnPropertyChanged("Mangas");
                mangaListBox.Items.Refresh();
            }
        }

        private void ShowMangaInExplorer_Click(object sender, RoutedEventArgs args)
        {
            //var mangaAddress = ((ContextMenu)((MenuItem)sender).Parent).Tag.ToString();
            var manga = Mangas[mangaListBox.SelectedIndex];
            var mangaDir = $"{BaseDir}\\{IOHelper.ValidateFileName(manga.Title)}";

            if (!Directory.Exists(mangaDir))
                Directory.CreateDirectory(mangaDir);
            Process.Start($"explorer", $" /select,\"{mangaDir}\"");
        }

        private async void DownloadAllMangaChapters_Click(object sender, RoutedEventArgs args)
        {
            var chapters = Mangas[mangaListBox.SelectedIndex].Chapters.ToList();
            for (int i = chapters.Count - 1; i > -1; i--)
            {
                var chapter = chapters[i];
                var filesAavailable = chapter.Parent.BookFormat == BookFormats.none
                ? chapter.Pages.All(page => File.Exists($"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}\\{page.Address.Split('/').Last()}"))
                : File.Exists($"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}.{chapter.Parent.BookFormat}");
                if (!chapter.IsComplete && !filesAavailable)
                {
                    try
                    {
                        await Task.Factory.StartNew(() => IOHelper.DownloadChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                        SendNotification($"Downloaded {chapter.Parent.Title} \u226B {chapter.Title}", NotificationType.FinishedTask);
                    }
                    catch (NotificationError ne)
                    {
                        SendNotification($"Incomplete download {chapter.Parent.Title} \u226B {chapter.Title}\n {ne.Notification}", ne.Type);
                    }

                }
            }
        }

        private async void ArchiveAllMangaChapters_Click(object sender, RoutedEventArgs args)
        {
            for (int i = Mangas[mangaListBox.SelectedIndex].Chapters.ToList().Count - 1; i > -1; i--)
            {
                var chapter = Mangas[mangaListBox.SelectedIndex].Chapters.ToList()[i];
                if (!chapter.IsArchiving && chapter.IsComplete)
                {
                    chapter.IsArchiving = true;

                    try
                    {
                        await Task.Factory.StartNew(() => IOHelper.ArchiveChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                        SendNotification($"Archived {chapter.Parent.Title} \u226B {chapter.Title}", NotificationType.FinishedTask);
                    }
                    catch (NotificationError ne)
                    {
                        SendNotification($"Failed archive {chapter.Parent.Title} \u226B {chapter.Title}\n{ne.Notification}", ne.Type);
                    }

                    chapter.IsArchiving = false;

                }
            }
        }

        private async void LoadMangaCoverImage_Click(object sender, RoutedEventArgs args)
        {
            var selectedManga = Mangas[mangaListBox.SelectedIndex];
            await Task.Factory.StartNew(() =>
            {
                IOHelper.DownloadImage(selectedManga.Cover);
            });
        }

        private void SelectedManga_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                OnPropertyChanged("MangaChapters");
                chapterListView.Items.Refresh();
            });
        }

        #endregion

        private async void DownloadChapter_Click(object sender, RoutedEventArgs args)
        {
            var chapterId = float.Parse(((ContextMenu)((MenuItem)sender).Parent).Tag.ToString());
            var chapter = Mangas[mangaListBox.SelectedIndex].Chapters.ToList().Find(item => item.Id == chapterId);
            var filesAavailable = chapter.Parent.BookFormat == BookFormats.none
                ? chapter.Pages.All(page => File.Exists($"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}\\{page.Address.Split('/').Last()}"))
                : File.Exists($"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}.{chapter.Parent.BookFormat}");
            if (!chapter.IsComplete && !filesAavailable)
            {
                try
                {
                    await Task.Factory.StartNew(() => IOHelper.DownloadChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                    SendNotification($"Downloaded {chapter.Parent.Title} \u226B {chapter.Title}", NotificationType.FinishedTask);
                }
                catch (NotificationError ne)
                {
                    SendNotification(ne.Notification, ne.Type);
                }

            }
        }

        private async void ArchiveChapter_Click(object sender, RoutedEventArgs args)
        {
            var chapterId = float.Parse(((ContextMenu)((MenuItem)sender).Parent).Tag.ToString());
            var chapter = Mangas[mangaListBox.SelectedIndex].Chapters.ToList().Find(item => item.Id == chapterId);

            if (!chapter.IsArchiving && chapter.IsComplete)
            {
                chapter.IsArchiving = true;

                await Task.Factory.StartNew(() => IOHelper.ArchiveChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                chapter.IsArchiving = false;
                SendNotification($"Archived {chapter.Parent.Title} \u226B {chapter.Title}", NotificationType.FinishedTask);
            }
        }

        private async void UnarchiveChapter_Click(object sender, RoutedEventArgs args)
        {
            var chapterId = float.Parse(((ContextMenu)((MenuItem)sender).Parent).Tag.ToString());
            var chapter = Mangas[mangaListBox.SelectedIndex].Chapters.ToList().Find(item => item.Id == chapterId);

            if (!chapter.IsUnarchiving)
            {
                chapter.IsUnarchiving = true;

                await Task.Factory.StartNew(() => IOHelper.UnarchiveChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                //((MenuItem)sender).Tag = "0";
                chapter.IsUnarchiving = false;
                SendNotification($"Unarchived {chapter.Parent.Title} \u226B {chapter.Title}", NotificationType.FinishedTask);
            }
        }

        private void OpenChapter_Click(object sender, RoutedEventArgs args)
        {
            var chapterId = float.Parse(((ContextMenu)((MenuItem)sender).Parent).Tag.ToString());
            var chapter = Mangas[mangaListBox.SelectedIndex].Chapters.ToList().Find(item => item.Id == chapterId);
            var chapterDir = $"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}";
            if (!Directory.Exists(chapterDir) && chapter.Parent.BookFormat == BookFormats.none)
            {
                MessageBox.Show("Folder does not exist");
                return;
            }
            var chapterFile = $"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}.{chapter.Parent.BookFormat}";
            if (!File.Exists(chapterFile))
            {
                MessageBox.Show("Archive does not exist");
                return;
            }

            if (File.Exists(chapterFile))
                Process.Start($"explorer", $" \"{chapterFile}\"");
            else
                MessageBox.Show(this, "The chapter hasn't been saved yet.");
        }


        private void ShowChapterInExplorer_Click(object sender, RoutedEventArgs args)
        {
            //var chapterId = float.Parse(((ContextMenu)((MenuItem)sender).Parent).Tag.ToString());
            var chapter = Mangas[mangaListBox.SelectedIndex].Chapters[chapterListView.SelectedIndex];
            var chapterDir = $"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}";
            var chapterFile = $"{BaseDir}\\{IOHelper.ValidateFileName(chapter.Parent.Title)}\\{IOHelper.ValidateFileName(chapter.Title)}.{chapter.Parent.BookFormat}";

            //Debug.WriteLine(chapterFile);
            //Debug.WriteLine(chapterDir);
            if (File.Exists(chapterFile))
                Process.Start($"explorer", $" /select,\"{chapterFile}\"");
            else if (Directory.Exists(chapterDir))
                Process.Start($"explorer", $" /select,\"{chapterDir}\"");
            else
                MessageBox.Show("The file hasn't been saved yet.");
        }

        private void RemoveChapter_Click(object sender, RoutedEventArgs args)
        {
            if (chapterListView.SelectedItem != null)
            {
                var selectedManga = Mangas[mangaListBox.SelectedIndex];
                var chapter = ((Chapter)chapterListView.SelectedItem);

                selectedManga.Chapters.Remove(chapter);
                selectedManga.ChaptersCount--;
                OnPropertyChanged("MangaChapters");
                chapterListView.Items.Refresh();
            }
        }

        private async void UpdateChapters_Click(object sender, RoutedEventArgs args)
        {
            if (mangaListBox.SelectedIndex > -1 && mangaListBox.SelectedIndex < mangaListBox.Items.Count)
            {
                var selectedManga = Mangas[mangaListBox.SelectedIndex];
                if (!selectedManga.IsAvailable)
                    return;
                var retrieverIndex = MangaRetrieverNames.FindIndex(item => item.Equals(selectedManga.RetrieverName));
                Debug.WriteLine(retrieverIndex);
                var retriever = MangaRetrievers[retrieverIndex];
                selectedManga.IsUpdatingChapters = true;
                OnPropertyChanged("IsUpdateEnabled");
                selectedManga.PropertyChanged += SelectedManga_PropertyChanged;

                await Task.Factory.StartNew(() =>
                    retriever.UpdateChapters(selectedManga),
                    TaskCreationOptions.PreferFairness);
                Debug.WriteLine("UpdateChapters_Click...");
                Debug.WriteLine($"fin: {selectedManga.Chapters.Count}");
                Debug.WriteLine($"fin2: {chapterListView.Items.Count}");
                selectedManga.IsUpdatingChapters = false;
                OnPropertyChanged("IsUpdateEnabled");
            }
        }

        private async void ChapterListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                Debug.WriteLine($"Downloading {chapterListView.SelectedItems.Count} chapters");
                var chapters = chapterListView.SelectedItems.OfType<Chapter>().ToList();
                int i = 0;
                while (i < chapters.Count)
                {
                    var chapter = chapters[i];
                    //var chapterId = float.Parse(((ContextMenu)((MenuItem)sender).Parent).Tag.ToString());

                    await Task.Factory.StartNew(() => IOHelper.DownloadChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                    i++;
                }
            }
            else if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                Debug.WriteLine("Selecting all chapters");
                chapterListView.SelectAll();

            }
            else if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                Debug.WriteLine($"Archiving {chapterListView.SelectedItems.Count} chapters");
                var chapters = chapterListView.SelectedItems.OfType<Chapter>();
                foreach (var chapter in chapters)
                {
                    if (!chapter.IsArchiving)
                    {
                        chapter.IsArchiving = true;

                        await Task.Factory.StartNew(() => IOHelper.ArchiveChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                        chapter.IsArchiving = false;
                    }
                }
            }
            else if (e.Key == Key.U && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                Debug.WriteLine($"Unarchiving {chapterListView.SelectedItems.Count} chapters");
                var chapters = chapterListView.SelectedItems.OfType<Chapter>();
                foreach (var chapter in chapters)
                {
                    if (!chapter.IsUnarchiving)
                    {
                        chapter.IsUnarchiving = true;

                        await Task.Factory.StartNew(() => IOHelper.UnarchiveChapter(BaseDir, chapter), TaskCreationOptions.AttachedToParent);
                        //((MenuItem)sender).Tag = "0";
                        chapter.IsUnarchiving = false;
                    }
                }
            }
            else if (e.Key == Key.Delete)
            {
                Debug.WriteLine($"Deleting {chapterListView.SelectedItems.Count} chapters");
                var chapters = chapterListView.SelectedItems.OfType<Chapter>().ToList();
                var selectedChapters = chapterListView.SelectedItems.OfType<Chapter>().ToList();
                var selectedManga = Mangas[mangaListBox.SelectedIndex];

                foreach (var chapter in selectedChapters)
                {
                    chapters.Remove(chapter);
                    selectedManga.ChaptersCount--;
                    OnPropertyChanged("MangaChapters");
                }
            }
            else
            {
                Debug.WriteLine($"{e.KeyboardDevice.Modifiers} and {e.Key}");
            }
        }

        private void ChapterListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                chapterListView.SelectionMode = SelectionMode.Multiple;
            }
            else
            {
                chapterListView.SelectionMode = SelectionMode.Single;
            }
        }


        #endregion

        private async void UpdateNetworkStatus_Event(object sender, ElapsedEventArgs args)
        {
            await Task.Factory.StartNew(() =>
            {
                var connectionActive = true;
                try
                {
                    using (var client = new WebClient())
                    using (client.OpenRead("http://google.com/generate_204"))
                    {
                        connectionActive = true;
                    }
                }
                catch
                {
                    connectionActive = false;
                }
                Dispatcher.Invoke(() =>
                {
                    if (connectionActive)
                    {
                        networkStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.NetworkStrength4;
                        networkStatus.SetValue(ForegroundProperty, new SolidColorBrush(Colors.Green));
                    }
                    else
                    {
                        networkStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.NetworkStrength0;
                        networkStatus.SetValue(ForegroundProperty, new SolidColorBrush(Colors.Red));
                    }
                });
            }, TaskCreationOptions.AttachedToParent);
        }

        public async void OpenFolder_Click(object obj, RoutedEventArgs args)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = "F:\\Projects\\Tests\\Manga"
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await Task.Factory.StartNew(() => OpenFolder(folderDialog.SelectedPath));
            }
        }

        public async void OpenFile_Click(object obj, RoutedEventArgs args)
        {
            var fileDialog = new OpenFileDialog()
            {
                InitialDirectory = "F:\\Projects\\Tests\\Manga",
                Filter = "All (*.min.json;*.json;*.mdbin)|*.min.json;*.json;*.mdbin|Minified JSON (*.min.json)|*.min.json|JSON (*.json)|*.json|MD Bin (*.mdbin)|*.mdbin"
            };

            if (fileDialog.ShowDialog() == true)
            {
                await Task.Factory.StartNew(() => OpenFile(fileDialog.FileName));
            }
        }

        public void OpenFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                statusNotifSnackbar.MessageQueue.Enqueue($"Cannot find such folder: {folder}");
                return;
            }
            BaseDir = folder;
            var settingsPaths = new[]{
                        $"{BaseDir}\\settings.json",
                        $"{BaseDir}\\settings.min.json",
                        $"{BaseDir}\\settings.mdbin",
                    };
            if (File.Exists(settingsPaths[0]))
            {
                SettingsFilePath = settingsPaths[0];
                Mangas = MangaDownloaderSettings.Deserialize(settingsPaths[0], SerializationType.JSON);
            }
            else if (File.Exists(settingsPaths[1]))
            {
                SettingsFilePath = settingsPaths[1];
                Mangas = MangaDownloaderSettings.Deserialize(settingsPaths[1], SerializationType.JSONMinified);
            }
            else if (File.Exists(settingsPaths[2]))
            {
                SettingsFilePath = settingsPaths[2];
                Mangas = MangaDownloaderSettings.Deserialize(settingsPaths[2], SerializationType.MDBin);
            }
            else
            {
                File.Create(settingsPaths[2]).Close();
                SettingsFilePath = settingsPaths[2];
                Mangas = new List<Manga>();
            }
            Dispatcher.Invoke(() =>
            {
                changeFolderBtn.ToolTip = BaseDir;
                OnPropertyChanged("Mangas");
                mangaListBox.SelectedIndex = Mangas.Count() > 0 ? 0 : -1;
            });
        }

        public void OpenFile(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"Cannot find such file: {file}");
                return;
            }
            BaseDir = System.IO.Path.GetDirectoryName(file);
            if (file.EndsWith(".min.json"))
            {
                SettingsFilePath = file;
                Mangas = MangaDownloaderSettings.Deserialize(file, SerializationType.JSONMinified);
            }
            else if (file.EndsWith(".json"))
            {
                SettingsFilePath = file;
                Mangas = MangaDownloaderSettings.Deserialize(file, SerializationType.JSON);
            }
            else if (file.EndsWith(".mdbin"))
            {
                SettingsFilePath = file;
                Mangas = MangaDownloaderSettings.Deserialize(file, SerializationType.MDBin);
            }
            Dispatcher.Invoke(() =>
            {
                changeFolderBtn.ToolTip = BaseDir;
                OnPropertyChanged("Mangas");
                mangaListBox.SelectedIndex = Mangas.Count() > 0 ? 0 : -1;
                statusNotifSnackbar.MessageQueue.Enqueue("File loaded");
            });
        }

        public async void SaveAndExit_Click(object obj, RoutedEventArgs args)
        {
            var procName = Process.GetCurrentProcess().ProcessName;
            await Task.Factory.StartNew(() => {
                IOOperationOngoing = true;
                OnPropertyChanged("IOOperationOngoing");
                var serializationType = SettingsFilePath.EndsWith(".min.json") ? SerializationType.JSONMinified : SettingsFilePath.EndsWith(".json") ? SerializationType.JSON : SerializationType.MDBin;
                MangaDownloaderSettings.Serialize(SettingsFilePath, Mangas, serializationType);
                Dispatcher.Invoke(() =>
                {
                    SendNotification($"Saved at: {SettingsFilePath}", NotificationType.Success);
                });

                Process.GetProcessesByName(procName)[0].CloseMainWindow();
            });
        }

        public async void Save_Click(object obj, RoutedEventArgs args)
        {
            await Task.Factory.StartNew(() =>
            {
                if (!IOOperationOngoing)
                {
                    IOOperationOngoing = true;
                    OnPropertyChanged("IOOperationOngoing");
                    var serializationType = SettingsFilePath.EndsWith(".min.json") ? SerializationType.JSONMinified : SettingsFilePath.EndsWith(".json") ? SerializationType.JSON : SerializationType.MDBin;
                    MangaDownloaderSettings.Serialize(SettingsFilePath, Mangas, serializationType);
                    Dispatcher.Invoke(() =>
                    {
                        SendNotification($"Saved at: {SettingsFilePath}", NotificationType.Success);
                    });

                    IOOperationOngoing = false;
                    OnPropertyChanged("IOOperationOngoing");
                }
            });
        }

        public async void SaveAs_Click(object obj, RoutedEventArgs args)
        {
            await Task.Factory.StartNew(() =>
            {
                if (!IOOperationOngoing)
                {
                    var saveDialog = new SaveFileDialog()
                    {
                        Title = "Save Manga Downloader Settings",
                        Filter = "JSON (*.json)|*.json|Minified JSON (*.min.json)|*.min.json|MD Binary (*.mdbin)|*.mdbin",
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        Debug.WriteLine($"saveDialog.FilterIndex: {saveDialog.FilterIndex}");
                        IOOperationOngoing = true;
                        OnPropertyChanged("IOOperationOngoing");
                        MangaDownloaderSettings.Serialize($"{saveDialog.FileName}", Mangas, (SerializationType)(saveDialog.FilterIndex - 1));
                        Dispatcher.Invoke(() =>
                        {
                            SendNotification($"Saved at: {saveDialog.FileName}", NotificationType.Success);
                            IOOperationOngoing = false;
                            OnPropertyChanged("IOOperationOngoing");
                        });
                    }
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void SendNotification(string notification, NotificationType notificationType)
        {

            Application.Current.MainWindow.Dispatcher.Invoke(() =>
            {
                var snackbar = ((Grid)Application.Current.MainWindow.Content).Children.OfType<MaterialDesignThemes.Wpf.Snackbar>().ToList()[0];

                switch (notificationType)
                {
                    case NotificationType.FinishedTask:
                        notificationAudioElement.Source = new Uri($"pack://application:,,,/Assets/Sounds/sharp-592.mp3");
                        break;
                    case NotificationType.Success:
                        notificationAudioElement.Source = new Uri($"pack://application:,,,/Assets/Sounds/pristine-609.mp3");
                        break;
                    case NotificationType.Error:
                        notificationAudioElement.Source = new Uri($"pack://application:,,,/Assets/Sounds/system-fault-518.mp3");
                        break;
                    case NotificationType.None:
                    default:
                        break;
                }
                notificationAudioElement.Play();
                snackbar.MessageQueue.Enqueue(notification);
                notificationAudioElement.MediaOpened += (obj, ev) => {
                    snackbar.MessageQueue.Enqueue(notification);
                    MessageBox.Show("Music played");
                };

            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var currentTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Second, DateTime.Now.Millisecond);
            var isDayTime = currentTime >= DayOpening && currentTime <= DayClosing;
            var darkTheme = new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml")
            };
            var lightTheme = new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml")
            };
            if (isDayTime)
            {
                Application.Current.Resources.MergedDictionaries.Add(lightTheme);
                Application.Current.Resources.MergedDictionaries.Remove(darkTheme);
                Application.Current.Properties.Add("DarkTheme", false);
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(darkTheme);
                Application.Current.Resources.MergedDictionaries.Remove(lightTheme);
                Application.Current.Properties.Add("DarkTheme", true);
            }

            //Process any command line args
            await Task.Factory.StartNew(() => {
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    if (File.Exists(args[1]))
                    {
                        OpenFile(args[1]);
                    }
                    else
                    {
                        OpenFolder(args[1]);
                    }
                }

            }, TaskCreationOptions.AttachedToParent);
        }

        private void ListViewBoxStackPanel_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

    }
}
