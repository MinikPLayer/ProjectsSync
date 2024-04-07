using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using LibGit2Sharp;
using PcSyncLib;

namespace PcSyncLibCLI;
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
        Console.WriteLine("\n=================  Auth  ===================");
        Console.WriteLine("Authentication required for " + url);
        Console.WriteLine("(Consider using SSH authentication)");
        Console.Write("Username: ");
        var username = Console.ReadLine();
        var password = GetPasswordFromUser();
        Console.WriteLine("============================================\n");
        return new UsernamePasswordCredentials
        {
            Username = username,
            Password = password
        };
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("\tpcsync clone <url> [path]");
        Console.WriteLine("\tpcsync <command> [path]");
    }
 
    static void Main()
    {
        var workingCwd = Directory.GetCurrentDirectory();
        Console.WriteLine("Current working directory: " + workingCwd);

        var args = Environment.GetCommandLineArgs();
        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        var machineName = Environment.MachineName;
        var signature = new Signature(machineName, "pcsynccli@mtomecki.pl", DateTimeOffset.Now);
        string path = Directory.GetCurrentDirectory();
        var command = args[1];
        if(command == "clone") {
            if(args.Length < 3) {
                Console.WriteLine("Usage: pcsync clone <url> [path]");
                return;
            }
            
            var url = args[2];
            if(args.Length > 3) {
                path = args[3];
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
            Console.WriteLine("Repository open: " + e.Message);
            try {
                repo = SyncDirectory.Create(path, signature, AskForCredentials);
                Console.WriteLine("Repository created at " + path);
                Console.Write("Enter repository URL: ");
                var url = Console.ReadLine();
                if(url == null) {
                    Console.WriteLine("URL not entered.");
                    return;
                }
                repo.AddRemote(url);
                
            } catch (Exception e2) {
                Console.WriteLine("Repository create: " +  e2.Message);
                return;
            }
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
                PrintUsage();
                return;
        }
    }
}