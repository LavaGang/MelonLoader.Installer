#if WINDOWS
using Microsoft.Win32;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer.GameLaunchers;

#pragma warning disable CA1416

public class EgsLauncher : GameLauncher
{
    private static readonly string? manifestDir;

    static EgsLauncher()
    {
        manifestDir = Registry.CurrentUser.OpenSubKey(@"Software\Epic Games\EOS")?.GetValue("ModSdkMetadataDir") as string;
        if (manifestDir != null && !Directory.Exists(manifestDir))
            manifestDir = null;
    }

    internal EgsLauncher() : base("/Assets/egs.png") { }

    public override void GetAddGameTasks(List<Task> tasks)
    {
        if (manifestDir == null)
            return;

        foreach (var file in Directory.GetFiles(manifestDir, "*.item"))
        {
            tasks.Add(AddGameAsync(file));
        }
    }

    private async Task AddGameAsync(string item)
    {
        JsonNode? json;
        try
        {
            using var str = File.OpenRead(item);
            json = await JsonNode.ParseAsync(str);
        }
        catch
        {
            return;
        }

        if (json == null || (bool?)json["bIsExecutable"] != true)
            return;

        var dir = (string?)json["InstallLocation"];
        if (dir == null || !Directory.Exists(dir))
            return;

        var name = (string?)json["DisplayName"];
        if (name == null)
            return;

        GameManager.TryAddGame(dir, name, this, null, out _);
    }
}
#endif