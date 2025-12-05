using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace MelonLoader.Installer.GameLaunchers;

#pragma warning disable CA1416

public class SteamLauncher : GameLauncher
{
    private static readonly string? steamPath;

    static SteamLauncher()
    {
#if WINDOWS
        var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
        key ??= Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
        steamPath = (string?)key?.GetValue("InstallPath");
#elif LINUX
        steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam");
#elif OSX
        steamPath = Path.Combine(Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal)), "Library", "Application Support", "Steam");

        if ((steamPath != null) && !Directory.Exists(steamPath))

            steamPath = "/Applications/Steam.app";
#endif

        if ((steamPath != null) && !Directory.Exists(steamPath))
            steamPath = null;
    }

    internal SteamLauncher() : base("/Assets/steam.png") { }

    public override void AddGames()
    {
        if (steamPath == null)
            return;

        var libPath = Path.Combine(steamPath, "config", "libraryfolders.vdf");
        if (!File.Exists(libPath))
            return;

        VToken vdfRoot;
        try
        {
            vdfRoot = VdfConvert.Deserialize(File.ReadAllText(libPath)).Value;
        }
        catch
        {
            return;
        }

        var libDirs = new List<string>();

        foreach (var child in vdfRoot.Children())
        {
            if (child is not VProperty prop)
                continue;

            // only process numeric keys (such as "0", "1", "2", ...)
            if (!int.TryParse(prop.Key, out _))
                continue;

            if (prop.Value is not VObject folderObj)
                continue;

            var pathProp = folderObj.Properties().FirstOrDefault(p => p.Key == "path");
            if (pathProp?.Value == null)
                continue;

            var rawPath = pathProp.Value.ToString();
            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            // normalize windows style escaped paths
            var cleanPath = rawPath.Replace(@"\\", @"\");

            if (Directory.Exists(cleanPath))
                libDirs.Add(cleanPath);
        }

        foreach (var library in libDirs)
        {
            var steamapps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamapps))
                continue;

            IEnumerable<string> acfs;
            try
            {
                acfs = Directory.EnumerateFiles(steamapps, "*.acf");
            }
            catch
            {
                continue;
            }

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

                var iconPath = Path.Combine(steamPath, "appcache", "librarycache", id);
                iconPath = Directory.Exists(iconPath)
                    ? Directory.EnumerateFiles(iconPath, "*.jpg").FirstOrDefault(x =>
                    {
                        var f = Path.GetFileName(x);
                        return !f.StartsWith("library") && !f.StartsWith("header") && !f.StartsWith("logo");
                    })
                    : null;

                GameManager.TryAddGame(appDir, name, this, iconPath, out _);
            }
        }
    }
}
