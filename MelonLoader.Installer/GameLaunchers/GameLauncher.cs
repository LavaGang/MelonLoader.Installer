namespace MelonLoader.Installer.GameLaunchers;

public abstract class GameLauncher(string iconPath)
{
    public static GameLauncher[] Launchers { get; private set; } =
    [
        new EgsLauncher(),
        new SteamLauncher(),
        new GogLauncher()
    ];

    public string IconPath => iconPath;

    public abstract void AddGames();
}
