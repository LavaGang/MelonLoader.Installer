using Semver;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

#if WINDOWS
using System.Diagnostics;
#endif

namespace MelonLoader.Installer;

internal static class MLManager
{
    private static bool inited;
    
    internal static readonly string[] proxyNames =
    [
        "version.dll",
        "winmm.dll",
        "winhttp.dll",
        "MelonBootstrap.so",
        "MelonLoader.Bootstrap.so",
        "version.so",
        "winmm.so",
        "winhttp.so",
        "libversion.so",
        "libwinmm.so",
        "libwinhttp.so",
        "MelonBootstrap.dylib",
        "MelonLoader.Bootstrap.dylib",
    ];

    private static readonly string[] userDirectories =
    [
        "UserData",
        "UserLibs",
        "Plugins",
        "Mods"
    ];

    private static readonly string[] extraFiles =
    [
        "NOTICE.txt",
        "README.txt",
        "dobby.dll",
        "dobby.so",
        "libdobby.so",
        "dobby.dylib",
        "libdobby.dylib",
        "melonloader-launch.sh"
    ];

    private static MLVersion? localBuild;
    public static List<MLVersion> Versions = [];

    static MLManager()
    {
        Program.Exiting += HandleExit;
    }

    private static void HandleExit()
    {
        if (Directory.Exists(PathManager.LocalZipCache))
        {
            try
            {
                Directory.Delete(PathManager.LocalZipCache, true);
            }
            catch { }
        }
    }
    
    public static async Task<string?> Init()
    {
        if (inited)
            return string.Empty;
        
        string? err = await RefreshVersions();
        inited = string.IsNullOrEmpty(err);
        return err;
    }

    private static async Task<string?> RefreshVersions()
    {
        Versions.Clear();

        if (localBuild != null)
            Versions.Add(localBuild);
        
        string? fetchNightlyError = await GetNightlyVersionsAsync(Versions);
        if (!string.IsNullOrEmpty(fetchNightlyError))
            return fetchNightlyError;
        
        string? fetchReleasesError = await GetReleasedVersionsAsync(Versions);
        if (!string.IsNullOrEmpty(fetchReleasesError))
            return fetchReleasesError;
        
        if (Versions.Count <= 0)
            return "No Versions Found";
        
        return string.Empty;
    }

    private static async Task<string?> GetNightlyVersionsAsync(List<MLVersion> versions)
    {
        var runsJson = await GitHubApi.GetWorkflowRuns();
        if (runsJson.Node == null)
            return runsJson.Error;
        
        foreach (var run in runsJson.Node.AsArray())
        {
            var runId = run!["id"]!.ToString();
            
            var runName = run!["name"]!.ToString();
            if (runName.ToLower().StartsWith("v"))
                runName = runName[1..];
            var runVerEnd = runName.IndexOf(' ');
            if (runVerEnd != -1)
                runName = runName[..runVerEnd];

            if (!SemVersion.TryParse(runName, SemVersionStyles.Any, out var runVersion))
                continue;
            
            if (versions.FirstOrDefault(x => x.Version.CompareSortOrderTo(runVersion) == 0) != null)
                continue;
            
            var version = new MLVersion { Version = runVersion };
            var artifacts = await GitHubApi.GetWorkflowRunArtifacts(runId);
            if (artifacts.Node == null)
                continue;
            
            foreach (var art in artifacts.Node.AsArray())
            {
                string fileName = art!["name"]!.ToString();
                string fixedNightlyDownload =
                    $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{runId}/{fileName}.zip";
                version.ApplyURLDownload(fileName, fixedNightlyDownload);
            }
            
            if (version.IsDownloadEmpty())
                continue;

            versions.Add(version);
        }

        return string.Empty;
    }
    
    private static async Task<string?> GetReleasedVersionsAsync(List<MLVersion> versions)
    {
        var releasesJson = await GitHubApi.GetReleases();
        if (releasesJson.Node == null)
            return releasesJson.Error;
        
        foreach (var release in releasesJson.Node.AsArray())
        {
            var releaseName = release!["tag_name"]!.ToString();
            if (releaseName.ToLower().StartsWith("v"))
                releaseName = releaseName[1..];
            var runVerEnd = releaseName.IndexOf(' ');
            if (runVerEnd != -1)
                releaseName = releaseName[..runVerEnd];
            
            if (!SemVersion.TryParse(releaseName, SemVersionStyles.Any, out var relVersion))
                continue;
            
            if (versions.FirstOrDefault(x => x.Version.CompareSortOrderTo(relVersion) == 0) != null)
                continue;

            if (relVersion.Major == 0 && relVersion.Minor <= 2)
                continue;

            var releaseAssets = release!["assets"]!.AsArray();
            if (releaseAssets.Count <= 0)
                continue;
            
            var version = new MLVersion { Version = relVersion };
            foreach (var asset in releaseAssets)
            {
                string fileName = asset!["name"]!.ToString();
                string fixedDownload = asset!["browser_download_url"]!.ToString();
                version.ApplyURLDownload(fileName, fixedDownload);
            }
            
            if (version.IsDownloadEmpty())
                continue;

            versions.Add(version);
        }
        
        return string.Empty;
    }

