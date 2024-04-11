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
    public SyncDirectory SyncDirectory
    {
        get
        {
            if (_syncDirectory == null)
                throw new Exception("Sync directory not set.");

            if (_username == null)
                throw new Exception("Username not set.");

            if (_password == null)
                throw new Exception("Password not set.");

            return _syncDirectory;
        }
        set => _syncDirectory = value;
    }

    private string? _username = null;
    private string? _password = null;

    private UsernamePasswordCredentials GetCredentials(string url)
    {
        if (_username == null || _password == null)
        {
            throw new Exception("Credentials not set.");
        }

        return new UsernamePasswordCredentials() { Username = _username, Password = _password };
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
            SyncDirectory.Pull(Console.WriteLine);
        });
        IsBusy = false;
    }

    public async void Refresh()
    {
        IsBusy = true;
        await System.Threading.Tasks.Task.Delay(1000);
        IsBusy = false;
    }

    public void Set(string directory, string email)
    {
        var identity = new Identity("PcSync2Av", email);
        var signature = new Signature(identity, DateTime.Now);
        _syncDirectory = SyncDirectory.Open(directory, signature, GetCredentials, true);
    }

    public MainViewModel() { }

    public MainViewModel(string directory, string email)
    {
        this.Set(directory, email);
    }
}
