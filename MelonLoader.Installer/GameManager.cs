using Avalonia.Media.Imaging;
using MelonLoader.Installer.GameLaunchers;
using MelonLoader.Installer.ViewModels;
using Semver;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

namespace MelonLoader.Installer;

internal static class GameManager
{
    private static bool inited;

    public static ObservableCollection<GameModel> Games { get; } = [];

    public static void Init()
    {
        if (inited)
            return;

        inited = true;

        foreach (var launcher in GameLauncher.Launchers)
        {
            launcher.AddGames();
        }

        LoadSavedGames();
    }

    private static void LoadSavedGames()
    {
        foreach (var gamePath in PathManager.LoadGameList())
        {
            TryAddGame(gamePath, null, null, null, out _);
        }

        // In case it was manually edited or if any games were removed
        SaveManualGameList();
    }

    public static void SaveManualGameList()
    {
        PathManager.SaveGameList(Games.Where(x => x.Launcher == null).Select(x => x.Path));
    }

    private static void AddGameSorted(GameModel game)
    {
        var gameIsProt = game.IsProtected;
        var gameHasMl = game.MLVersion != null;
        for (var i = 0; i < Games.Count; i++)
        {
            var iIsProt = Games[i].IsProtected;
            if (gameIsProt && !iIsProt)
                continue;

            var iHasMl = Games[i].MLVersion != null;
            if (gameHasMl && !iHasMl)
            {
                Games.Insert(i, game);
                return;
            }
            if (!gameHasMl && iHasMl)
                continue;

            if (string.Compare(game.Name, Games[i].Name, StringComparison.OrdinalIgnoreCase) <= 0)
            {
                Games.Insert(i, game);
                return;
            }
        }

        Games.Add(game);
    }

    public static void ResortGame(GameModel game)
    {
        if (!Games.Remove(game))
            return;

        AddGameSorted(game);
    }

    public static void RemoveGame(GameModel game)
    {
        Games.Remove(game);
    }

