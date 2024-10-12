using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace MelonLoader.Installer.Utils;

public static class SteamReader
{
    public static string? SteamPath { get; private set; }

    static SteamReader()
    {
        var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
        key ??= Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");

        SteamPath = (string?)key?.GetValue("InstallPath");
        if (SteamPath != null && !Directory.Exists(SteamPath))
            SteamPath = null;
    }

    public static List<SteamGame>? GetGames()
    {
        if (SteamPath == null)
            return null;

        var libPath = Path.Combine(SteamPath, "config", "libraryfolders.vdf");
        if (!File.Exists(libPath))
            return null;

        var libraryFolders = VdfConvert.Deserialize(File.ReadAllText(libPath));
        var libDirs = libraryFolders.Value.Select(x => ((VProperty)((VProperty)x).Value.First(y => ((VProperty)y).Key == "path")).Value.ToString()); // Lord forgive me for this one-liner

        var games = new List<SteamGame>();

        foreach (var library in libDirs)
        {
            var steamapps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamapps))
                continue;

            var acfs = Directory.EnumerateFiles(steamapps, "*.acf");
            foreach (var acfPath in acfs)
            {
                VToken acf;
                try
                {
                    acf = VdfConvert.Deserialize(File.ReadAllText(acfPath)).Value;
                }
                catch
                {
                    continue;
                }

                var id = ((VProperty?)acf.FirstOrDefault(x => ((VProperty)x).Key == "appid"))?.Value?.ToString();
                var name = ((VProperty?)acf.FirstOrDefault(x => ((VProperty)x).Key == "name"))?.Value?.ToString();
                var dirName = ((VProperty?)acf.FirstOrDefault(x => ((VProperty)x).Key == "installdir"))?.Value?.ToString();

                if (id == null || name == null || dirName == null)
                    continue;

                var appDir = Path.Combine(steamapps, "common", dirName);
                if (!Directory.Exists(appDir))
                    continue;

                games.Add(new()
                {
                    AppId = id,
                    Name = name,
                    Directory = appDir
                });
            }
        }

        return games;
    }
}
