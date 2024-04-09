using System.Reflection;
using System.Runtime.Versioning;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace PcSyncLib;

[SupportedOSPlatform("Linux")]
public class SyncDirectory
{
    public delegate UsernamePasswordCredentials UserProvidedCredentialsHandler(string url);

    private Repository _repo;
    private Signature _signature;
    private UserProvidedCredentialsHandler _credentialsHandler;
    public string Path { get; }
    public bool UsePasswordSecureStorage { get; }

    private static CredentialsHandler GetCredentialsProvider(UserProvidedCredentialsHandler handler) => new CredentialsHandler((url, usernameFromUrl, types) => handler.Invoke(url));

    static SyncDirectory()
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("Platform not supported.");
        }

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

        var libDir = System.IO.Path.Combine(assemblyDir, "lib");
        LibGit2Sharp.GlobalSettings.NativeLibraryPath = libDir;
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

    public static SyncDirectory Clone(string url, string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider, bool usePasswordSecureStorage, CheckoutProgressHandler? checkoutProgressHandler = null)
    {
        var cloneOptions = new CloneOptions();
        cloneOptions.FetchOptions.CredentialsProvider = GetCredentialsProvider(credentialsProvider);
        cloneOptions.OnCheckoutProgress = checkoutProgressHandler;

        var repo = Repository.Clone(url, path, cloneOptions);
        if (repo == null)
            throw new InvalidOperationException("Failed to clone repository.");

        return new SyncDirectory(path, signature, credentialsProvider, usePasswordSecureStorage);
    }

    private SyncDirectory(string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider, bool usePasswordSecureStorage)
    {
        Path = path;
        _repo = new Repository(path);
        _signature = signature;
        _credentialsHandler = credentialsProvider;
        UsePasswordSecureStorage = usePasswordSecureStorage;
    }

    private void SaveCredentials(string url, UsernamePasswordCredentials credentials, Action<string> logHandler)
    {
        logHandler($"[Info] Saving password for {url} in secure storage.");
        SecureStorage.SavePassword(credentials, url);
    }

    private void ClearCredentials(string url)
    {
        if (!SecureStorage.PasswordExists(url))
            return;

        var ret = SecureStorage.ClearPassword(url);
        if (!ret)
            throw new InvalidOperationException($"Failed to clear password for {url}.");
    }

    private T TryWithAuth<T>(Func<T> tryCommand, Action<string> errorHandler)
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
                errorHandler("[ERROR] Authentication error.");
                throw;
            }

            if (string.IsNullOrEmpty(_lastUrl))
            {
                errorHandler("[ERORR] Unknown authentication error");
                throw;
            }

            errorHandler("[ERROR] Authentication error. Clearing credentials and trying again.");
            ClearCredentials(_lastUrl);
            return TryWithAuth(tryCommand, errorHandler);
        }

        if (UsePasswordSecureStorage && _lastCredentials != null)
            SaveCredentials(_lastUrl, _lastCredentials, errorHandler);

        return ret;
    }

    private void TryWithAuth(Action tryCommand, Action<string> logHandler)
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

    public void CommitAndPush(bool force, PushTransferProgressHandler pushHandler, PackBuilderProgressHandler packHandler, Action<string> errorHandler, Action<string> logHandler)
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
                errorHandler.Invoke("[Commit] Failed to commit - " + e.Message);
                if (!force)
                    return;
            }
        }
        else
        {
            logHandler.Invoke("[Commit] Nothing to commit, working tree clean");
            if (!force)
                return;
        }

        TryWithAuth(() => _repo.Network.Push(
            remote,
            _repo.Head.CanonicalName,
            new PushOptions()
            {
                CredentialsProvider = GetCredentialsInternal(),
                OnPushStatusError = (e) => errorHandler.Invoke(e.Message),
                OnPushTransferProgress = pushHandler,
                OnPackBuilderProgress = packHandler,
            }
        ), errorHandler);
    }

    public void CommitAndPush(bool force, Action<string> logAction, bool addAll = true)
    {
        if (addAll)
            AddAll(logAction);

        var pushHandler = new PushTransferProgressHandler((current, total, bytes) =>
        {
            logAction($"[Push] {current}/{total} ({bytes} bytes)");
            return true;
        });

        var packHandler = new PackBuilderProgressHandler((stage, current, total) =>
        {
            logAction($"[Push/Pack] {stage}: {current}/{total}");
            return true;
        });

        var errorHandler = new Action<string>((e) => logAction("[Push/ERROR] " + e));
        var logHandler = new Action<string>((s) => logAction("[Push] " + s));

        CommitAndPush(force, pushHandler, packHandler, errorHandler, logHandler);
    }

    public string StatusString(Action<string> logHandler, bool addAll = true)
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

    public void AddAll(Action<string>? logAction)
    {
        logAction?.Invoke("[AddAll] Adding files. (This could take a long time!)");
        Commands.Unstage(_repo, "*");
        Commands.Stage(_repo, "*");
    }

    public void Stage(string path)
    {
        Commands.Stage(_repo, path);
    }

    public void Pull(Action<string> logHandler, bool addAll = true)
    {
        if (addAll)
            AddAll(logHandler);

        var options = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                CredentialsProvider = GetCredentialsInternal(),
                OnProgress = (s) =>
                {
                    logHandler("[Fetch]: " + s);
                    return true;
                },
                OnTransferProgress = (current) =>
                {
                    logHandler($"[Fetch]: {current}");
                    return true;
                },
                OnUpdateTips = (refName, oldId, newId) =>
                {
                    logHandler($"[Fetch]: {refName} {oldId} -> {newId}");
                    return true;
                }
            }
        };

        MergeResult result = TryWithAuth(() => Commands.Pull(_repo, _signature, options), logHandler);

        if (result.Status == MergeStatus.Conflicts)
        {
            logHandler("[Pull] Conflicts detected, please resolve them.");
        }
        else if (result.Status == MergeStatus.UpToDate)
        {
            logHandler("[Pull] Already up to date.");
        }
        else if (result.Status == MergeStatus.FastForward)
        {
            logHandler("[Pull] Fast forward merge.");
        }
        else
        {
            logHandler("[Pull] Merge completed.");
        }
    }
}