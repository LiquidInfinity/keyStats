using System;
using System.Windows;
using KeyStats.ViewModels;

namespace KeyStats.Views;

public partial class AppStatsWindow : Window
{
    private readonly AppStatsViewModel _viewModel;

    public AppStatsWindow()
    {
        InitializeComponent();
        _viewModel = (AppStatsViewModel)DataContext;
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.Cleanup();
    }
}