    private static void GetDataDirectories(
        string path,
        bool searchAllDirectories,
        out string? exeExt,
        out int skipCount, 
        out List<string> dataDirs)
    {
        exeExt = null;
        skipCount = 5;
        dataDirs = new();
        
        ReadOnlySpan<string> extensions = [".app", ".exe", ".x86_64", ""];
        foreach (var referenceExt in extensions)
        {
            exeExt = referenceExt;
            skipCount = referenceExt == ".app" ? 4 : 5;
            var searchPattern = referenceExt == ".app" ? $"*{exeExt}" : "*_Data";
            Func<string?, bool> exists = referenceExt == ".app" ? Directory.Exists : File.Exists;
            
            int foundDirectoriesLength = 0;
            IEnumerable<string> foundDirectories = [];
            try
            {
                foundDirectories = Directory.EnumerateDirectories(path, searchPattern, searchAllDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foundDirectoriesLength = foundDirectories.Count();
            }
            catch
            {
                return;
            }

            for (int i = 0; i < foundDirectoriesLength; i++)
            {
                try
                {
                    string x = foundDirectories.ElementAt(i);
                    string candidate = x[..^skipCount] + referenceExt;
                    if (exists(candidate))
                        dataDirs.Add(x);
                }
                catch { }
            }

            if (dataDirs.Any())
                break;
        }
    }

    public static bool ValidateGame(ref string path, out string? exe, out string? exeExt, out string? errorMessage)
    {
        exe = null;
        exeExt = null;
        errorMessage = null;

        // Parse Path to Base Game Directory
        if (File.Exists(path) || (Directory.Exists(path) && path.EndsWith(".app")))
        {
            path = Path.GetDirectoryName(path)!;
        }
        else if (!Directory.Exists(path))
        {
            errorMessage = "The selected directory does not exist.";
            return false;
        }

        // Make sure to get Full Path
        path = Path.GetFullPath(path);

        // Validate Directory contains Applications
        GetDataDirectories(path, false, out exeExt, out int skipCount, out List<string> dataDirs);
        if (!dataDirs.Any())
            GetDataDirectories(path, true, out exeExt, out skipCount, out dataDirs);
        if (!dataDirs.Any())
        {
            errorMessage = "The selected directory does not contain a Unity game.";
            return false;
        }
        
        // Validate Directory only contains 1 Application
        if (dataDirs.Count() > 1)
        {
            errorMessage = "The selected directory contains multiple Unity games?";
            return false;
        }

        // Get Executable and Return
        exe = dataDirs.First()[..^skipCount] + exeExt;
        path = Path.GetFullPath(Path.GetDirectoryName(exe)!);
        return true;
    }

    public static GameModel? TryAddGame(string path, string? customName, GameLauncher? launcher, string? iconPath, out string? errorMessage)
    {
        // Validate Game
        string gameDir = path;
        if (!ValidateGame(ref gameDir, out var exe, out var exeExt, out errorMessage))
        {
            if (errorMessage == null)
                errorMessage = "Game failed Validation.";
            return null;
        }

        // Check for Duplicate
        if (Games.Any(x => x.Path.Equals(exe, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "Game is already listed.";
            return null;
        }

        // Get Architecture of Game
        Architecture arch = GetGameArchitecture(exe!, exeExt!);
        if (arch == Architecture.Unknown)
        {
            errorMessage = "The game executable is invalid (possibly corrupted).";
            return null;
        }

        // Get Installed MelonLoader Version Information
        string? proxyName = null;
        SemVersion? foundVersion = null;
        var mlVersion = MLVersion.GetMelonLoaderVersion(gameDir, out var mlArch, out errorMessage);
        if ((mlVersion != null)
            && (arch == mlArch))
        {
            foundVersion = mlVersion.Value.Item1;
            proxyName = mlVersion.Value.Item2;
        }

        // Get Icon
        Bitmap? icon = null;
        if (iconPath != null && File.Exists(iconPath))
        {
            try
            {
                icon = new Bitmap(iconPath);
            }
            catch { }
        }
#if WINDOWS
        if ((arch == Architecture.WindowsX64) || (arch == Architecture.WindowsX86))
            icon ??= IconExtractor.GetExeIcon(exe!);
#endif

        // Check for EAC Protection
        var isProtected = Directory.Exists(Path.Combine(path, "EasyAntiCheat"));

        // Create New Result
        var result = new GameModel(exe!, (customName ?? Path.GetFileNameWithoutExtension(exe))!, arch, launcher, icon, foundVersion, proxyName, isProtected);
        errorMessage = null;
        AddGameSorted(result);
        return result;
    }

    private static Architecture GetGameArchitecture(string exe, string exeExt)
    {
        var result = exeExt switch
        {
            ".app" => Architecture.MacOSX64,
            ".exe" => MLVersion.ReadFromPE(exe),
            ".x86_64" => Architecture.LinuxX64,
            _ => Architecture.Unknown
        };

        if ((result == Architecture.MacOSX64)
            || (result == Architecture.MacOSArm64))
        {
            var unityPlayerPath = Path.Combine(exe, "Contents/Frameworks/UnityPlayer.dylib");
            if (File.Exists(unityPlayerPath))
                result = MLVersion.ReadFromMachO(unityPlayerPath);
            else
            {
                var unityPlayerPath2 = Path.Combine(Path.GetDirectoryName(exe)!, "Contents/Frameworks/libUnityPlayer.dylib");
                if (File.Exists(unityPlayerPath2))
                    result = MLVersion.ReadFromMachO(unityPlayerPath2);
            }
        }

        if (result == Architecture.Unknown)
        {
            var unityPlayerPath = Path.Combine(Path.GetDirectoryName(exe)!, "UnityPlayer.dll");
            if (File.Exists(unityPlayerPath))
                result = MLVersion.ReadFromPE(unityPlayerPath);
            else
            {
                var unityPlayerLinuxPath = Path.Combine(Path.GetDirectoryName(exe)!, "UnityPlayer.so");
                if (File.Exists(unityPlayerLinuxPath))
                    result = MLVersion.ReadFromELF(unityPlayerLinuxPath);
                else
                {
                    var unityPlayerLinuxPath2 = Path.Combine(Path.GetDirectoryName(exe)!, "libUnityPlayer.so");
                    if (File.Exists(unityPlayerLinuxPath2))
                        result = MLVersion.ReadFromELF(unityPlayerLinuxPath2);
                }
            }
        }

        return result;
    }
}
