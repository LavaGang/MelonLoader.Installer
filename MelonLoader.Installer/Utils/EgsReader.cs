using Microsoft.Win32;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer.Utils;

public static class EgsReader
{
    private static readonly string? manifestDir;

    static EgsReader()
    {
        manifestDir = Registry.CurrentUser.OpenSubKey(@"Software\Epic Games\EOS")?.GetValue("ModSdkMetadataDir") as string;
        if (manifestDir != null && !Directory.Exists(manifestDir))
            manifestDir = null;
    }

    public static List<EgsGame>? GetGames()
    {
        if (manifestDir == null)
            return null;

        var games = new List<EgsGame>();
        foreach (var item in Directory.EnumerateFiles(manifestDir, "*.item"))
        {
            var json = JsonNode.Parse(File.ReadAllText(item));
            if (json == null || (bool?)json["bIsExecutable"] != true)
                continue;

            var dir = (string?)json["InstallLocation"];
            if (dir == null || !Directory.Exists(dir))
                continue;

            var name = (string?)json["DisplayName"];
            if (name == null)
                continue;

            games.Add(new()
            {
                Directory = dir,
                Name = name
            });
        }

        return games;
    }
}
