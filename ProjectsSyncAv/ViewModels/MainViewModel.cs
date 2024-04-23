using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;
using LibGit2Sharp;
using PRSyncAv.Views;
using ProjectsSyncLib;
using ReactiveUI;
using ProjectsSyncAv.Views;
using Avalonia.Threading;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;

namespace PRSyncAv.ViewModels;

public class MainViewModel : ViewModelBase
{

    private bool? _pullAvailable = null;
    public bool PullAvailableNotSet => _pullAvailable == null;
    public bool PullNotAvailable => _pullAvailable != null && _pullAvailable.Value == false;
    public bool? PullAvailableRaw => _pullAvailable;
    public bool? PullAvailable
    {
        get => _pullAvailable != null && _pullAvailable.Value;
        set
        {
            if (value == _pullAvailable)
                return;

            _pullAvailable = value;
            this.RaisePropertyChanged(nameof(PullAvailable));
            this.RaisePropertyChanged(nameof(PullNotAvailable));
            this.RaisePropertyChanged(nameof(PullAvailableNotSet));
            this.RaisePropertyChanged(nameof(PullAvailableRaw));
        }
    }

    private bool? _isModified = null;
    public bool IsModifiedNotSet => _isModified == null;
    public bool IsNotModified => _isModified != null && _isModified.Value == false;
    public bool? IsModifiedRaw => _isModified;
    public bool? IsModified
    {
        get => _isModified != null && _isModified.Value;
        set
        {
            if (value == _isModified)
                return;

            _isModified = value;
            this.RaisePropertyChanged(nameof(IsModified));
            this.RaisePropertyChanged(nameof(IsNotModified));
            this.RaisePropertyChanged(nameof(IsModifiedNotSet));
            this.RaisePropertyChanged(nameof(IsModifiedRaw));
        }
    }

    private bool _IsSetUp = false;
    public bool IsSetUp
    {
        get => _IsSetUp;
        set => this.RaiseAndSetIfChanged(ref _IsSetUp, value);
    }

    private bool _IsExpanded = true;
    public bool IsExpanded
    {
        get => _IsExpanded;
        set => this.RaiseAndSetIfChanged(ref _IsExpanded, value);
    }


    private string _LogsText = "";
    public string LogsText
    {
        get => _LogsText;
        set => this.RaiseAndSetIfChanged(ref _LogsText, value);
    }

    private string _CurrentLogText = "";
    public string CurrentLogText
    {
        get => _CurrentLogText;
        set => this.RaiseAndSetIfChanged(ref _CurrentLogText, value);
    }


    private SyncDirectory? _syncDirectory;
    public SyncDirectory SyncDirectory
    {
        get
        {
            if (_syncDirectory == null)
                throw new Exception("Sync directory not set.");

            return _syncDirectory;
        }
        set => _syncDirectory = value;
    }

    private UsernamePasswordCredentials GetCredentials(string url)
    {
        if (MainWindow.Current == null)
            throw new NullReferenceException("MainWindow.Current shouldn't be null!");

        var creds = Dispatcher.UIThread.Invoke(async () =>
        {
            var newWindow = new CredentialsWindow(url);
            await newWindow.ShowDialog(MainWindow.Current);
            return newWindow.Result;
        }).Result;

        if (creds == null || !creds.HasValue)
            return new UsernamePasswordCredentials();

        return new UsernamePasswordCredentials() { Username = creds.Value.Username, Password = creds.Value.Password };
    }

    private bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    private void LogAction(SyncLogEntry log)
    {
        // TODO: Add logging
        Debug.WriteLine(log);

        // Logs.Insert(0, log);
        var logText = log.ToString();
        LogsText = logText + "\n" + LogsText;
        CurrentLogText = logText;
    }

    public async Task Pull()
    {
        Debug.WriteLine("[MOCK] Pull");
        IsBusy = true;
        await Task.Run(() =>
        {
            LogAction(SyncLogEntry.Trace("Pulling changes..."));
            SyncDirectory.Pull(LogAction);
            LogAction(SyncLogEntry.Trace("Pulling complete!"));
        });
        IsBusy = false;
    }

    public async Task Refresh()
    {
        IsBusy = true;

        IsModified = null;
        PullAvailable = null;
        await Task.Run(() =>
        {
            LogAction(SyncLogEntry.Trace("Checking if up to date..."));
            PullAvailable = !SyncDirectory.IsUpToDate(LogAction);
            LogAction(SyncLogEntry.Trace("Check if is modified..."));
            IsModified = SyncDirectory.IsModified();
            LogAction(SyncLogEntry.Trace("Refresh complete!"));
        });
        IsBusy = false;
    }

    public async Task Push(bool force = false)
    {
        IsBusy = true;

        await Task.Run(() =>
        {
            LogAction(SyncLogEntry.Trace("Pushing changes..."));
            SyncDirectory.CommitAndPush(force, LogAction);
            LogAction(SyncLogEntry.Trace("Push complete!"));
        });
        await Refresh();
    }

    public void Set(string directory, string email)
    {
        var identity = new Identity("ProjectsSync2Av", email);
        var signature = new Signature(identity, DateTime.Now);
        _syncDirectory = SyncDirectory.Open(directory, signature, GetCredentials, true);
        IsSetUp = true;
        Refresh();
    }

    public MainViewModel() { }

    public MainViewModel(string directory, string email)
    {
        this.Set(directory, email);
    }
}
