using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using PowerModeChangedEventArgs = Microsoft.Win32.PowerModeChangedEventArgs;

namespace Quotes;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _notifyIcon;
    private AppConfig _config = AppConfig.Load();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        EnsureQuotesFolderHasSamples();
        CreateTrayIcon();

        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        TryShowDailyQuote();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        // if (e.Mode == PowerModes.Resume)
            Dispatcher.Invoke(TryShowDailyQuote);
    }

    private void TryShowDailyQuote()
    {
        _config = AppConfig.Load();
        //if (_config.LastShownDate.Date == DateTime.Today)
        //    return;

        ShowQuoteWindow();
    }

    private void ShowQuoteWindow()
    {
        _config = AppConfig.Load();
        var quote = QuoteService.GetRandomQuote(_config.QuotesFolder);
        if (quote == null)
        {
            OpenSettings();
            return;
        }

        _config.LastShownDate = DateTime.Today;
        _config.Save();

        var window = new MainWindow(quote);
        window.SettingsRequested += OpenSettings;
        window.Show();
        window.Activate();
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_config);
        settingsWindow.ShowDialog();
        _config = AppConfig.Load();
    }

    private void CreateTrayIcon()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show Today's Quote", null, (_, _) => ShowQuoteWindow());
        contextMenu.Items.Add("Settings", null, (_, _) => OpenSettings());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApp());

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Text = "Quotes – Quote of the Day",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ShowQuoteWindow();
    }

    private void EnsureQuotesFolderHasSamples()
    {
        _config = AppConfig.Load();
        if (Directory.Exists(_config.QuotesFolder) && Directory.GetFiles(_config.QuotesFolder, "*.txt").Length > 0)
            return;

        Directory.CreateDirectory(_config.QuotesFolder);

        File.WriteAllText(Path.Combine(_config.QuotesFolder, "quote1.txt"),
            "The only way to do great work is to love what you do.\n\n— Steve Jobs");
        File.WriteAllText(Path.Combine(_config.QuotesFolder, "quote2.txt"),
            "In the middle of every difficulty lies opportunity.\n\n— Albert Einstein");
        File.WriteAllText(Path.Combine(_config.QuotesFolder, "quote3.txt"),
            "It does not matter how slowly you go as long as you do not stop.\n\n— Confucius");
        File.WriteAllText(Path.Combine(_config.QuotesFolder, "quote4.txt"),
            "Life is what happens when you're busy making other plans.\n\n— John Lennon");
        File.WriteAllText(Path.Combine(_config.QuotesFolder, "quote5.txt"),
            "The future belongs to those who believe in the beauty of their dreams.\n\n— Eleanor Roosevelt");
    }

    private void ExitApp()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _notifyIcon?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

