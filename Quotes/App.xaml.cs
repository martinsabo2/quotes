using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using PowerModeChangedEventArgs = Microsoft.Win32.PowerModeChangedEventArgs;

namespace Quotes;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _notifyIcon;
    private Icon? _trayAppIcon;
    private AppConfig _config = AppConfig.Load();
    private MainWindow? _mainWindow;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint hIcon);

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
        //_config = AppConfig.Load();
        //if (_config.LastShownDate.Date == DateTime.Today)
        //    return;

        ShowQuoteWindow();
    }

    private void ShowQuoteWindow()
    {
        // a guard to allow only one open window at a time
        if (_mainWindow is { IsLoaded: true })
        {
            _mainWindow.Activate();
            return;
        }

        _config = AppConfig.Load();
        var quote = QuoteService.GetRandomQuote(_config.QuotesFolder, _config.DefaultFontFamily, _config.DefaultFontSize);
        if (quote == null)
        {
            OpenSettings();
            return;
        }

        _config.LastShownDate = DateTime.Today;
        _config.Save();

        _mainWindow = new MainWindow(quote, () =>
        {
            _config = AppConfig.Load();
            return QuoteService.GetRandomQuote(_config.QuotesFolder, _config.DefaultFontFamily, _config.DefaultFontSize);
        });
        _mainWindow.SettingsRequested += OpenSettings;
        _mainWindow.Show();
        _mainWindow.Activate();
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

        _trayAppIcon = CreateModernQTrayIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayAppIcon,
            Text = "Quote of the Day",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ShowQuoteWindow();
    }

    private static Icon CreateModernQTrayIcon()
    {
        const int iconSize = 64;
        var beige = Color.FromArgb(245, 231, 214);
        var textColor = Color.FromArgb(46, 37, 30);

        using var bitmap = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(Color.Transparent);

        var tileSize = iconSize - 6;
        var tileRect = new Rectangle(3, 3, tileSize, tileSize);

        using (var shadowBrush = new SolidBrush(Color.FromArgb(38, 0, 0, 0)))
        {
            graphics.FillEllipse(shadowBrush, tileRect.X + 1, tileRect.Y + 2, tileRect.Width - 2, tileRect.Height - 2);
        }

        using (var backBrush = new SolidBrush(beige))
        {
            graphics.FillEllipse(backBrush, tileRect);
        }

        using (var borderPen = new Pen(Color.FromArgb(210, 195, 176), 2f))
        {
            graphics.DrawEllipse(borderPen, tileRect);
        }

        using var font = new System.Drawing.Font("Aharoni", 50, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        var textRect = new RectangleF(0, 5f, iconSize, iconSize);
        using var textBrush = new SolidBrush(textColor);
        using var textFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString("Q", font, textBrush, textRect, textFormat);

        var hIcon = bitmap.GetHicon();
        try
        {
            using var generatedIcon = Icon.FromHandle(hIcon);
            return (Icon)generatedIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
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
        _trayAppIcon?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _notifyIcon?.Dispose();
        _trayAppIcon?.Dispose();
        base.OnExit(e);
    }
}

