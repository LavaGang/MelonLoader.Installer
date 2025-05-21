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

    public static GameModel? TryAddGame(string path, string? customName, GameLauncher? launcher, string? iconPath, [NotNullWhen(false)] out string? errorMessage)
    {
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path)!;
        }
        else if (!Directory.Exists(path))
        {
            errorMessage = "The selected directory does not exist.";
            return null;
        }

        path = Path.GetFullPath(path);

        var arch = Architecture.Unknown;

        var rawDataDirs = Directory.GetDirectories(path, "*_Data");
        var dataDirs = rawDataDirs.Where(x => File.Exists(x[..^5] + ".exe"));
        if (!dataDirs.Any())
        {
            dataDirs = rawDataDirs.Where(x => File.Exists(x[..^5] + ".x86_64"));
            if (dataDirs.Any())
            {
                arch = Architecture.LinuxX64;
            }
            else
            {
                errorMessage = "The selected directory does not contain a Unity game.";
                return null;
            }
        }

        if (dataDirs.Count() > 1)
        {
            errorMessage = "The selected directory contains multiple Unity games?";
            return null;
        }

        var exe = dataDirs.First()[..^5] + (arch == Architecture.LinuxX64 ? ".x86_64" : ".exe");

        if (Games.Any(x => x.Path.Equals(exe, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "Game is already listed.";
            return null;
        }

        if (arch == Architecture.Unknown)
        {
            try
            {
                using var pe = new PEReader(File.OpenRead(exe));
                arch = pe.PEHeaders.CoffHeader.Machine == Machine.Amd64 ? Architecture.WindowsX64 : Architecture.WindowsX86;
            }
            catch
            {
                var unityPlayerPath = Path.Combine(path, "UnityPlayer.dll");
                if (File.Exists(unityPlayerPath))
                {
                    try
                    {
                        using var pe = new PEReader(File.OpenRead(unityPlayerPath));
                        arch = pe.PEHeaders.CoffHeader.Machine == Machine.Amd64 ? Architecture.WindowsX64 : Architecture.WindowsX86;
                    }
                    catch { }
                }
            }

            if (arch == Architecture.Unknown)
            {
                errorMessage = "The game executable is invalid (possibly corrupted).";
                return null;
            }
        }

        var mlVersion = MLVersion.GetMelonLoaderVersion(path, out var mlArch);
        if (mlVersion != null && mlArch != arch)
            mlVersion = null;

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
        if (arch != Architecture.LinuxX64)
            icon ??= IconExtractor.GetExeIcon(exe);
#endif

        var isProtected = Directory.Exists(Path.Combine(path, "EasyAntiCheat"));

        var result = new GameModel(exe, customName ?? Path.GetFileNameWithoutExtension(exe), arch, launcher, icon, mlVersion, isProtected);
        errorMessage = null;

        AddGameSorted(result);

        return result;
    }
}
