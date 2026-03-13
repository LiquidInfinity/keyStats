using System.Diagnostics;
using System.Windows;

namespace KeyStats.Views;

public partial class SettingsWindow : Window
{
    private const string GitHubUrl = "https://github.com/debugtheworldbot/keyStats";

    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OpenStats_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.ShowStatsPanel();
    }

    private void ImportData_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.ImportData();
    }

    private void ExportData_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.ExportData();
    }

    private void NotificationSettings_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.ShowNotificationSettings();
    }

    private void MouseCalibration_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.ShowMouseCalibration();
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(GitHubUrl)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show(this, "无法打开 GitHub 页面。", "KeyStats", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
