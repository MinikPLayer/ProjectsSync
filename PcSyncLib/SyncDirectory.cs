using LibGit2Sharp;

namespace PcSyncLib;

public class SyncDirectory
{
    public string Path { get; }

    static SyncDirectory()
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("Platform not supported.");
        }
        LibGit2Sharp.GlobalSettings.NativeLibraryPath = "./lib";
    }

    private SyncDirectory(string path)
    {
        Path = path;
    }

    public static SyncDirectory Create(string path)
    {
        if (Repository.IsValid(path))
            throw new ArgumentException("Repository already exists", nameof(path));

        Repository.Init(path);
        return new SyncDirectory(path);
    }

    public static SyncDirectory Open(string path)
    {
        if(!Repository.IsValid(path))
            throw new ArgumentException("Repository not found at ", nameof(path));

        return new SyncDirectory(path);
    }
}