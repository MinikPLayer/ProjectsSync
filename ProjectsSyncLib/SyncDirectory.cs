using System.Reflection;
using System.Runtime.Versioning;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace ProjectsSyncLib;

public struct SyncLogEntry
{
    public enum LogSeverity
    {
        TRACE = 0,
        DEBUG = 1,
        INFO = 2,
        WARNING = 3,
        ERROR = 4,
        FATAL = 5,
        EXCEPTION = 6,
    }

    public LogSeverity Severity;
    public DateTime Time;
    public string Title = "";
    public string Value;

    public override string ToString()
    {
        var ret = Value;
        if (!string.IsNullOrEmpty(Title))
            ret = $"[{Title}] {ret}";

        return $"[{Severity}] {ret}";
    }

    public SyncLogEntry()
    {
        Severity = LogSeverity.TRACE;
        Time = DateTime.Now;
        Value = "";
    }

    private SyncLogEntry(LogSeverity severity, string title, string value)
    {
        Severity = severity;
        Value = value;
        Time = DateTime.Now;
    }

    public static SyncLogEntry Trace(string title, string value) => new SyncLogEntry(LogSeverity.TRACE, title, value);
    public static SyncLogEntry Trace(string value) => Trace("", value);

    public static SyncLogEntry Debug(string title, string value) => new SyncLogEntry(LogSeverity.DEBUG, title, value);
    public static SyncLogEntry Debug(string value) => Debug("", value);

    public static SyncLogEntry Info(string title, string value) => new SyncLogEntry(LogSeverity.INFO, title, value);
    public static SyncLogEntry Info(string value) => Info("", value);

    public static SyncLogEntry Warning(string title, string value) => new SyncLogEntry(LogSeverity.WARNING, title, value);
    public static SyncLogEntry Warning(string value) => Warning("", value);

    public static SyncLogEntry Error(string title, string value) => new SyncLogEntry(LogSeverity.ERROR, title, value);
    public static SyncLogEntry Error(string value) => Error("", value);

    public static SyncLogEntry Fatal(string title, string value) => new SyncLogEntry(LogSeverity.FATAL, title, value);
    public static SyncLogEntry Fatal(string value) => Fatal("", value);

    public static SyncLogEntry Exception(string title, Exception e) => new SyncLogEntry(LogSeverity.EXCEPTION, title, "An exception occured: " + e.ToString() + "\nStack trace: " + e.StackTrace);
    public static SyncLogEntry Exception(Exception e) => Exception("", e);
}

[SupportedOSPlatform("Linux")]
[SupportedOSPlatform("Windows")]
public class SyncDirectory
{
    public delegate UsernamePasswordCredentials UserProvidedCredentialsHandler(string url);
    public delegate void LogHandler(SyncLogEntry log);

    private Repository _repo;
    private Signature _signature;
    private UserProvidedCredentialsHandler _credentialsHandler;
    public string Path { get; }
    public bool UsePasswordSecureStorage { get; }

    private static CredentialsHandler GetCredentialsProvider(UserProvidedCredentialsHandler handler) => new CredentialsHandler((url, usernameFromUrl, types) => handler.Invoke(url));

    static SyncDirectory()
    {
        var assembly = Assembly.GetAssembly(typeof(SyncDirectory));
        if (assembly == null)
        {
            throw new InvalidOperationException("Could not get assembly.");
        }

        var assemblyLocation = assembly.Location;
        var assemblyDir = System.IO.Path.GetDirectoryName(assemblyLocation);
        if (assemblyDir == null)
        {
            throw new InvalidOperationException("Could not get assembly directory.");
        }
    }

    public static SyncDirectory Create(string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider, bool usePasswordSecureStorage)
    {
        if (Repository.IsValid(path))
            throw new ArgumentException("Repository already exists at " + path, nameof(path));

        Repository.Init(path);
        return new SyncDirectory(path, signature, credentialsProvider, usePasswordSecureStorage);
    }

    public static SyncDirectory Open(string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider, bool usePasswordSecureStorage)
    {
        if (!Repository.IsValid(path))
            throw new ArgumentException("Repository not found at " + path, nameof(path));

        return new SyncDirectory(path, signature, credentialsProvider, usePasswordSecureStorage);
    }

    public static SyncDirectory Clone(string url, string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider, bool usePasswordSecureStorage, LogHandler logHandler)
    {
        var cloneOptions = new CloneOptions();
        cloneOptions.FetchOptions.CredentialsProvider = GetCredentialsProvider(credentialsProvider);
        cloneOptions.OnCheckoutProgress = (path, completedSteps, totalSteps) =>
        {
            var logEntry = SyncLogEntry.Info("Clone", $"Checkout: {path} {completedSteps}/{totalSteps}");
            logHandler(logEntry);
        };

        var repo = Repository.Clone(url, path, cloneOptions);
        if (repo == null)
            throw new InvalidOperationException("Failed to clone repository.");

        return new SyncDirectory(path, signature, credentialsProvider, usePasswordSecureStorage);
    }