    public static string? Uninstall(string gameDir, bool removeUserFiles)
    {
        if (!Directory.Exists(gameDir))
        {
            return "The provided directory does not exist.";
        }
        
        List<string> filesToDelete = new();
        List<string> dirsToDelete = new();
        dirsToDelete.Add(Path.Combine(gameDir, "MelonLoader"));

        foreach (var proxy in proxyNames)
        {
            filesToDelete.Add(Path.Combine(gameDir, $"{proxy}.dbg"));
            
            var proxyPath = Path.Combine(gameDir, proxy);
            if (!File.Exists(proxyPath))
                continue;

#if WINDOWS
            try
            {
                var versionInf = FileVersionInfo.GetVersionInfo(proxyPath);
                if (versionInf.LegalCopyright != null && versionInf.LegalCopyright.Contains("Microsoft"))
                    continue;
            }
            catch {}
#endif
            
            filesToDelete.Add(proxyPath);
        }

        foreach (var extraFileName in extraFiles)
            filesToDelete.Add(Path.Combine(gameDir, extraFileName));

        if (removeUserFiles)
            foreach (var userFolderName in userDirectories)
                dirsToDelete.Add(Path.Combine(gameDir, userFolderName));

        foreach (var filePath in filesToDelete)
        {
            if (!File.Exists(filePath))
                continue;
            
            string fileName = Path.GetFileName(filePath);
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                return $"Failed to remove {fileName} file\nEnsure that the game is fully closed before trying again.";
            }
        }

        foreach (var dirPath in dirsToDelete)
        {
            if (!Directory.Exists(dirPath))
                continue;
            
            string dirName = new DirectoryInfo(dirPath).Name;
            try
            {
                Directory.Delete(dirPath, true);
            }
            catch
            {
                return $"Failed to remove {dirName} folder\nEnsure that the game is fully closed before trying again.";
            }
        }

