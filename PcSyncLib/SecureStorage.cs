using System.Runtime.Versioning;
using System.Text.Json;
using LibGit2Sharp;
using CM = Simple.CredentialManager;

[SupportedOSPlatform("Linux")]
public static class SecureStorage {
    const string COLLECTION_NAME = "pcsync2";

    public static bool TrySavePassword(UsernamePasswordCredentials userCredentials, string url) {
        try {
            SavePassword(userCredentials, url);
            return true;
        }
        catch(Exception) {
            return false;
        }
    }

    public static void SavePassword(UsernamePasswordCredentials userCredentials, string url) {
        var credsJson = JsonSerializer.Serialize(userCredentials);
        var creds = new CM.LinuxCredential(url, credsJson, $"PCSync2 credentials for \"{url}\"", collection: COLLECTION_NAME);
        var result = creds.Save();
        if(!result)
            throw new Exception("Failed to save credentials.");
    
    }

    public static bool TryGetPassword(string url, out UsernamePasswordCredentials? creds) {
        creds = null;
        try {
            creds = GetPassword(url);
            return true;
        }
        catch(Exception) {
            return false;
        }
    }

    public static UsernamePasswordCredentials GetPassword(string url) {
        var creds = new CM.LinuxCredential(url, "", "", collection: COLLECTION_NAME);
        var result = creds.Load();
        if(!result)
            throw new Exception("Failed to load credentials.");
        
        var pass = creds.Password;
        var userCreds = JsonSerializer.Deserialize<UsernamePasswordCredentials>(pass);
        if(userCreds == null)
            throw new Exception("Failed to parse credentials.");

        return userCreds;
    }
}