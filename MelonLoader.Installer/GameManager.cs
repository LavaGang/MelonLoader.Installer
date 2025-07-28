using Avalonia.Media.Imaging;
using MelonLoader.Installer.GameLaunchers;
using MelonLoader.Installer.ViewModels;
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
        foreach (var gamePath in Config.LoadGameList())
        {
            TryAddGame(gamePath, null, null, null, out _);
        }

        // In case it was manually edited or if any games were removed
        SaveManualGameList();
    }

    public static void SaveManualGameList()
    {
        Config.SaveGameList(Games.Where(x => x.Launcher == null).Select(x => x.Path));
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
        exeExt = ".app";
        string? referenceExt = exeExt;
        int skipCount = 4;
        var rawDataDirs = Directory.GetDirectories(path, $"*{exeExt}");
        var dataDirs = rawDataDirs.Where(x => Directory.Exists(x[..^skipCount] + referenceExt));
        if (!dataDirs.Any())
        {
            exeExt = ".exe";
            referenceExt = exeExt;
            skipCount = 5;
            rawDataDirs = Directory.GetDirectories(path, "*_Data");
            dataDirs = rawDataDirs.Where(x => File.Exists(x[..^skipCount] + referenceExt));
            if (!dataDirs.Any())
            {
                exeExt = ".x86_64";
                referenceExt = exeExt;
                dataDirs = rawDataDirs.Where(x => File.Exists(x[..^skipCount] + referenceExt));
                if (!dataDirs.Any())
                {
                    errorMessage = "The selected directory does not contain a Unity game.";
                    return false;
                }
            }
        }

        // Validate Directory only contains 1 Application
        if (dataDirs.Count() > 1)
        {
            errorMessage = "The selected directory contains multiple Unity games?";
            return false;
        }

        // Get Executable and Return
        exe = dataDirs.First()[..^skipCount] + exeExt;
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
        var mlVersion = MLVersion.GetMelonLoaderVersion(gameDir, out var mlArch, out errorMessage);
        if (mlVersion != null && mlArch != arch)
            mlVersion = null;

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
        var result = new GameModel(exe!, (customName ?? Path.GetFileNameWithoutExtension(exe))!, arch, launcher, icon, mlVersion, isProtected);
        errorMessage = null;
        AddGameSorted(result);
        return result;
    }

    private static Architecture GetGameArchitecture(string exe, string exeExt)
    {
        Architecture result = Architecture.Unknown;
        switch (exeExt)
        {
            case ".app":
                result = Architecture.MacOSX64;
                break;

            case ".exe":
                result = MLVersion.ReadFromPE(exe);
                break;

            case ".x86_64":
                result = Architecture.LinuxX64;
                break;

            default:
                break;
        }

        if (result == Architecture.Unknown)
        {
            var unityPlayerPath = Path.Combine(Path.GetDirectoryName(exe)!, "UnityPlayer.dll");
            if (File.Exists(unityPlayerPath))
                result = MLVersion.ReadFromPE(unityPlayerPath);
        }

        return result;
    }
}
