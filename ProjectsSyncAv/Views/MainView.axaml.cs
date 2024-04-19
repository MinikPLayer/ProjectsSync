using System;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PRSyncAv.ViewModels;

namespace PRSyncAv.Views;

[SupportedOSPlatform("Linux")]
[SupportedOSPlatform("Windows")]
public partial class MainView : UserControl
{
    private MainViewModel ViewModel => (MainViewModel?)DataContext ?? throw new InvalidOperationException();

    public MainView()
    {
        InitializeComponent();
    }

    public void PullButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.Refresh();
    }

    public void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.Refresh();
    }
}
