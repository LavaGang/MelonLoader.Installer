namespace MelonLoader.Installer.GameLaunchers;

public abstract class GameLauncher(string iconPath)
{
    public static GameLauncher[] Launchers { get; private set; } =
    [
        new SteamLauncher(),
#if WINDOWS
        new EgsLauncher(),
        new GogLauncher(),
        new MetaLauncher()
#endif
    ];

    public string IconPath => iconPath;

    public abstract void AddGames();
}
