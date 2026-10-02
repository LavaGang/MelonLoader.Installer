using System.Diagnostics;
using MelonLoader.Installer;
using MelonLoader.Installer.ViewModels;
using Semver;
using System.Xml.Linq;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
    passed++;
}
MLVersion Version() => new() { Version = SemVersion.Parse("0.7.3") };

foreach (var extension in new[] { "", ".zip" })
{
    var intel = Version();
    intel.ApplyURLDownload("MelonLoader.macOS.x64" + extension, "intel");
    Check(intel.GetDownload(Architecture.MacOSX64) == "intel" &&
        intel.GetDownload(Architecture.MacOSArm64) == null, $"Intel-only {extension} does not offer ARM64");
    var arm = Version();
    arm.ApplyURLDownload("MelonLoader.macOS.arm64" + extension, "arm");
    Check(arm.GetDownload(Architecture.MacOSArm64) == "arm" &&
        arm.GetDownload(Architecture.MacOSX64) == null, $"ARM-only {extension} does not offer Intel");
    foreach (var reverse in new[] { false, true })
    {
        var both = Version();
        var assets = new[] { ("x64", "intel"), ("arm64", "arm") };
        foreach (var (arch, url) in reverse ? assets.Reverse() : assets)
            both.ApplyURLDownload($"MelonLoader.macOS.{arch}{extension}", url);
        Check(both.GetDownload(Architecture.MacOSX64) == "intel" &&
            both.GetDownload(Architecture.MacOSArm64) == "arm", $"Both architectures {extension}, reverse={reverse}");
    }
    var legacy = Version();
    legacy.ApplyURLDownload("MelonLoader.macOS" + extension, "legacy");
    Check(legacy.GetDownload(Architecture.MacOSX64) == "legacy" &&
        legacy.GetDownload(Architecture.MacOSArm64) == null, $"Legacy {extension} is Intel-only");
    legacy.ApplyURLDownload("MelonLoader.macOS.x64" + extension, "explicit");
    legacy.ApplyURLDownload("MelonLoader.macOS" + extension, "legacy");
    Check(legacy.GetDownload(Architecture.MacOSX64) == "explicit", $"Explicit Intel overrides legacy in either order {extension}");
}
var unsupported = Version();
foreach (var name in new[] { "MelonLoader.macOS.arm32.zip", "MelonLoader.macOS.x64.zip.sha256", "MelonLoader.macOS.x64-debug.zip" })
    unsupported.ApplyURLDownload(name, "unsupported");
Check(unsupported.IsDownloadEmpty(), "Unsupported macOS assets are ignored");
unsupported.ApplyURLDownload("MELONLOADER.MACOS.ARM64.ZIP", "arm");
Check(unsupported.GetDownload(Architecture.MacOSArm64) == "arm", "Asset matching is case-insensitive");

var game = new GameModel("/tmp/Game.app", "Game", Architecture.MacOSArm64, null, null, null, null, false);
var model = new DetailsViewModel(game);
Check(!model.CanInstall && !model.HasCompatibleVersion, "Empty selection disables Install");
var intelVersion = Version();
intelVersion.ApplyURLDownload("MelonLoader.macOS.x64.zip", "intel");
model.SelectedVersion = intelVersion;
Check(!model.CanInstall, "Incompatible selected version disables Install");
var armVersion = Version();
armVersion.ApplyURLDownload("MelonLoader.macOS.arm64.zip", "arm");
var changes = new List<string?>();
model.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
model.SelectedVersion = armVersion;
Check(model.CanInstall && changes.Contains(nameof(model.CanInstall)) && changes.Contains(nameof(model.HasCompatibleVersion)), "Compatible selection enables Install and notifies bindings");
model.Installing = true;
Check(!model.CanInstall, "Installation in progress disables Install");
model.Installing = false;
var localVersion = new MLVersion { Version = SemVersion.Parse("0.7.3"), IsLocalPath = true };
localVersion.ApplyLocalPathDownload(Architecture.MacOSArm64, "/tmp/extracted-local-build");
model.SelectedVersion = localVersion;
Check(model.CanInstall && model.HasCompatibleVersion, "Compatible local ZIP selection remains installable");
model.Offline = true;
Check(!model.CanInstall, "Offline state disables Install");
Check(model.NoCompatibleVersionMessage.Contains("connection") && !model.NoCompatibleVersionMessage.Contains("ZIP"), "Offline empty-state copy does not suggest disabled controls");
model.Offline = false;
model.SelectedVersion = null;
Check(!model.CanInstall, "Clearing selection disables Install again");
var installedChanges = new List<string?>();
game.PropertyChanged += (_, e) => installedChanges.Add(e.PropertyName);
game.MLVersion = SemVersion.Parse("0.7.3");
Check(game.MLInstalled && installedChanges.Contains(nameof(game.MLInstalled)), "First install notifies launch-help visibility binding");
installedChanges.Clear();
game.MLVersion = null;
Check(!game.MLInstalled && installedChanges.Contains(nameof(game.MLInstalled)), "Uninstall notifies launch-help visibility binding");

// Check that the UI consumes the tested production state, rather than a stale copied flag.
var view = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "DetailsView.axaml"));
var macHelp = view.Descendants().Single(x => (string?)x.Attribute("Name") == "MacOSLaunchHelp");
Check(macHelp.Descendants().Any(x => (string?)x.Attribute("IsVisible") == "{Binding Game.MLInstalled}"), "Mac help binds directly to installed state");
var installButton = view.Descendants().Single(x => (string?)x.Attribute("Name") == "InstallButton");
Check((string?)installButton.Attribute("IsEnabled") == "{Binding CanInstall}", "Install button binds to valid selection state");
Check(LaunchInstructions.MacOSCompletion(true, false).Contains("remove that command") &&
      LaunchInstructions.MacOSCompletion(true, false).Contains("Keep any other launch options"), "Uninstall guidance preserves unrelated Steam options");
Check(LaunchInstructions.MacOSCompletion(true, true).Contains("Finish setup") &&
      LaunchInstructions.MacOSCompletion(false, false) == "", "Setup guidance only applies to Mac games");

if (!OperatingSystem.IsWindows())
{
    var root = Path.Combine(Path.GetTempPath(), "melonloader-command-test-" + Guid.NewGuid());
    var dir = Path.Combine(root, "Game's $dollars `ticks` (spaces) & library");
    Directory.CreateDirectory(dir);
    try
    {
        var app = Path.Combine(dir, "Game's $name.app");
        var wrapper = Path.Combine(dir, "melonloader-launch.sh");
        File.WriteAllText(wrapper, "#!/bin/sh\nprintf '%s\\n' \"$#\" \"$1\"\n");
        File.SetUnixFileMode(wrapper, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var start = new ProcessStartInfo("/bin/bash") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(LaunchInstructions.MacOSManual(app));
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode == 0 && output == "1\n" + app + "\n", "Manual command passes the literal app path as one argument, including shell metacharacters");
        Check(LaunchInstructions.MacOSSteam("/tmp/Game Library/Game.app") == "\"/tmp/Game Library/melonloader-launch.sh\" %command%", "Steam command preserves double-quoted path and command placeholder");
    }
    finally { Directory.Delete(root, true); }
}
Console.WriteLine($"{passed} regression checks passed.");
