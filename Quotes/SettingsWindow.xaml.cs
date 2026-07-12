using Microsoft.Win32;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Quotes;

public partial class SettingsWindow : Window
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Quotes";

    private static readonly double[] CommonFontSizes = [12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 40];

    private readonly AppConfig _config;

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;

        FolderTextBox.Text = config.QuotesFolder;

        foreach (var fontFamily in Fonts.SystemFontFamilies.OrderBy(f => f.Source, StringComparer.CurrentCultureIgnoreCase))
            FontFamilyComboBox.Items.Add(fontFamily.Source);

        foreach (var fontSize in CommonFontSizes)
            FontSizeComboBox.Items.Add(fontSize.ToString("0.##", CultureInfo.InvariantCulture));

        FontFamilyComboBox.SelectedItem = FontFamilyComboBox.Items.Contains(config.DefaultFontFamily)
            ? config.DefaultFontFamily
            : "Georgia";

        var selectedFontSize = config.DefaultFontSize.ToString("0.##", CultureInfo.InvariantCulture);
        FontSizeComboBox.SelectedItem = FontSizeComboBox.Items.Contains(selectedFontSize)
            ? selectedFontSize
            : "24";

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        StartupCheckBox.IsChecked = key?.GetValue(AppName) is not null;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Quotes Folder",
            InitialDirectory = FolderTextBox.Text
        };

        if (dialog.ShowDialog(this) == true)
            FolderTextBox.Text = dialog.FolderName;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var fontFamily = FontFamilyComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            System.Windows.MessageBox.Show(this, "Please select a default font family.", "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fontSizeText = FontSizeComboBox.SelectedItem as string;
        if (!double.TryParse(fontSizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize) || fontSize <= 0)
        {
            System.Windows.MessageBox.Show(this, "Please select a valid default font size.", "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _config.QuotesFolder = FolderTextBox.Text.Trim();
        _config.DefaultFontFamily = fontFamily;
        _config.DefaultFontSize = fontSize;
        _config.Save();

        ApplyStartupSetting(StartupCheckBox.IsChecked == true);

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static void ApplyStartupSetting(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null) return;

        if (enable)
        {
            var exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe is not null)
                key.SetValue(AppName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
