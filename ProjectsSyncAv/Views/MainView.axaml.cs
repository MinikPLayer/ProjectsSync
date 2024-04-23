using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ProjectsSyncAv.Views;
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

        var configVerify = App.CurrentConfig.VerifyConfig();
        if (!configVerify.isGood)
        {
            if (MainWindow.Current == null)
                throw new Exception("Window shouldn't be null");

            this.DataContext = new MainViewModel();
            MainWindow.Current.Show();
            var mbox = new ErrorWindow(
                "Config verification failed",
                $"Config error: {configVerify.errorString}\nClick OK, then fix errors on the setup screen."
            )
            .ShowDialog(MainWindow.Current).ContinueWith(async (r) =>
            {
                await OpenSetupWindow(false);
            });
        }
        else
        {
            this.DataContext = new MainViewModel(App.CurrentConfig.RepoPath, App.CurrentConfig.EmailAddress);
        }
    }

    async Task OpenSetupWindow(bool allowClose)
    {
        if (MainWindow.Current == null)
            throw new Exception("Window shouldn't be null");

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var setupWindow = new SetupWindow(allowClose);
            await setupWindow.ShowDialog(MainWindow.Current);
            if(setupWindow.Result)
                this.DataContext = new MainViewModel(App.CurrentConfig.RepoPath, App.CurrentConfig.EmailAddress);
        });
    }

    public async void PullButton_OnClick(object? sender, RoutedEventArgs e) => await ViewModel.Pull();
    public async void RefreshButton_OnClick(object? sender, RoutedEventArgs e) => await ViewModel.Refresh();
    public async void PushButton_OnClick(object? sender, RoutedEventArgs e) => await ViewModel.Push();
    public async void ForcePushButton_OnClick(object? sender, RoutedEventArgs e) => await ViewModel.Push(true);
    public async void SettingsButton_Click(object? sender, RoutedEventArgs e) => await OpenSetupWindow(true);
    private void ExpandButton_Click(object? sender, RoutedEventArgs e) => ViewModel.IsExpanded = !ViewModel.IsExpanded;
}