    public static bool VerifyDirectory(string directory)
    {
        try
        {
            var dummySignature = new Signature("dummy", "dummy", DateTime.Now);

            var sd = Open(directory, dummySignature, (p) => { return null!; }, false);
            return true;
        }
        catch (ArgumentException e)
        {
            return false;
        }
    }

    private SyncDirectory(string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider, bool usePasswordSecureStorage)
    {
        Path = path;
        _repo = new Repository(path);
        _signature = signature;
        _credentialsHandler = credentialsProvider;
        UsePasswordSecureStorage = usePasswordSecureStorage;
    }

    private void SaveCredentials(string url, UsernamePasswordCredentials credentials, LogHandler logHandler)
    {
        if (SecureStorage.IsSupportedOS())
        {
            logHandler(SyncLogEntry.Info($"Saving password for {url} in secure storage."));
            SecureStorage.SavePassword(credentials, url);
        }
        else
        {
            logHandler(SyncLogEntry.Warning($"Secure storage not supported on this platform. Password will not be saved."));
        }
    }

    private void ClearCredentials(string url)
    {
        if (!SecureStorage.PasswordExists(url))
            return;

        var ret = SecureStorage.ClearPassword(url);
        if (!ret)
            throw new InvalidOperationException($"Failed to clear password for {url}.");
    }

    private T TryWithAuth<T>(Func<T> tryCommand, LogHandler logHandler)
    {
        T ret;
        try
        {
            ret = tryCommand();
        }
        catch (LibGit2SharpException e)
        {
            if (!UsePasswordSecureStorage || !e.Message.Contains("authentication"))
            {
                logHandler(SyncLogEntry.Exception(e));
                throw;
            }

            if (string.IsNullOrEmpty(_lastUrl))
            {
                logHandler(SyncLogEntry.Error("Unknown authentication error"));
                throw;
            }

            logHandler(SyncLogEntry.Error("Authentication error. Clearing credentials and trying again."));
            ClearCredentials(_lastUrl);
            return TryWithAuth(tryCommand, logHandler);
        }

        if (UsePasswordSecureStorage && _lastCredentials != null)
            SaveCredentials(_lastUrl, _lastCredentials, logHandler);

        return ret;
    }

    private void TryWithAuth(Action tryCommand, LogHandler logHandler)
    {
        var actionProxy = () =>
        {
            tryCommand();
            return true;
        };

        TryWithAuth<bool>(actionProxy, logHandler);
    }

    private string _lastUrl = "";
    private UsernamePasswordCredentials? _lastCredentials;
    private CredentialsHandler GetCredentialsInternal()
    {
        var creds = GetCredentialsProvider((url) =>
        {
            _lastUrl = url;
            if (UsePasswordSecureStorage && SecureStorage.TryGetPassword(url, out var c))
            {
                _lastCredentials = c!;
            }
            else
            {
                _lastCredentials = _credentialsHandler.Invoke(url);
            }

            return _lastCredentials;
        });
        return creds;
    }

    public void AddRemote(string url)
    {
        _repo.Network.Remotes.Add("origin", url);
    }

    public void CommitAndPush(bool force, PushTransferProgressHandler pushHandler, PackBuilderProgressHandler packHandler, LogHandler logHandler)
    {
        var remote = _repo.Network.Remotes["origin"];
        if (remote == null)
            throw new InvalidOperationException("No remote found.");

        var status = _repo.RetrieveStatus();
        if (status.IsDirty)
        {
            var now = DateTime.Now;
            var message = $"Auto commit at {now}";
            try
            {
                _repo.Commit(message, _signature, _signature);
            }
            catch (Exception e)
            {
                logHandler.Invoke(SyncLogEntry.Exception("Commit", e));
                if (!force)
                    return;
            }
        }
        else
        {
            logHandler.Invoke(SyncLogEntry.Info("Commit", "Nothing to commit, working tree clean"));
            if (!force)
                return;
        }

        TryWithAuth(() => _repo.Network.Push(
            remote,
            _repo.Head.CanonicalName,
            new PushOptions()
            {
                CredentialsProvider = GetCredentialsInternal(),
                OnPushStatusError = (e) => logHandler.Invoke(SyncLogEntry.Error("Push status", e.Message)),
                OnPushTransferProgress = pushHandler,
                OnPackBuilderProgress = packHandler,
            }
        ), logHandler);
    }

