using Microsoft.Win32;
using System.Windows;

namespace Quotes;

public partial class SettingsWindow : Window
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Quotes";

    private readonly AppConfig _config;

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;

        FolderTextBox.Text = config.QuotesFolder;

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
        _config.QuotesFolder = FolderTextBox.Text.Trim();
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
