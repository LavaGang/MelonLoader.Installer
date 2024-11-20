using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer;

public static partial class Updater
{
    public static volatile UpdateState State;
    public static event InstallProgressEventHandler? Progress;

    public static async Task<Task?> UpdateIfPossible()
    {
        if (State != UpdateState.None)
            return null;
        
        // Don't auto-update on CI builds
        if (Program.Version.Revision > 0)
        {
            State = UpdateState.AlreadyChecked;
            return null;
        }

        var downloadUrl = await CheckForUpdateAsync();
        if (downloadUrl == null)
        {
            State = UpdateState.AlreadyChecked;
            return null;
        }

        State = UpdateState.Updating;

        return Task.Run(() => UpdateAsync(downloadUrl));
    }

    public static bool WaitAndRemoveApp(string originalPath, int prevPID)
    {
        if (!File.Exists(originalPath))
            return true;

        // Make sure we're not waiting for ourselves.
        if (prevPID == Environment.ProcessId || Path.GetFullPath(originalPath).Equals(Path.GetFullPath(Environment.ProcessPath!), StringComparison.OrdinalIgnoreCase))
            return false;

        if (prevPID != 0)
        {
            try
            {
                var prevProc = Process.GetProcessById(prevPID);

                if (!prevProc.HasExited)
                    prevProc.WaitForExit();
            }
            catch { }
        }

        try
        {
            File.Delete(originalPath);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static void HandleUpdate(string originalPath, int prevPID)
    {
        if (!WaitAndRemoveApp(originalPath, prevPID))
            return;

        File.Copy(Environment.ProcessPath!, originalPath, true);

        Process.Start(originalPath, ["-cleanup", Environment.ProcessPath!, Environment.ProcessId.ToString()]);
    }

    private static async Task UpdateAsync(string downloadUrl)
    {
        var newPath = Path.GetTempFileName()
#if WINDOWS
                      + ".exe"
#endif
                      ;

        await using (var newStr = File.OpenWrite(newPath))
        {
            var result = await InstallerUtils.DownloadFileAsync(downloadUrl, newStr, (progress, newStatus) => Progress?.Invoke(progress, newStatus));
            if (result != null)
            {
                throw new Exception("Failed to download the latest installer version: " + result);
            }
        }
        
#if LINUX
        // Make the file executable on Unix
        Chmod(newPath, S_IRUSR | S_IXUSR | S_IWUSR | S_IRGRP | S_IXGRP | S_IROTH | S_IXOTH);
#endif

        Process.Start(newPath, ["-handleupdate", Environment.ProcessPath!, Environment.ProcessId.ToString()]);

        State = UpdateState.Done;
    }

#if WINDOWS
    public static bool CheckLegacyUpdate()
    {
        if (!Environment.ProcessPath!.EndsWith(".tmp.exe", StringComparison.OrdinalIgnoreCase))
            return false;

        var dir = Path.GetDirectoryName(Environment.ProcessPath)!;
        var name = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        name = name.Remove(name.Length - 4) + ".exe";

        var final = Path.Combine(dir, name);

        var prevProc = Process.GetProcessesByName(name);

        HandleUpdate(final, prevProc.FirstOrDefault()?.Id ?? 0);
        return true;
    }
#endif

    private static async Task<string?> CheckForUpdateAsync()
    {
        HttpResponseMessage response;
        try
        {
            response = await InstallerUtils.Http.GetAsync(Config.InstallerLatestReleaseApi);
        }
        catch
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var respStr = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(respStr)!;

        if (json["tag_name"] == null)
            return null;

        var versionStr = json["tag_name"]!.ToString();
        if (versionStr.StartsWith('v'))
            versionStr = versionStr[1..];

        if (!Version.TryParse(versionStr, out var latestVer))
            return null;

        var currentVer = typeof(Updater).Assembly.GetName().Version;
        if (currentVer == null || currentVer >= latestVer)
            return null;

        var asset = json["assets"]?.AsArray().FirstOrDefault(x => x!["name"]!.ToString().EndsWith(
#if WINDOWS
            ".exe"
#else
            ".Linux"
#endif
            ));
        
        return asset?["browser_download_url"]?.ToString();
    }
    
#if LINUX
    // user permissions
    const int S_IRUSR = 0x100;
    const int S_IWUSR = 0x80;
    const int S_IXUSR = 0x40;

    // group permission
    const int S_IRGRP = 0x20;
    const int S_IXGRP = 0x8;

    // other permissions
    const int S_IROTH = 0x4;
    const int S_IXOTH = 0x1;
        
    [LibraryImport("libc", EntryPoint = "chmod", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Chmod(string pathname, int mode);
#endif

    public enum UpdateState
    {
        None,
        Updating,
        Done,
        AlreadyChecked
    }
}
