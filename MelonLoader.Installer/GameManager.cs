﻿using Avalonia.Media.Imaging;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using MelonLoader.Installer.ViewModels;
using NexusMods.Paths;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

namespace MelonLoader.Installer;

internal static class GameManager
{
    private static bool inited;

    public static ObservableCollection<GameModel> Games { get; private set; } = [];

    public static void Init()
    {
        if (inited)
            return;

        inited = true;

        LoadSteamGames();
        LoadSavedGames();
    }

    private static void LoadSavedGames()
    {
        foreach (var gamePath in Config.LoadGameList())
        {
            TryAddGame(gamePath, null, GameSource.Manual, null, out _);
        }

        // In case it was manually edited or if any games were removed
        SaveManualGameList();
    }

    private static void LoadSteamGames()
    {
        var steam = new SteamHandler(FileSystem.Shared, WindowsRegistry.Shared);
        var games = steam.FindAllGames().Where(x => x.IsT0).Select(x => x.AsT0);
        foreach (var game in games)
        {
            var iconPath = Path.Combine(game.SteamPath.ToString(), "appcache", "librarycache", game.AppId.Value.ToString() + "_icon.jpg");
            TryAddGame(game.Path.ToString(), game.Name, GameSource.Steam, iconPath, out _);
        }
    }

    public static void SaveManualGameList()
    {
        Config.SaveGameList(Games.Where(x => x.GameSource == GameSource.Manual).Select(x => x.Path));
    }

    private static void AddGameSorted(GameModel game)
    {
        var gameHasMl = game.MLVersion != null;
        for (var i = 0; i < Games.Count; i++)
        {
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

    public static GameModel? TryAddGame(string path, string? customName, GameSource gameSource, string? iconPath, [NotNullWhen(false)] out string? errorMessage)
    {
        if (File.Exists(path))
            path = Path.GetDirectoryName(path)!;
        else if (!Directory.Exists(path))
        {
            errorMessage = "The selected directory does not exist.";
            return null;
        }

        path = Path.GetFullPath(path);

        var dataDirs = Directory.GetDirectories(path, "*_Data").Where(x => File.Exists(x[..^5] + ".exe"));
        if (!dataDirs.Any())
        {
            errorMessage = "The selected directory does not contain a Unity game.";
            return null;
        }
        if (dataDirs.Count() > 1)
        {
            errorMessage = "The selected directory contains multiple Unity games?";
            return null;
        }

        var exe = dataDirs.First()[..^5] + ".exe";

        if (Games.Any(x => x.Path.Equals(exe, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "Game is already listed.";
            return null;
        }

        bool is64;
        try
        {
            using var pe = new PEReader(File.OpenRead(exe));
            is64 = pe.PEHeaders.CoffHeader.Machine == Machine.Amd64;
        }
        catch
        {
            errorMessage = "The game executable is possibly corrupted.";
            return null;
        }

        var mlVersion = GetMelonLoaderVersion(path);

        Bitmap? icon = null;

        if (iconPath != null && File.Exists(iconPath))
        {
            try
            {
                icon = new Bitmap(iconPath);
            }
            catch { }
        }

        icon ??= IconExtractor.GetExeIcon(exe);

        var result = new GameModel(exe, customName ?? Path.GetFileNameWithoutExtension(exe), !is64, gameSource, icon, mlVersion);
        errorMessage = null;

        AddGameSorted(result);

        return result;
    }

    public static Version? GetMelonLoaderVersion(string gameDir)
    {
        var mlDir = Path.Combine(gameDir, "MelonLoader");
        if (Directory.Exists(mlDir))
        {
            var mlAssemblyPath = Path.Combine(mlDir, "MelonLoader.dll");
            if (!File.Exists(mlAssemblyPath))
            {
                mlAssemblyPath = Path.Combine(mlDir, "MelonLoader.ModHandler.dll");
                if (!File.Exists(mlAssemblyPath))
                {
                    mlAssemblyPath = Path.Combine(mlDir, "net35", "MelonLoader.dll");
                    if (!File.Exists(mlAssemblyPath))
                        mlAssemblyPath = null;
                }
            }
            if (mlAssemblyPath != null)
            {
                try
                {
                    return Version.Parse(System.Diagnostics.FileVersionInfo.GetVersionInfo(mlAssemblyPath).FileVersion!);
                }
                catch { }
            }
        }

        return null;
    }
}