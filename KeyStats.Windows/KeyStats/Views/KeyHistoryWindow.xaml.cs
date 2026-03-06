using System;
using System.Windows;
using System.Windows.Interop;
using KeyStats.Helpers;
using KeyStats.ViewModels;

namespace KeyStats.Views;

public partial class KeyHistoryWindow : Window
{
    private readonly KeyHistoryViewModel _viewModel;

    public KeyHistoryWindow()
    {
        InitializeComponent();
        _viewModel = (KeyHistoryViewModel)DataContext;
        Loaded += OnLoaded;
        Closed += OnClosed;
        ThemeManager.Instance.ThemeChanged += OnThemeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowTitleBarTheme();
        App.CurrentApp?.TrackPageView("key_history");
    }

    private void OnThemeChanged()
    {
        Dispatcher.BeginInvoke(new Action(ApplyWindowTitleBarTheme));
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
        _viewModel.Cleanup();
    }

    private void ApplyWindowTitleBarTheme()
    {
        var handle = new WindowInteropHelper(this).Handle;
        NativeInterop.TrySetImmersiveDarkMode(handle, ThemeManager.Instance.IsDarkTheme);
    }
}
