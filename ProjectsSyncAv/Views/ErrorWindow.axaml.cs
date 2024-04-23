using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ProjectsSyncAv.ViewModels;
using System.IO;

namespace ProjectsSyncAv.Views
{
    public partial class ErrorWindow : Window
    {

        public ErrorWindow(string title, string message)
        {
            this.DataContext = new ErrorWindowViewModel(title, message);
            InitializeComponent();
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
