namespace MelonLoader.Installer;

public static class LaunchInstructions
{
    // Keep paths literal even when a game or library name contains shell syntax.
    private static string Quote(string path) => "'" + path.Replace("'", "'\"'\"'") + "'";

    private static string MacOSWrapper(string appPath) =>
        Quote(Path.Combine(Path.GetDirectoryName(appPath)!, "melonloader-launch.sh"));

    public static string MacOSManual(string appPath) =>
        $"{MacOSWrapper(appPath)} {Quote(appPath)}";

    public static string MacOSSteam(string appPath) =>
        // Steam parses launch options itself; preserve its existing double-quoted format.
        $"\"{Path.Combine(Path.GetDirectoryName(appPath)!, "melonloader-launch.sh")}\" %command%";

    public static string MacOSCompletion(bool isMacOSGame, bool installed) => !isMacOSGame
        ? string.Empty
        : installed
            ? "\n\nNext, open ‘Finish setup: launch instructions’ to configure Steam or launch manually."
            : "\n\nIf you added the MelonLoader command in Steam → Properties → Launch Options, remove that command now. Keep any other launch options you use.";
}
