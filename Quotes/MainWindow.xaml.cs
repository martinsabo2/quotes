using System.Windows;
using System.Windows.Threading;

namespace Quotes;

public partial class MainWindow : Window
{
    public event Action? SettingsRequested;

    private readonly DispatcherTimer _copiedTimer;
    private readonly Func<string?> _nextQuoteProvider;

    public MainWindow(string quote, Func<string?> nextQuoteProvider)
    {
        InitializeComponent();
        QuoteTextBox.Text = quote;
        _nextQuoteProvider = nextQuoteProvider;

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
        if (!string.IsNullOrWhiteSpace(nextQuote))
        {
            QuoteTextBox.Text = nextQuote;
            CopiedLabel.Visibility = Visibility.Collapsed;
            _copiedTimer.Stop();
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText(QuoteTextBox.Text);
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
