using Semver;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer;

internal static class MLManager
{
    private static bool inited;
    internal static readonly string[] proxyNames = ["version", "winmm", "winhttp"];

    private static MLVersion? localBuild;

    public static List<MLVersion> Versions { get; private set; } = [];
    public static bool Initialized { get; private set; }

    static MLManager()
    {
        Program.Exiting += HandleExit;
    }

    private static void HandleExit()
    {
        if (Directory.Exists(Config.LocalZipCache))
        {
            try
            {
                Directory.Delete(Config.LocalZipCache, true);
            }
            catch { }
        }
    }

    public static bool Init()
    {
        if (inited)
            return true;

        inited = RefreshVersions();
        return inited;
    }

    private static bool RefreshVersions()
    {
        Versions.Clear();

        if (localBuild != null)
            Versions.Add(localBuild);

        return GetVersionsAsync(Versions).GetAwaiter().GetResult();
    }

    private static async Task<bool> GetVersionsAsync(List<MLVersion> versions)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await InstallerUtils.Http.GetAsync(Config.MelonLoaderBuildWorkflowApi).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }

        if (!resp.IsSuccessStatusCode)
            return false;

        var relStr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var runsJson = JsonNode.Parse(relStr)!["workflow_runs"]!.AsArray();

        // All run names must follow the following format: "{SemVersion} Remaining name"
        foreach (var run in runsJson)
        {
            var runName = run!["name"]!.ToString();
            var runVerEnd = runName.IndexOf(' ');
            if (runVerEnd == -1)
                continue;

            if (!SemVersion.TryParse(runName[..runVerEnd], SemVersionStyles.Any, out var runVersion))
                continue;

            try
            {
                resp = await InstallerUtils.Http.GetAsync(run!["artifacts_url"]!.ToString()).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }

            if (!resp.IsSuccessStatusCode)
                return false;

            relStr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var artifactsJson = JsonNode.Parse(relStr)!["artifacts"]!.AsArray();

            var art64 = artifactsJson.FirstOrDefault(x => x!["name"]!.ToString() == "MelonLoader.Windows.x64.CI.Release");
            var art86 = artifactsJson.FirstOrDefault(x => x!["name"]!.ToString() == "MelonLoader.Windows.x86.CI.Release");

            var version = new MLVersion()
            {
                Version = runVersion,
                DownloadUrl = art64 != null ? $"https://nightly.link/LavaGang/MelonLoader/suites/{run["id"]}/artifacts/{art64["id"]}" : null,
                DownloadX86Url = art86 != null ? $"https://nightly.link/LavaGang/MelonLoader/suites/{run["id"]}/artifacts/{art86["id"]}" : null
            };

            if (version.DownloadUrl == null && version.DownloadX86Url == null)
                continue;

            versions.Add(version);
        }

        try
        {
            resp = await InstallerUtils.Http.GetAsync(Config.MelonLoaderReleasesApi).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }

        if (!resp.IsSuccessStatusCode)
            return false;

        relStr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var releasesJson = JsonNode.Parse(relStr)!.AsArray();

        foreach (var release in releasesJson)
        {
            if (!SemVersion.TryParse(release!["tag_name"]!.ToString(), SemVersionStyles.Any, out var relVersion))
                continue;

            if (relVersion.Major == 0 && relVersion.Minor <= 2)
                continue;

            var x64Asset = release["assets"]!.AsArray().FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x64.zip");
            var x86Asset = release["assets"]!.AsArray().FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x86.zip");

            var version = new MLVersion()
            {
                Version = relVersion,
                DownloadUrl = x64Asset != null ? x64Asset["browser_download_url"]!.ToString() : null,
                DownloadX86Url = x86Asset != null ? x86Asset["browser_download_url"]!.ToString() : null
            };

            if (version.DownloadUrl == null && version.DownloadX86Url == null)
                continue;

            versions.Add(version);
        }

        return true;
    }

    public static bool Uninstall(string gameDir, bool removeUserFiles, [NotNullWhen(false)] out string? errorMessage)
    {
        if (!Directory.Exists(gameDir))
        {
            errorMessage = "The provided directory does not exist.";
            return false;
        }

        foreach (var proxy in proxyNames)
        {
            var proxyPath = Path.Combine(gameDir, proxy + ".dll");
            if (!File.Exists(proxyPath))
                continue;

            var versionInf = FileVersionInfo.GetVersionInfo(proxyPath);
            if (versionInf.LegalCopyright != null && versionInf.LegalCopyright.Contains("Microsoft"))
                continue;

            try
            {
                File.Delete(proxyPath);
            }
            catch
            {
                errorMessage = "Failed to uninstall MelonLoader. Ensure that the game is fully closed before trying again.";
                return false;
            }
        }

        var mlDir = Path.Combine(gameDir, "MelonLoader");
        if (Directory.Exists(mlDir))
        {
            try
            {
                Directory.Delete(mlDir, true);
            }
            catch
            {
                errorMessage = "Failed to uninstall MelonLoader. Ensure that the game is fully closed before trying again.";
                return false;
            }
        }

        var dobbyPath = Path.Combine(gameDir, "dobby.dll");
        if (File.Exists(dobbyPath))
        {
            try
            {
                File.Delete(dobbyPath);
            }
            catch
            {
                errorMessage = $"Failed to fully uninstall MelonLoader: Failed to remove dobby.";
                return false;
            }
        }

        var noticePath = Path.Combine(gameDir, "NOTICE.txt");
        if (File.Exists(noticePath))
        {
            try
            {
                File.Delete(noticePath);
            }
            catch
            {
                errorMessage = $"Failed to fully uninstall MelonLoader: Failed to remove 'NOTICE.txt'.";
                return false;
            }
        }

        if (removeUserFiles)
        {
            var modsDir = Path.Combine(gameDir, "Mods");
            if (Directory.Exists(modsDir))
            {
                try
                {
                    Directory.Delete(modsDir, true);
                }
                catch
                {
                    errorMessage = $"Failed to fully uninstall MelonLoader: Failed to remove the Mods folder.";
                    return false;
                }
            }

            var pluginsDir = Path.Combine(gameDir, "Plugins");
            if (Directory.Exists(pluginsDir))
            {
                try
                {
                    Directory.Delete(pluginsDir, true);
                }
                catch
                {
                    errorMessage = $"Failed to fully uninstall MelonLoader: Failed to remove the Plugins folder.";
                    return false;
                }
            }

            var userDataDir = Path.Combine(gameDir, "UserData");
            if (Directory.Exists(userDataDir))
            {
                try
                {
                    Directory.Delete(userDataDir, true);
                }
                catch
                {
                    errorMessage = $"Failed to fully uninstall MelonLoader: Failed to remove the UserData folder.";
                    return false;
                }
            }

            var userLibsDir = Path.Combine(gameDir, "UserLibs");
            if (Directory.Exists(userLibsDir))
            {
                try
                {
                    Directory.Delete(userLibsDir, true);
                }
                catch
                {
                    errorMessage = $"Failed to fully uninstall MelonLoader: Failed to remove the UserLibs folder.";
                    return false;
                }
            }
        }

        errorMessage = null;
        return true;
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

        if (Directory.Exists(Config.LocalZipCache))
        {
            try
            {
                Directory.Delete(Config.LocalZipCache, true);
            }
            catch
            {
                onFinished?.Invoke("Failed to remove the previously extracted zip data.");
                return;
            }
        }

        onProgress?.Invoke(0, "Extracting local zip archive");

        using var zipStr = File.OpenRead(zipPath);
        var extRes = InstallerUtils.Extract(zipStr, Config.LocalZipCache, onProgress);
        if (extRes != null)
        {
            onFinished?.Invoke(extRes);
            return;
        }

        var mlVer = MLVersion.GetMelonLoaderVersion(Config.LocalZipCache, out var x86);
        if (mlVer == null)
        {
            onFinished?.Invoke("The selected zip archive does not contain a valid MelonLoader build.");
            return;
        }

        var version = new MLVersion()
        {
            Version = mlVer,
            DownloadUrl = !x86 ? Config.LocalZipCache : null,
            DownloadX86Url = x86 ? Config.LocalZipCache : null,
            IsLocalPath = true
        };

        localBuild = version;
        Versions.Insert(0, version);

        onFinished?.Invoke(null);
    }

    public static async Task InstallAsync(string gameDir, bool removeUserFiles, MLVersion version, bool x86, InstallProgressEventHandler? onProgress, InstallFinishedEventHandler? onFinished)
    {
        var downloadUrl = x86 ? version.DownloadX86Url : version.DownloadUrl;
        if (downloadUrl == null)
        {
            onFinished?.Invoke($"The selected version does not support the selected architecture: {(x86 ? "x86" : "x64")}");
            return;
        }

        onProgress?.Invoke(0, "Uninstalling previous versions");

        if (!Uninstall(gameDir, removeUserFiles, out var error))
        {
            onFinished?.Invoke(error);
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

            SetProgress(0, "Downloading MelonLoader " + version.ToString());

            using var bufferStr = new MemoryStream();
            var result = await InstallerUtils.DownloadFileAsync(downloadUrl, bufferStr, SetProgress);
            if (result != null)
            {
                onFinished?.Invoke("Failed to download MelonLoader: " + result);
                return;
            }
            bufferStr.Seek(0, SeekOrigin.Begin);

            currentTask++;

            SetProgress(0, "Installing " + version.ToString());

            var extRes = InstallerUtils.Extract(bufferStr, gameDir, SetProgress);
            if (extRes != null)
            {
                onFinished?.Invoke(extRes);
                return;
            }
        }

        Directory.CreateDirectory(Path.Combine(gameDir, "Mods"));
        Directory.CreateDirectory(Path.Combine(gameDir, "Plugins"));
        Directory.CreateDirectory(Path.Combine(gameDir, "UserData"));
        Directory.CreateDirectory(Path.Combine(gameDir, "UserLibs"));

        onFinished?.Invoke(null);
    }
}