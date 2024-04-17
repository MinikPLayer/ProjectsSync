using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LibGit2Sharp;
using ProjectsSyncLib;

namespace ProjectsSyncLibCLI;

[SupportedOSPlatform("Linux")]
[SupportedOSPlatform("Windows")]
static class Program
{
    static string AppVersionString = "";

    static bool USE_PASSWORD_SECURE_STORAGE = OperatingSystem.IsLinux();

    static Program()
    {
        if (string.IsNullOrEmpty(AppVersionString))
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion ?? "dev";
            AppVersionString = version;
        }
    }

    private static string GetPasswordFromUser()
    {
        Console.Write("Password: ");

        try
        {
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
        catch (InvalidOperationException)
        {
            Console.WriteLine("[Warning] Could not mask password. Using plain text method.");
            Console.Write("Password: ");
            return Console.ReadLine() ?? throw new Exception("Password not entered.");
        }

    }

    private static UsernamePasswordCredentials AskForCredentials(string url)
    {
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
        Console.WriteLine("ProjectsSync CLI interface.");
        Console.WriteLine("");

        PrintHelpEntry("prsync help", "Displays this help message.");
        PrintHelpEntry("prsync clone <url> [path]", "Clonse repository under <url> to the specified [path]. \nIf path is not specified a new directory is created with a name guessed from the <url>.");
        PrintHelpEntry("prsync pull [path]", "Pulls changes to repository under [path]. \nIf [path] is not specified current directory is used.");
        PrintHelpEntry("prsync push [path] [--force]", "Pushes changes to repository under [path]. \nIf [path] is not specified current directory is used.");
        PrintHelpEntry("prsync status [path]", "Displays status for repository under [path]. \nIf [path] is not specified current directory is used.");
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
        var signature = new Signature(machineName, "prsynccli@mtomecki.pl", DateTimeOffset.Now);
        string path = Directory.GetCurrentDirectory();
        var command = args[1];
        if (command == "version" || command == "--version")
        {
            Console.WriteLine($"Version: {AppVersionString}");
            return;
        }
        else if (command == "clone")
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: prsync clone <url> [path]");
                return;
            }

            var url = args[2];
            if (!url.EndsWith(".git"))
            {
                Console.WriteLine("[Error] Only HTTP/HTTPS protocol is supported. URL must end with .git");
                return;
            }

            if (args.Length > 3)
            {
                path = args[3];
            }
            else
            {
                // Try to guess directory name
                var splitted = url.Split('/');
                path = splitted.Last();
                path = path.Substring(0, path.Length - ".git".Length);

                if (string.IsNullOrEmpty(path))
                {
                    Console.WriteLine("[Error] Could not guess directory name. Please provide one.");
                    return;
                }

            }

            path = Path.GetFullPath(path);
            Console.WriteLine($"[Info] Using directory \"{path}\"");

            if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
            {
                Console.WriteLine("[Error] Directory already exists and is not empty.");
                return;
            }
            else
            {
                Directory.CreateDirectory(path);
            }

            var clonedRepo = SyncDirectory.Clone(
                url,
                path,
                signature,
                (url) =>
                {
                    var creds = AskForCredentials(url);
                    Console.WriteLine("Trying to clone... (This could take a long time!)");
                    return creds;
                },
                USE_PASSWORD_SECURE_STORAGE,
                (s) => Console.WriteLine("[Checkout] " + s)
            );
            Console.WriteLine("[Info] Repository cloned to " + clonedRepo.Path);
            return;
        }

        bool force = false;
        if (args.Length > 2)
        {
            if (args[2] == "--force")
            {
                force = true;
            }
            else
            {
                path = args[2];
                if (args.Length > 3 && args[3] == "--force")
                {
                    force = true;
                }
            }
        }

        Console.WriteLine($"[Info] Using directory \"{path}\"");

        SyncDirectory repo;
        try
        {
            repo = SyncDirectory.Open(path, signature, AskForCredentials, USE_PASSWORD_SECURE_STORAGE);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine("[Error] Repository open exception: " + e.Message);
            return;
        }

        switch (command)
        {
            case "status":
                Console.WriteLine("[Info] Generating repo status...\n");
                var statusStr = repo.StatusString(Console.WriteLine);
                Console.WriteLine("Status:\n" + statusStr);
                return;

            case "push":
                Console.WriteLine("[Info] Pushing changes...\n");
                repo.CommitAndPush(force, Console.WriteLine);
                Console.WriteLine("\n[Info] Pushing finished.");
                return;

            case "pull":
                Console.WriteLine("[Info] Pulling changes...\n");
                repo.Pull(Console.WriteLine);
                Console.WriteLine("\n[Info] Pulling finished.");
                return;

            default:
                PrintHelp();
                return;
        }
    }
}