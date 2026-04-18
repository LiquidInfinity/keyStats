using System.Diagnostics;
using System.Reflection;
using System.Windows;
using KeyStats.Helpers;

namespace KeyStats.Views;

public partial class SettingsWindow : Window
{
    private const string GitHubUrl = "https://github.com/debugtheworldbot/keyStats";

    public SettingsWindow()
    {
        InitializeComponent();
        VersionTextBlock.Text = $"\u5f53\u524d\u7248\u672c {GetDisplayVersion()}";
        Loaded += OnLoaded;
        Closed += OnClosed;
        ThemeManager.Instance.ThemeChanged += OnThemeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowBackdrop();
        App.CurrentApp?.TrackPageView("settings");
    }

    private void OnClosed(object? sender, System.EventArgs e)
    {
        ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        Dispatcher.BeginInvoke(new System.Action(ApplyWindowBackdrop));
    }

    private void ApplyWindowBackdrop()
    {
        WindowBackdropHelper.Apply(this, NativeInterop.DwmSystemBackdropType.TransientWindow);
    }

    private static string GetDisplayVersion()
    {
        var assembly = typeof(App).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var safeVersion = informationalVersion ?? string.Empty;
            var separatorIndex = safeVersion.IndexOf('+');
            return separatorIndex >= 0
                ? safeVersion.Substring(0, separatorIndex)
                : safeVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private void OpenStats_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.TrackClick("open_stats_popup");
        App.CurrentApp?.ShowStatsPanel();
    }

    private void ImportData_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.TrackClick("import_data");
        App.CurrentApp?.ImportData();
    }

    private void ExportData_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.TrackClick("export_data");
        App.CurrentApp?.ExportData();
    }

    private void NotificationSettings_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.TrackClick("open_notification_settings");
        App.CurrentApp?.ShowNotificationSettings();
    }

    private void MouseCalibration_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentApp?.TrackClick("open_mouse_calibration");
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
