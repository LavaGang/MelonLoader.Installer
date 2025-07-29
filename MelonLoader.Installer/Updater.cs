using System.Diagnostics;
using System.Text.Json.Nodes;

#if LINUX
using System.Runtime.InteropServices;
#endif

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

    public static bool WaitAndRemoveApp(string originalPath, int prevPID
#if OSX
        , bool removeParentDirectory = false
#endif
        )
    {
        if (!File.Exists(originalPath))
            return true;

        // Make sure we're not waiting for ourselves.
        if (prevPID == Environment.ProcessId || Path.GetFullPath(originalPath).Equals(Path.GetFullPath(Config.ProcessPath!), StringComparison.OrdinalIgnoreCase))
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

#if OSX
        
        try
        {
            Directory.Delete(originalPath);
            if (removeParentDirectory)
            {
                string parentPath = Path.GetDirectoryName(originalPath);
                Directory.Delete(parentPath);
            }
        }
        catch
        {
            return false;
        }

#else

        try
        {
            File.Delete(originalPath);
        }
        catch
        {
            return false;
        }

#endif

        return true;
    }

    public static void HandleUpdate(string originalPath, int prevPID)
    {
        if (!WaitAndRemoveApp(originalPath, prevPID))
            return;

        File.Copy(Config.ProcessPath!, originalPath, true);

#if OSX
        Process.Start("open", [originalPath, "--args", "-cleanup", Config.ProcessPath!, Environment.ProcessId.ToString()]);
#else
        Process.Start(originalPath, ["-cleanup", Config.ProcessPath!, Environment.ProcessId.ToString()]);
#endif
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

        // Make the file executable
        InstallerUtils.Chmod(newPath, InstallerUtils.S_IRUSR 
            | InstallerUtils.S_IXUSR
            | InstallerUtils.S_IWUSR 
            | InstallerUtils.S_IRGRP 
            | InstallerUtils.S_IXGRP 
            | InstallerUtils.S_IROTH
            | InstallerUtils.S_IXOTH);

#elif OSX

        // Extract Zip
        var archivePath = newPath;
        
        string tempFolderPath = Path.Combine(Config.CacheDir, "tmp");
        if (Directory.Exists(tempFolderPath))
            Directory.Delete(tempFolderPath, true);
        Directory.CreateDirectory(tempFolderPath);

        using var zipStr = File.OpenRead(archivePath);
        if (InstallerUtils.Extract(zipStr, tempFolderPath, null) == null)
        {
            if (File.Exists(archivePath))
                File.Delete(archivePath);
            if (Directory.Exists(tempFolderPath))
                Directory.Delete(tempFolderPath);
            throw new Exception("Failed to extract the latest installer version");
        }
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        // Find New File
        newPath = Path.Combine(tempFolderPath, "MelonLoader.Installer.MacOS.app");
        if (!File.Exists(newPath))
        {
            if (Directory.Exists(tempFolderPath))
                Directory.Delete(tempFolderPath);
            throw new Exception("Failed to extract the latest installer version");
        }

        // Make the file executable
        InstallerUtils.Chmod(newPath, InstallerUtils.S_IRUSR 
            | InstallerUtils.S_IXUSR
            | InstallerUtils.S_IWUSR 
            | InstallerUtils.S_IRGRP 
            | InstallerUtils.S_IXGRP 
            | InstallerUtils.S_IROTH
            | InstallerUtils.S_IXOTH);
#endif

#if OSX
        Process.Start("open", [newPath, "--args", "-handleupdate", Config.ProcessPath!, Environment.ProcessId.ToString()]);
#else
        Process.Start(newPath, ["-handleupdate", Config.ProcessPath!, Environment.ProcessId.ToString()]);
#endif

        State = UpdateState.Done;
    }

#if WINDOWS
    public static bool CheckLegacyUpdate()
    {
        if (!Config.ProcessPath!.EndsWith(".tmp.exe", StringComparison.OrdinalIgnoreCase))
            return false;

        var dir = Path.GetDirectoryName(Config.ProcessPath)!;
        var name = Path.GetFileNameWithoutExtension(Config.ProcessPath);
        name = name[..^4] + ".exe";

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

        var asset = json["assets"]?.AsArray().FirstOrDefault((x) =>
        {
            string fileName = x!["name"]!.ToString();
            return fileName.EndsWith(
#if WINDOWS
                ".exe"
#elif LINUX
                ".Linux"
#elif OSX_X64
                ".MacOS.x64.zip"
#endif
            );
        });

        return asset?["browser_download_url"]?.ToString();
    }

    public enum UpdateState
    {
        None,
        Updating,
        Done,
        AlreadyChecked
    }
}
