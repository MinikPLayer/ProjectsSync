using System;
using Avalonia.Controls;

namespace PRSyncAv.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Console.WriteLine("MainWindow initialized");
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        Hide();
        e.Cancel = true;
    }
}
