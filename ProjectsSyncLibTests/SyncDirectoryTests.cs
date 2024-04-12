using System.Diagnostics;
using LibGit2Sharp;
using ProjectsSyncLib;

namespace PRSyncLibTests;

[TestFixture]
public class SyncDirectoryTests
{
    string _tempPath = Path.Combine(Path.GetTempPath(), "repos");

    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);

        SyncDirectory.Create(_tempPath);
        Assert.That(Directory.Exists(_tempPath));
        Assert.That(Directory.Exists(Path.Combine(_tempPath, ".prsync")));
    }

    [Test]
    public void TestRepo()
    {
        var dir = SyncDirectory.Open(_tempPath);
        Assert.That(dir.Path, Is.EqualTo(_tempPath));
    }

    [Test]
    public void TestGitignore()
    {
        var repo = SyncDirectory.Open(_tempPath);
        var gitignore = Path.Combine(_tempPath, ".gitignore");
        var notIgnoredPath = Path.Combine(_tempPath, "not_ignored_file.txt");
        var ignoredPath = Path.Combine(_tempPath, "ignored_file.txt");
        var ignoredDirPath = Path.Combine(_tempPath, "ignored_directory", "ignored_dir_file.txt");
        var notIgnoredDirPath = Path.Combine(_tempPath, "not_ignored_directory", "not_ignored_dir_file.txt");

        File.WriteAllText(gitignore, "ignored_file.txt\nignored_directory/");
        File.WriteAllText(notIgnoredPath, "Not ignored content");
        File.WriteAllText(ignoredPath, "Ignored content");
        Directory.CreateDirectory(Path.Combine(_tempPath, "ignored_directory"));
        File.WriteAllText(ignoredDirPath, "Ignored Dir content");
        Directory.CreateDirectory(Path.Combine(_tempPath, "not_ignored_directory"));
        File.WriteAllText(notIgnoredDirPath, "Not Ignored Dir content");

        repo.Add(".");
        var status = repo.Status();
        Assert.That(status.Ignored.FirstOrDefault(x => x.FilePath == "ignored_file.txt"), Is.Not.Null);
        Assert.That(status.Ignored.FirstOrDefault(x => x.FilePath == "ignored_directory/"), Is.Not.Null);

        Assert.That(status.Untracked.FirstOrDefault(x => x.FilePath == "not_ignored_file.txt"), Is.Not.Null);
    }

    [Test]
    public void TestStatus()
    {
        var dir = SyncDirectory.Open(_tempPath);
        var status = dir.Status();
        Assert.That(status, Is.Not.Null);

        var statusString = dir.StatusString();
        Assert.That(statusString, Is.Not.Null);
        Console.WriteLine(statusString);
    }
}