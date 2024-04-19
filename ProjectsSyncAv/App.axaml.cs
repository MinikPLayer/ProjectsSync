using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using PRSyncAv.ViewModels;
using PRSyncAv.Views;

using HotAvalonia;
using System;
using System.Diagnostics;
using Avalonia.Controls;
using System.Runtime.Versioning;

namespace PRSyncAv;

[SupportedOSPlatform("Linux")]
[SupportedOSPlatform("Windows")]
public partial class App : Application
{
    public AppConfig CurrentConfig;

    public override void Initialize()
    {
        this.EnableHotReload();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        CurrentConfig = AppConfig.Load();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Console.WriteLine("Launching in desktop mode");
#if DEBUG
            RestoreWindow();
#endif
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            throw new NotSupportedException("Single View Platforms are not supported right now");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RestoreWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        desktop.MainWindow ??= new MainWindow()
        {
            DataContext = new MainViewModel(),
        };

        desktop.MainWindow.Show();
    }

    private void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
        else
        {
            Debug.WriteLine("ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime, so Exit is not supported.");
        }
    }

    private void RestoreWindowMenuItemOnClick(object? sender, EventArgs e) => RestoreWindow();

    private void ExitMenuItemOnClick(object? sender, EventArgs e) => ExitApplication();
}
