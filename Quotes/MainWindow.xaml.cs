using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace Quotes;

public partial class MainWindow : Window
{
    public event Action? SettingsRequested;

    private readonly DispatcherTimer _copiedTimer;
    private readonly Func<QuoteContent?> _nextQuoteProvider;
    private string _currentPlainText;
    private string _currentHtml;
    private bool _webViewReady;

    public MainWindow(QuoteContent quote, Func<QuoteContent?> nextQuoteProvider)
    {
        InitializeComponent();
        _nextQuoteProvider = nextQuoteProvider;

        _currentPlainText = quote.PlainText;
        _currentHtml = quote.Html;

        Loaded += MainWindow_Loaded;

        _copiedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _copiedTimer.Tick += (_, _) =>
        {
            CopiedLabel.Visibility = Visibility.Collapsed;
            _copiedTimer.Stop();
        };
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_webViewReady)
            return;

        await QuoteBrowser.EnsureCoreWebView2Async();
        QuoteBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        QuoteBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;

        _webViewReady = true;
        QuoteBrowser.NavigateToString(_currentHtml);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        var nextQuote = _nextQuoteProvider();
        if (nextQuote is not null)
        {
            _currentPlainText = nextQuote.PlainText;
            _currentHtml = nextQuote.Html;

            if (_webViewReady)
                QuoteBrowser.NavigateToString(_currentHtml);

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
