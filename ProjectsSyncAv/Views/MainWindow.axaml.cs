using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ProjectsSyncAv.Views;

namespace PRSyncAv.Views;

public partial class MainWindow : Window
{
    public static MainWindow? Current;

    public MainWindow()
    {
        Current = this;

        InitializeComponent();
        Console.WriteLine("MainWindow initialized");
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        Hide();
        e.Cancel = true;
    }
}