    public void CommitAndPush(bool force, LogHandler logHandler, bool addAll = true)
    {
        if (addAll)
            AddAll(logHandler);

        var pushHandler = new PushTransferProgressHandler((current, total, bytes) =>
        {
            logHandler(SyncLogEntry.Info("Push", $"{current}/{total} ({bytes} bytes)"));
            return true;
        });

        var packHandler = new PackBuilderProgressHandler((stage, current, total) =>
        {
            logHandler(SyncLogEntry.Info("Push/Pack", $"{stage}: {current}/{total}"));
            return true;
        });

        CommitAndPush(force, pushHandler, packHandler, logHandler);
    }

    public bool IsModified()
    {
        var status = _repo.RetrieveStatus();
        return status.IsDirty;
    }

    public bool IsUpToDate(LogHandler logHandler)
    {
        Fetch(logHandler);

        logHandler(SyncLogEntry.Trace("Getting remote origin..."));
        var remote = _repo.Network.Remotes["origin"];
        if (remote == null)
            throw new InvalidOperationException("No remote found.");

        logHandler(SyncLogEntry.Trace("Getting refspec..."));
        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
        var refSpec = refSpecs.FirstOrDefault();
        if (refSpec == null)
            throw new InvalidOperationException("No refspec found.");

        logHandler(SyncLogEntry.Trace("Getting ref name..."));
        var refName = refSpec.Split(':')[1];
        var remoteBranch = _repo.Branches[refName.Replace("*", "master")];
        if (remoteBranch == null)
            throw new InvalidOperationException("Remote branch not found.");

        logHandler(SyncLogEntry.Trace("Getting local branch..."));
        var localBranch = _repo.Branches[_repo.Head.FriendlyName];
        if (localBranch == null)
            throw new InvalidOperationException("Local branch not found.");

        return localBranch.Tip.Sha == remoteBranch.Tip.Sha;
    }

    public string StatusString(LogHandler logHandler, bool addAll = true)
    {
        if (addAll)
            AddAll(logHandler);

        var status = _repo.RetrieveStatus();
        var ret = "On branch " + _repo.Head.FriendlyName + "\n\n";

        var hasChanges = false;
        foreach (var entry in status)
        {
            ret += $"{entry.State}: {entry.FilePath}\n";
            if (entry.State != FileStatus.Ignored)
                hasChanges = true;
        }

        if (!hasChanges)
        {
            ret += "Nothing to commit, working tree clean\n";
        }

        return ret;

    }

    public void AddAll(LogHandler? logAction)
    {
        logAction?.Invoke(SyncLogEntry.Info("AddAll", "Adding files. (This could take a long time!)"));
        Commands.Unstage(_repo, "*");
        Commands.Stage(_repo, "*");
    }

    public void Stage(string path)
    {
        Commands.Stage(_repo, path);
    }
    private FetchOptions GetFetchOptions(LogHandler logHandler)
    {
        return new FetchOptions
        {
            CredentialsProvider = GetCredentialsInternal(),
            OnProgress = (s) =>
            {
                logHandler(SyncLogEntry.Info("Fetch", s));
                return true;
            },
            OnTransferProgress = (progress) =>
            {
                logHandler(SyncLogEntry.Info("Fetch", $"{progress.ReceivedObjects}/{progress.TotalObjects} ({progress.ReceivedBytes} bytes)"));
                return true;
            },
            OnUpdateTips = (refName, oldId, newId) =>
            {
                logHandler(SyncLogEntry.Info("Fetch", $"{refName} {oldId} -> {newId}"));
                return true;
            }
        };
    }

    public void Fetch(LogHandler logHandler)
    {
        var options = GetFetchOptions(logHandler);

        logHandler(SyncLogEntry.Trace("Fetching changes..."));
        TryWithAuth(() => Commands.Fetch(_repo, "origin", new List<string>(), options, ""), logHandler);
    }

    public void Pull(LogHandler logHandler, bool addAll = true)
    {
        if (addAll)
            AddAll(logHandler);

        var options = new PullOptions
        {
            FetchOptions = GetFetchOptions(logHandler),
        };

        MergeResult result = TryWithAuth(() => Commands.Pull(_repo, _signature, options), logHandler);

        if (result.Status == MergeStatus.Conflicts)
        {
            logHandler(SyncLogEntry.Error("Pull", "Conflicts detected, please resolve them."));
        }
        else if (result.Status == MergeStatus.UpToDate)
        {
            logHandler(SyncLogEntry.Info("Pull", "Already up to date."));
        }
        else if (result.Status == MergeStatus.FastForward)
        {
            logHandler(SyncLogEntry.Info("Pull", "Fast forward merge."));
        }
        else
        {
            logHandler(SyncLogEntry.Info("Pull", "Merge completed."));
        }
    }

    ~SyncDirectory()
    {
        _repo.Dispose();
    }
}