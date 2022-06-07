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


    }
}
