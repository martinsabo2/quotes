using System.Windows;
using System.Windows.Threading;

namespace Quotes;

public partial class MainWindow : Window
{
    public event Action? SettingsRequested;

    private readonly DispatcherTimer _copiedTimer;
    private readonly Func<QuoteContent?> _nextQuoteProvider;
    private readonly System.Windows.Controls.WebBrowser _quoteBrowser;
    private string _currentPlainText;

    public MainWindow(QuoteContent quote, Func<QuoteContent?> nextQuoteProvider)
    {
        InitializeComponent();
        _nextQuoteProvider = nextQuoteProvider;
        _quoteBrowser = (System.Windows.Controls.WebBrowser)FindName("QuoteBrowser")!;
        _currentPlainText = quote.PlainText;
        _quoteBrowser.NavigateToString(quote.Html);

        _copiedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _copiedTimer.Tick += (_, _) =>
        {
            CopiedLabel.Visibility = Visibility.Collapsed;
            _copiedTimer.Stop();
        };
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        var nextQuote = _nextQuoteProvider();
        if (nextQuote is not null)
        {
            _currentPlainText = nextQuote.PlainText;
            _quoteBrowser.NavigateToString(nextQuote.Html);
            CopiedLabel.Visibility = Visibility.Collapsed;
            _copiedTimer.Stop();
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText(_currentPlainText);
        CopiedLabel.Visibility = Visibility.Visible;
        _copiedTimer.Stop();
        _copiedTimer.Start();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