        return null;
    }

    public static void SetLocalZip(string zipPath, InstallProgressEventHandler? onProgress, InstallFinishedEventHandler? onFinished)
    {
        if (!File.Exists(zipPath))
        {
            onFinished?.Invoke("The selected zip file does not exist.");
            return;
        }

        localBuild = null;
        if (Versions.Count > 0 && Versions[0].IsLocalPath)
            Versions.RemoveAt(0);

        if (Directory.Exists(PathManager.LocalZipCache))
        {
            try
            {
                Directory.Delete(PathManager.LocalZipCache, true);
            }
            catch
            {
                onFinished?.Invoke("Failed to remove the previously extracted zip data.");
                return;
            }
        }

        onProgress?.Invoke(0, "Extracting local zip archive");

        using var zipStr = File.OpenRead(zipPath);
        var extRes = InstallerUtils.Extract(zipStr, PathManager.LocalZipCache, onProgress);
        if (extRes != null)
        {
            onFinished?.Invoke(extRes);
            return;
        }

        var mlVer = MLVersion.GetMelonLoaderVersion(PathManager.LocalZipCache, out var arch, out _);
        if (mlVer == null)
        {
            onFinished?.Invoke("The selected zip archive does not contain a valid MelonLoader build.");
            return;
        }

        var version = new MLVersion()
        {
            Version = mlVer,
            IsLocalPath = true
        };
        version.ApplyLocalPathDownload(arch, PathManager.LocalZipCache);

        localBuild = version;
        Versions.Insert(0, version);

        onFinished?.Invoke(null);
    }

    public static async Task InstallAsync(string gameDir, bool removeUserFiles, MLVersion version, Architecture arch, InstallProgressEventHandler? onProgress, InstallFinishedEventHandler? onFinished)
    {
        var oldVersion = MLVersion.GetMelonLoaderVersion(gameDir, out _, out _);

        var downloadUrl = version.GetDownload(arch);

        if (downloadUrl == null)
        {
            onFinished?.Invoke($"The selected version does not support the selected architecture: {arch}");
            return;
        }

        onProgress?.Invoke(0, "Uninstalling previous versions");

        var unErr = Uninstall(gameDir, removeUserFiles);
        if (unErr != null)
        {
            onFinished?.Invoke(unErr);
            return;
        }

        if (version.IsLocalPath)
        {
            if (!Directory.Exists(downloadUrl))
            {
                onFinished?.Invoke("The selected local build was not found.");
                return;
            }

            onProgress?.Invoke(0, "Copying extracted files");

            foreach (var file in Directory.EnumerateFiles(downloadUrl, "*.*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(downloadUrl, file);
                var dest = Path.Combine(gameDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, true);
            }
        }
        else
        {
            var tasks = 2;
            var currentTask = 0;

            void SetProgress(double progress, string? newStatus = null)
            {
                onProgress?.Invoke(currentTask / (double)tasks + progress / tasks, newStatus);
            }

            SetProgress(0, "Downloading MelonLoader " + version);

            using var bufferStr = new MemoryStream();
            var result = await InstallerUtils.DownloadFileAsync(downloadUrl, bufferStr, SetProgress);
            if (result != null)
            {
                onFinished?.Invoke("Failed to download MelonLoader: " + result);
                return;
            }
            bufferStr.Seek(0, SeekOrigin.Begin);

            currentTask++;

            SetProgress(0, "Installing " + version);

            var extRes = InstallerUtils.Extract(bufferStr, gameDir, SetProgress);
            if (extRes != null)
            {
                onFinished?.Invoke(extRes);
                return;
            }
        }

        CreateNewDirectory(Path.Combine(gameDir, "UserData"));
        HandleMelonFolder(version, oldVersion, Path.Combine(gameDir, "UserLibs"));
        HandleMelonFolder(version, oldVersion, Path.Combine(gameDir, "Mods"));
        HandleMelonFolder(version, oldVersion, Path.Combine(gameDir, "Plugins"));
        
#if LINUX || OSX
        TrySetLaunchScriptExecutable(Path.Combine(gameDir, "melonloader-launch.sh"));
#endif

        onFinished?.Invoke(null);
    }
    
#if LINUX || OSX
    private static void TrySetLaunchScriptExecutable(string scriptPath)
    {
        if (!File.Exists(scriptPath))
            return;
        
#pragma warning disable CA1416
        try
        {
            File.SetUnixFileMode(
                scriptPath,
                File.GetUnixFileMode(scriptPath) | UnixFileMode.UserExecute
            );
        }
        catch
        {
        }
#pragma warning restore CA1416
    }
#endif

    private static bool CreateNewDirectory(string path)
    {
        if (Directory.Exists(path))
            return false;
        Directory.CreateDirectory(path);
        return true;
    }

    private static void HandleMelonFolder(MLVersion newVersion, SemVersion? oldVersion, string path)
    {
        // Create Folder
        if (CreateNewDirectory(path))
            return;

        // Handle Subfolders
        FixSubfolderFunctionality(newVersion.Version, oldVersion, path);
    }

    private static readonly SemVersion? subFolderUpdateVersion_070 = SemVersion.Parse("0.7.0");
    private static readonly SemVersion? subFolderUpdateVersion_071 = SemVersion.Parse("0.7.1");
    private static bool ShouldHandleSubfolderFunctionality(SemVersion? newVersion, SemVersion? oldVersion)
    {
        // Validate Version
        if ((newVersion == null)
            || (oldVersion == null))
            return false;

        // Check for Melon Subfolder Functionality
        var newVersionComp_070 = newVersion.ComparePrecedenceTo(subFolderUpdateVersion_070);
        if (newVersionComp_070 < 0) // newVersion < 0.7.0
            return false;
        var newVersionComp_071 = newVersion.ComparePrecedenceTo(subFolderUpdateVersion_071);
        if (newVersionComp_071 == 0) // newVersion == 0.7.1
            return true;
        var oldVersionComp_070 = oldVersion.ComparePrecedenceTo(subFolderUpdateVersion_070);
        if (oldVersionComp_070 >= 0) // oldVersion >= 0.7.0
            return false;

        // Return true
        return true;
    }
    private static void FixSubfolderFunctionality(SemVersion? newVersion, SemVersion? oldVersion, string path)
    {
        // Check for Subfolder Functionality
        if (!ShouldHandleSubfolderFunctionality(newVersion, oldVersion))
            return;

        // Get Subfolders
        var directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        foreach (var subdir in directories)
        {
            // Check for ~
            string subdirName = Path.GetFileName(subdir);
            if (subdirName.StartsWith("~"))
                continue;

            // Check for manifest.json
            string manifestPath = Path.Combine(subdir, "manifest.json");
            if (File.Exists(manifestPath))
                continue;

            // Rename Directory
            int moveAttempts = 0;
            while (moveAttempts < 100)
                try
                {
                    string newPath = Path.Combine(path, $"~{subdirName}{((moveAttempts == 0) ? string.Empty : $"_{moveAttempts}")}");
                    if (Directory.Exists(newPath))
                        moveAttempts++;
                    else
                    {
                        Directory.Move(subdir, newPath);
                        if (!Directory.Exists(newPath) 
                            && Directory.Exists(subdir))
                            moveAttempts++;
                        else
                            break;
                    }
                }
                catch
                {
                    moveAttempts++;
                }
        }
    }
}