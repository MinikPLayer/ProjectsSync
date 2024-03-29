using LibGit2Sharp;
using PcSyncLib;

namespace PCSyncLibTests;

[TestFixture]
public class SyncDirectoryTests
{
    string _tempPath = Path.Combine(Path.GetTempPath(), "repos");

    [SetUp]
    public void Setup()
    {
        if(Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);

        SyncDirectory.Create(_tempPath);
        Assert.That(Directory.Exists(_tempPath));
        Assert.That(Directory.Exists(Path.Combine(_tempPath, ".pcsync")));
    }

    [Test]
    public void TestRepo()
    {
        var dir = SyncDirectory.Open(_tempPath);
        Assert.That(dir.Path, Is.EqualTo(_tempPath));
    }
}