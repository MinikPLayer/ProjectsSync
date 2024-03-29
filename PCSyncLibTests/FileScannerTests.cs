using PcSyncLib;

namespace PCSyncLibTests;

[TestFixture]
public class FileScannerTests
{
    private static string _testPath = Path.Combine(Path.GetTempPath(), "PcSyncAvLibTest");

    [SetUp]
    public void Setup()
    {
        // Create test directory
        if(Directory.Exists(_testPath))
            Directory.Delete(_testPath, true);

        Directory.CreateDirectory(_testPath);
        File.WriteAllText(Path.Combine(_testPath, "file1.txt"), "File 1 :>");
        File.WriteAllText(Path.Combine(_testPath, "file2.txt"), "Hello, world! :>");
        File.WriteAllText(Path.Combine(_testPath, "ignored_file.txt"), "Ignored file contents");
        File.WriteAllText(Path.Combine(_testPath, ".gitignore"), "ignored_file.txt\ngit_ignored/");
        File.WriteAllText(Path.Combine(_testPath, "not_ignored_file.ign"), "This file should NOT be ignored.");

        var curDir = Path.Combine(_testPath, "test_dir1");
        Directory.CreateDirectory(curDir);
        File.WriteAllText(Path.Combine(curDir, "td_file1.txt"), "Test dir file 1 :>");
        File.WriteAllText(Path.Combine(curDir, "td_file2.txt"), "Test dir file 2 :>");
        File.WriteAllText(Path.Combine(curDir, "td_file3.txt"), "Test dir file 3 :>");

        curDir = Path.Combine(_testPath, "test_dir2");
        Directory.CreateDirectory(curDir);
        File.WriteAllText(Path.Combine(curDir, "td_file1.txt"), "Test dir file 1 :>");
        File.WriteAllText(Path.Combine(curDir, ".gitignore"), "*.ign");
        File.WriteAllText(Path.Combine(curDir, "ignored_file.ign"), "This file SHOULD be ignored");


        curDir = Path.Combine(curDir, "git_ignored");
        Directory.CreateDirectory(curDir);
        File.WriteAllText(Path.Combine(curDir, "ignored_dir_file.txt"), "Ignored dir file 1");
    }

    [Test]
    public void TestScanAll()
    {
        var lastSyncDate = DateTime.MinValue;
        var files = FileScanner.ScanModified(_testPath, lastSyncDate);
        var filesStr = string.Join(", ", files);
        Console.WriteLine("Got files: " + filesStr);

        Assert.That(files, Contains.Item("file1.txt"));
        Assert.That(files, Contains.Item("file2.txt"));
        Assert.That(files, Contains.Item(".gitignore"));
        Assert.That(files, Contains.Item("not_ignored_file.ign"));

        Assert.That(files, Contains.Item(Path.Combine("test_dir1", "td_file1.txt")));
        Assert.That(files, Contains.Item(Path.Combine("test_dir1", "td_file2.txt")));
        Assert.That(files, Contains.Item(Path.Combine("test_dir1", "td_file3.txt")));
        Assert.That(files, Contains.Item(Path.Combine("test_dir2", "td_file1.txt")));
        Assert.That(files, Contains.Item(Path.Combine("test_dir2", ".gitignore")));

        Assert.That(files, !Contains.Item(Path.Combine("test_dir2", "ignored_file.ign")));
        Assert.That(files, !Contains.Item(Path.Combine("test_dir2", "git_ignored", "ignored_dir_file.txt")));
    }

    [Test]
    public void TestScanModified()
    {
        var now = DateTime.Now;

        var files = FileScanner.ScanModified(_testPath, now);
        Assert.That(files.Count, Is.EqualTo(0));

        var file1Path = Path.Combine(_testPath, "file1.txt");
        var ignoredFilePath = Path.Combine(_testPath, "ignored_file.txt");
        File.AppendAllText(file1Path, "Modified file 1");
        File.AppendAllText(ignoredFilePath, "Modified, but ignored file");

        files = FileScanner.ScanModified(_testPath, now);
        Assert.That(files.Count, Is.EqualTo(1));
        Assert.That(files, Contains.Item("file1.txt"));
        Assert.That(files, !Contains.Item("test_dir2/git_ignored/ignored_dir_file.txt"));
    }
}