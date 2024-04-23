using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ProjectsSyncAv.ViewModels;
using ProjectsSyncLib;
using PRSyncAv;
using System.IO;

namespace ProjectsSyncAv.Views
{
    public partial class SetupWindow : Window
    {
        public bool Result { get; set; } = false;

        private bool allowClosing = false;

        private SetupWindowViewModel ViewModel => (SetupWindowViewModel)this.DataContext!;

        public SetupWindow() : this(true) { }

        public SetupWindow(bool allowClosing)
        {
            InitializeComponent();

            this.allowClosing = allowClosing;
            this.DataContext = new SetupWindowViewModel(allowClosing, App.CurrentConfig);
        }

        private AppConfig ToConfig()
        {
            var config = new AppConfig();

            var vm = ViewModel;
            config.RepoPath = vm.RepoPath;
            config.EmailAddress = vm.UserEmail;

            return config;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = !allowClosing;
        }

        public void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            if (allowClosing)
            {
                Result = false;
                this.Close();
            }
        }

        public void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            var newConfig = this.ToConfig();
            var verifyStatus = newConfig.VerifyConfig();
            if(!verifyStatus.isGood)
            {
                new ErrorWindow("Save error", verifyStatus.errorString).ShowDialog(this);
                return;
            }

            App.CurrentConfig = newConfig;
            allowClosing = true;
            Result = true;
            this.Close();
        }

        public async void RepoChooseDirectoryButton_Click(object? sender, RoutedEventArgs e)
        {
            var pickerOptions = new FolderPickerOpenOptions();
            pickerOptions.AllowMultiple = false;
            pickerOptions.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(ViewModel.RepoPath);
            pickerOptions.Title = "Select repository directory";

            var result = await StorageProvider.OpenFolderPickerAsync(pickerOptions);
            if(result.Count > 0)
                ViewModel.RepoPath = result[0].Path.AbsolutePath;
            
        }
    }
}
