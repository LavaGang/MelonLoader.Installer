#if WINDOWS
using Microsoft.Win32;

namespace MelonLoader.Installer.GameLaunchers;

public class GogLauncher : GameLauncher
{
    private static readonly RegistryKey? gamesKey;

    internal GogLauncher() : base("/Assets/gog.png") { }

    static GogLauncher()
    {
        gamesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games");
    }

    public override void GetAddGameTasks(List<Task> tasks)
    {
        if (gamesKey == null)
            return;

        var subKeyNames = gamesKey.GetSubKeyNames();
        foreach (var subKeyName in subKeyNames)
        {
            var game = gamesKey.OpenSubKey(subKeyName)!;

            var path = (string?)game.GetValue("path");
            if (path == null || !Directory.Exists(path))
                continue;

            var name = (string?)game.GetValue("gameName");
            if (name == null)
                continue;

            tasks.Add(Task.Run(() => GameManager.TryAddGame(path, name, this, null, out _)));
        }
    }
}
#endif