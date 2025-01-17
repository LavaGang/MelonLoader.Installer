using System.Diagnostics;
using System.Text.Json.Nodes;

#if LINUX
using System.Runtime.InteropServices;
#endif

namespace MelonLoader.Installer;

public static partial class Updater
{
    public static async Task<bool> UpdateIfPossibleAsync(InstallProgressEventHandler onProgress)
    {
        // Don't auto-update on CI builds
        if (Program.Version.Revision > 0)
            return false;

        onProgress?.Invoke(0, "Checking for updates");

        var downloadUrl = await CheckForUpdateAsync();
        if (downloadUrl == null)
            return false;

        var newPath = Path.GetTempFileName()
#if WINDOWS
                      + ".exe"
#endif
                      ;

        onProgress?.Invoke(0, "Updating");

        await using (var newStr = File.OpenWrite(newPath))
        {
            var result = await InstallerUtils.DownloadFileAsync(downloadUrl, newStr, (progress, newStatus) => onProgress?.Invoke(progress, newStatus));
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

        return true;
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
}
