using System.Collections.Generic;
using System.ComponentModel;

namespace MangaDownloader;

public class Chapter : INotifyPropertyChanged
{
    public Chapter(Manga parent)
    {
        Parent = parent;
        isComplete = false;
        progress = 0;
    }

    public float Id { get; set; }
    public string Title { get; set; }
    public List<Page> Pages { get; set; }
    public string Address { get; set; }
    public Manga Parent { get; }
    public bool IsArchiving { get; set; }
    public bool IsUnarchiving { get; set; }
    public bool IsComplete
    {
        get
        {
            return isComplete;
        }
        set
        {
            isComplete = value;
            OnPropertyChanged("IsComplete");
        }
    }
    public byte Progress
    {
        get { return progress; }
        set
        {
            progress = value;
            OnPropertyChanged("Progress");
        }
    }


    private bool isComplete;
    private byte progress;
    public event PropertyChangedEventHandler PropertyChanged;
    // Create the OnPropertyChanged method to raise the event
    // The calling member's name will be used as the parameter.
    protected void OnPropertyChanged(string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public static void SortChapters(List<Chapter> unsortedChapters)
    {
        InsertionSortChapters(unsortedChapters);
    }

    private static void InsertionSortChapters(List<Chapter> unsortedChapters)
    {
        Chapter chapterKey;
        for (int i = 0; i < unsortedChapters.Count; i++)
        {
            chapterKey = unsortedChapters[i];
            int j = i - 1;
            while (j >= 0 && unsortedChapters[j].Id > chapterKey.Id)
            {
                unsortedChapters[j + 1] = unsortedChapters[j];
                j--;
            }
            unsortedChapters[j + 1] = chapterKey;
        }
    }
}
