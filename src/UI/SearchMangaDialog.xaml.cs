using System.Windows;
using System.Windows.Input;

namespace MDXAMLUI;

/// <summary>
/// Interaction logic for SearchMangaDialog.xaml
/// </summary>
public partial class SearchMangaDialog : Window
{
    public SearchMangaDialog()
    {
        InitializeComponent();
        DataContext = this;
        searchQueryTxb.Focus();
    }

    public string URL { get; set; }
    public string[] RetrieverNames { get; set; }
    public int SelectedRetrieverIndex { get; set; }

    public void Ok_Click(object o, RoutedEventArgs args)
    {
        DialogResult = true;
        Close();
    }

    private void SearchQueryTxb_MouseDown(object sender, MouseButtonEventArgs e)
    {
        searchQueryTxb.SelectAll();
    }

    private void SearchQueryTxb_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DialogResult = true;
            Close();
        }
    }
}
