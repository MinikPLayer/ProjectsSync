using System.Runtime.Versioning;
using System.Text.Json;
using LibGit2Sharp;
using Simple.CredentialManager;
using CM = Simple.CredentialManager;

[SupportedOSPlatform("Linux")]
[SupportedOSPlatform("Windows")]
public static class SecureStorage
{
    const string COLLECTION_NAME = "prsync";

    public static bool IsSupportedOS()
    {
        return OperatingSystem.IsLinux() || OperatingSystem.IsWindows();
    }

    static ICredential GetCredential(string url, string credsJson)
    {
        string tag = $"PRSync credentials for \"{url}\"";
        if(OperatingSystem.IsLinux())
        {
            return new LinuxCredential(url, credsJson, tag, collection: COLLECTION_NAME);
        }
        else if(OperatingSystem.IsWindows())
        {
            var creds = new WinCredential(url, credsJson, tag, CredentialType.Generic);
            creds.PersistenceType = PersistenceType.LocalComputer;
            return creds;
        }
        else
        {
            throw new PlatformNotSupportedException("This platform is not supported.");
        }
    }

    public static bool TrySavePassword(UsernamePasswordCredentials userCredentials, string url)
    {
        try
        {
            SavePassword(userCredentials, url);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void SavePassword(UsernamePasswordCredentials userCredentials, string url)
    {
        var credsJson = JsonSerializer.Serialize(userCredentials);
        var creds = GetCredential(url, credsJson);
        var result = creds.Save();
        if (!result)
            throw new Exception("Failed to save credentials.");

    }

    public static bool TryGetPassword(string url, out UsernamePasswordCredentials? creds)
    {
        creds = null;
        try
        {
            creds = GetPassword(url);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static UsernamePasswordCredentials GetPassword(string url)
    {
        var creds = GetCredential(url, "");
        var result = creds.Load();
        if (!result)
            throw new Exception("Failed to load credentials.");

        var pass = creds.Password;
        var userCreds = JsonSerializer.Deserialize<UsernamePasswordCredentials>(pass);
        if (userCreds == null)
            throw new Exception("Failed to parse credentials.");

        return userCreds;
    }

    public static bool PasswordExists(string url)
    {
        var creds = GetCredential(url, "");
        return creds.Exists();
    }

    public static bool ClearPassword(string url)
    {
        var creds = GetCredential(url, "");
        return creds.Delete();
    }
}