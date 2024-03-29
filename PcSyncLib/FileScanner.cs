namespace PcSyncLib;

public static class FileScanner
{
    public struct Result
    {
        public struct FileResult
        {
            public string Path { get; set; }
            public bool Upload { get; set; }
            public bool Download { get; set; }
            public bool Conflict => Upload  && Download;

            public FileResult(string path, bool upload, bool download)
            {
                this.Path = path;
                this.Upload = upload;
                this.Download = download;
            }
        }

        public List<FileResult> Files { get; set; }

        public Result(List<FileResult> files)
        {
            this.Files = files;
        }
    }

    public static List<string> ScanModified(string path, DateTime lastSyncDate) =>
        ScanModified(path, lastSyncDate, "", new Ignore.Ignore());

    public static List<string> ScanModified(string basePath, DateTime lastSyncDate, string subPath, Ignore.Ignore ignore)
    {
        var path = Path.Combine(basePath, subPath);

        var gitIgnorePath = Path.Combine(path, ".gitignore");
        if (File.Exists(gitIgnorePath))
        {
            var ogRules = ignore.OriginalRules;
            ignore = new Ignore.Ignore();
            ignore.Add(ogRules);
            ignore.Add(File.ReadAllLines(gitIgnorePath));
        }

        var ret = new List<string>();
        foreach(var directory in Directory.GetDirectories(path))
        {
            var dirRelativePath = directory.Substring(basePath.Length + 1) + Path.DirectorySeparatorChar;
            if (ignore.IsIgnored(dirRelativePath))
                continue;

            var subFiles = ScanModified(basePath, lastSyncDate, dirRelativePath, ignore);
            if (subFiles.Count > 0)
                ret.AddRange(subFiles);
        }

        foreach(var file in Directory.GetFiles(path))
        {
            var fileRelativePath = file.Substring(basePath.Length + 1);
            if (ignore.IsIgnored(fileRelativePath))
                continue;

            var lastWriteTime = File.GetLastWriteTime(file);
            if (lastWriteTime > lastSyncDate)
                ret.Add(fileRelativePath);
        }

        return ret;
    }
}