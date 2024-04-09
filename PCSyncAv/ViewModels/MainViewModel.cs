using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;
using LibGit2Sharp;
using PCSyncAv.Views;
using PcSyncLib;
using ReactiveUI;

namespace PCSyncAv.ViewModels;

[SupportedOSPlatform("Linux")]
public class MainViewModel : ViewModelBase
{
    private SyncDirectory? _syncDirectory;
    private string? _username = null;
    private SecureString? _password = null;

    private UsernamePasswordCredentials GetCredentials(string url)
    {
        if (_username == null || _password == null)
        {
            throw new Exception("Credentials not set.");
        }

        return new UsernamePasswordCredentials() { Username = _username, Password = _password.ToString() };
    }

    private bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public async void Pull()
    {
        Debug.WriteLine("[MOCK] Pull");
        IsBusy = true;
        await Task.Run(() =>
        {

        });
        IsBusy = false;
    }

    public async void Refresh()
    {
        IsBusy = true;
        await System.Threading.Tasks.Task.Delay(1000);
        IsBusy = false;
    }

    public MainViewModel() { }

    public MainViewModel(string directory, string username, string email, SecureString password)
    {
        this._username = username;
        this._password = password;

        var identity = new Identity(username, email);
        var signature = new Signature(identity, DateTime.Now);
        _syncDirectory = SyncDirectory.Open(directory, signature, GetCredentials, true);
    }
}
