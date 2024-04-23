using Avalonia.Controls;
using ProjectsSyncAv.ViewModels;

namespace ProjectsSyncAv.Views
{
    public struct Credentials
    {
        public string Username;
        public string Password;
    }

    public partial class CredentialsWindow : Window
    {
        public Credentials? Result = null;

        public CredentialsWindow() : this("Unknown URL") { }

        public CredentialsWindow(string url)
        {
            var vm = new CredentialsWindowViewModel();
            vm.Url = url;
            this.DataContext = vm;
            InitializeComponent();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if(Result == null)
            {
                e.Cancel = true;
                return;
            }    
        }

        public void LoginButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var viewModel = this.DataContext as CredentialsWindowViewModel;
            if (viewModel == null)
                return;

            if(string.IsNullOrEmpty(viewModel.Login))
            {
                new ErrorWindow("Login window", "Login cannot be empty!").Show();
                return;
            }

            if(string.IsNullOrEmpty(viewModel.Password))
            {
                new ErrorWindow("Login window", "Password cannot be empty!").Show();
                return;
            }

            Result = new Credentials() { Username = viewModel.Login, Password = viewModel.Password };
            this.Close();
        }
    }
}
