using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LibGit2Sharp;
using PcSyncLib;

namespace PcSyncLibCLI;

[SupportedOSPlatform("Linux")]
static class Program
{       

    private static string GetPasswordFromUser()
    {
        Console.Write("Password: ");

        try {
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            Console.WriteLine();
            return pass;
        }
        catch(InvalidOperationException) {
            Console.WriteLine("[Warning] Could not mask password. Using plain text method.");
            Console.Write("Password: ");
            return Console.ReadLine() ?? throw new Exception("Password not entered.");
        }

    }

    // TODO: Add saving credentials using libsecret.
    private static UsernamePasswordCredentials AskForCredentials(string url, SupportedCredentialTypes types)
    {
        if(SecureStorage.TryGetPassword(url, out var secureCreds))
            return secureCreds!;

        Console.WriteLine("\n=================  Auth  ===================");
        Console.WriteLine("Authentication required for " + url);
        Console.Write("Username: ");
        var username = Console.ReadLine();
        var password = GetPasswordFromUser();
        Console.WriteLine("============================================\n");

        var creds = new UsernamePasswordCredentials
        {
            Username = username,
            Password = password
        };

        if(SecureStorage.TrySavePassword(creds, url))
        {
            Console.WriteLine("[Info] Credentials saved using SecureStorage.");    
        }
        else 
        {
            Console.WriteLine("[Warning] Failed to save credentials using SecureStorage.");
        }

        return creds;
    }

    static void PrintHelpEntry(string header, string description) 
    {
        Console.WriteLine($"\t- {header}");
        description = description.Replace("\n", "\n\t\t");
        Console.WriteLine($"\t\t{description}");
        Console.WriteLine("");
    }

    static void PrintHelp()
    {
        Console.WriteLine("PCSync CLI interface.");
        Console.WriteLine("");

        PrintHelpEntry("pcsync help", "Displays this help message.");
        PrintHelpEntry("pcsync clone <url> [path]", "Clonse repository under <url> to the specified [path]. \nIf path is not specified a new directory is created with a name guessed from the <url>.");
        PrintHelpEntry("pcsync pull [path]", "Pulls changes to repository under [path]. \nIf [path] is not specified current directory is used.");
        PrintHelpEntry("pcsync push [path]", "Pushes changes to repository under [path]. \nIf [path] is not specified current directory is used.");
        PrintHelpEntry("pcsync status [path]", "Displays status for repository under [path]. \nIf [path] is not specified current directory is used.");
    }

    static void Main()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length < 2 || args[1] == "help")
        {
            PrintHelp();
            return;
        }

        var machineName = Environment.MachineName;
        var signature = new Signature(machineName, "pcsynccli@mtomecki.pl", DateTimeOffset.Now);
        string path = Directory.GetCurrentDirectory();
        var command = args[1];
        if(command == "clone" ) {
            if(args.Length < 3) {
                Console.WriteLine("Usage: pcsync clone <url> [path]");
                return;
            }
            
            var url = args[2];
            if(!url.EndsWith(".git")) {
                Console.WriteLine("Only HTTP/HTTPS protocol is supported. URL must end with .git");
                return;
            }
            
            if(args.Length > 3) {
                path = args[3];
            }
            else {
                // Try to guess directory name
                var splitted = url.Split('/');
                var dirName = splitted.Last();
                dirName = dirName.Substring(0, dirName.Length - ".git".Length);

                if(string.IsNullOrEmpty(dirName))
                {
                    Console.WriteLine("Could not guess directory name. Please provide one.");
                    return;
                }
            }

            if(Directory.EnumerateFileSystemEntries(path).Any()) {
                Console.WriteLine("Directory already exists and is not empty.");
                return;
            }

            var clonedRepo = SyncDirectory.Clone(url, path, signature, AskForCredentials, (s, _, _) => Console.WriteLine("[Checkout] " + s));
            Console.WriteLine("Repository cloned to " + clonedRepo.Path);
            return;
        }

        if(args.Length > 2) {
            path = args[2];
        }

        SyncDirectory repo;
        try
        {
            repo = SyncDirectory.Open(path, signature, AskForCredentials);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine("Repository open exception: " + e.Message);
            return;
        }

        File.AppendAllText(Path.Combine(repo.Path, "test.txt"), DateTime.Now.ToString());
        repo.AddAll();

        switch(command)
        {
            case "status":
                Console.WriteLine("Generating repo status...");
                Console.WriteLine(repo.StatusString());
                return;

            case "push":
                Console.WriteLine("Pushing changes...");
                repo.CommitAndPush((s) => Console.WriteLine("LOG: " + s));
                Console.WriteLine("Pushing finished.");
                return;

            case "pull":
                Console.WriteLine("Pulling changes...");
                repo.Pull((s) => Console.WriteLine("LOG: " + s));
                Console.WriteLine("Pulling finished.");
                return;

            default:
                PrintHelp();
                return;
        }
    }
}