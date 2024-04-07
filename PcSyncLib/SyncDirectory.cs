using System.Reflection;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace PcSyncLib;

public class SyncDirectory
{
    public delegate Credentials UserProvidedCredentialsHandler(string url, SupportedCredentialTypes types);

    private Repository _repo;
    private Signature _signature;
    private UserProvidedCredentialsHandler _credentialsHandler;
    public string Path { get; }

    private static CredentialsHandler GetCredentialsProvider(UserProvidedCredentialsHandler handler) => new CredentialsHandler((url, usernameFromUrl, types) => handler.Invoke(url, types));

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

    public static SyncDirectory Create(string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider)
    {
        if (Repository.IsValid(path))
            throw new ArgumentException("Repository already exists at " + path, nameof(path));

        Repository.Init(path);
        return new SyncDirectory(path, signature, credentialsProvider);
    }

    public static SyncDirectory Open(string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider)
    {
        if (!Repository.IsValid(path))
            throw new ArgumentException("Repository not found at " + path, nameof(path));

        return new SyncDirectory(path, signature, credentialsProvider);
    }

    public static SyncDirectory Clone(string url, string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider, CheckoutProgressHandler? checkoutProgressHandler = null)
    {
        var cloneOptions = new CloneOptions();
        cloneOptions.FetchOptions.CredentialsProvider = GetCredentialsProvider(credentialsProvider);
        cloneOptions.OnCheckoutProgress = checkoutProgressHandler;

        var repo = Repository.Clone(url, path, cloneOptions);
        if (repo == null)
            throw new InvalidOperationException("Failed to clone repository.");

        return new SyncDirectory(path, signature, credentialsProvider);
    }

    private SyncDirectory(string path, Signature signature, UserProvidedCredentialsHandler credentialsProvider)
    {
        Path = path;
        _repo = new Repository(path);
        _signature = signature;
        _credentialsHandler = credentialsProvider;
    }

    public void AddRemote(string url)
    {
        _repo.Network.Remotes.Add("origin", url);
    }

    public void CommitAndPush(PushTransferProgressHandler? pushHandler = null, PackBuilderProgressHandler? packHandler = null, Action<string>? errorHandler = null)
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
                errorHandler?.Invoke("ERROR: Failed to commit - " + e.Message);
            }
        }
        else
        {
            errorHandler?.Invoke("Nothing to commit, working tree clean");
        }

        _repo.Network.Push(
            remote, 
            _repo.Head.CanonicalName, 
            new PushOptions() { 
                CredentialsProvider = GetCredentialsProvider(_credentialsHandler), 
                OnPushStatusError = (e) => errorHandler?.Invoke(e.Message),
                OnPushTransferProgress = pushHandler,
                OnPackBuilderProgress = packHandler,
            }
        );


    }

    public void CommitAndPush(Action<string> logAction)
    {
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

        var errorHandler = new Action<string>((e) => logAction("[Push ERROR] " + e));

        CommitAndPush(pushHandler, packHandler, errorHandler);
    }

    public string StatusString()
    {

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

    public void AddAll()
    {
        Commands.Unstage(_repo, "*");
        Commands.Stage(_repo, "*");
    }

    public void Stage(string path)
    {
        Commands.Stage(_repo, path);
    }

    public void Test()
    {
        using var _repo = new Repository(Path);

        Commands.Unstage(_repo, "*");
        Commands.Stage(_repo, "*");
        var status = _repo.RetrieveStatus();
        Console.WriteLine(_repo);
        Console.WriteLine(StatusString());
    }

    public void Pull(Action<string> logHandler)
    {
        var options = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                CredentialsProvider = GetCredentialsProvider(_credentialsHandler),
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

        var result = Commands.Pull(_repo, _signature, options);
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