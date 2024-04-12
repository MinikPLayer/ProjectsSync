using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public struct AppConfig
{
    public static string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "prsync");

    public string RepoPath { get; set; }
    public string EmailAddress { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this);
    public static AppConfig? FromJson(string json) => JsonSerializer.Deserialize<AppConfig>(json);

    public void Save()
    {
        var path = DefaultPath;

        var dir = Path.GetDirectoryName(path);
        if (dir == null)
            throw new Exception("Invalid path");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, ToJson());
    }

    public static AppConfig Load()
    {
        var path = DefaultPath;

        var jsonContent = File.ReadAllText(path);
        var config = FromJson(jsonContent);
        if (config == null)
            return new AppConfig();

        return config.Value;
    }
}