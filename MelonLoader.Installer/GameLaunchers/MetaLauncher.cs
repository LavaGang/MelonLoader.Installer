#if WINDOWS
using Avalonia.Controls;
using Microsoft.Win32;
using System.IO;

namespace MelonLoader.Installer.GameLaunchers;

#pragma warning disable CA1416

public class MetaLauncher : GameLauncher
{
    private static readonly RegistryKey? gamesKey;

    internal MetaLauncher() : base("/Assets/meta.png") { }

    static MetaLauncher()
    {
        gamesKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Oculus VR, LLC\Oculus\Libraries");
    }

    public override void AddGames()
    {
        if (gamesKey == null)
            return;

        var subKeyNames = gamesKey.GetSubKeyNames();
        foreach (var subKeyName in subKeyNames)
        {
            var game = gamesKey.OpenSubKey(subKeyName);
            var originalPath = (string?)game?.GetValue("OriginalPath");

            if (string.IsNullOrEmpty(originalPath) || !Directory.Exists(originalPath))
                continue;

            if (!originalPath.EndsWith(@"\"))
                originalPath += @"\";

            var softwareDirectory = Path.Combine(originalPath, "Software");

            if (!Directory.Exists(softwareDirectory))
                continue;

            var exeFiles = Directory.GetFiles(softwareDirectory, "*.exe", SearchOption.AllDirectories);
            if (exeFiles.Length == 0)
                continue;

            foreach (var exe in exeFiles)
            {
                var gameName = Path.GetFileNameWithoutExtension(exe);
                GameManager.TryAddGame(exe, gameName, this, null, out _);
            }
        }
    }
}
#endif
