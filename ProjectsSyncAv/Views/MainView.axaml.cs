using System;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PRSyncAv.ViewModels;

namespace PRSyncAv.Views;

[SupportedOSPlatform("Linux")]
public partial class MainView : UserControl
{
    private MainViewModel ViewModel => (MainViewModel?)DataContext ?? throw new InvalidOperationException();

    public MainView()
    {
        InitializeComponent();
    }

    private void PullButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.Refresh();
    }

    private void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.Refresh();
    }
}
