using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer;

internal static class MLManager
{
    private static bool inited;
    private static readonly string[] proxyNames = ["version", "winmm", "winhttp"];

    public static MLVersion[] Versions { get; private set; } = [];

    public static void Init()
    {
        if (inited)
            return;

        inited = RefreshVersions();
    }

    public static bool RefreshVersions()
    {
        var versions = GetVersionsAsync().GetAwaiter().GetResult();
        if (versions == null)
        {
            Versions = [];
            return false;
        }

        Versions = [.. versions];
        return true;
    }

    private static async Task<List<MLVersion>?> GetVersionsAsync()
    {
        HttpResponseMessage resp;
        try
        {
            resp = await InstallerUtils.Http.GetAsync(Config.MelonLoaderBuildWorkflowApi).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }

        if (!resp.IsSuccessStatusCode)
            return null;

        var relStr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var runsJson = JsonNode.Parse(relStr)!["workflow_runs"]!.AsArray();

        var versionsList = new List<MLVersion>();

        foreach (var run in runsJson)
        {
            try
            {
                resp = await InstallerUtils.Http.GetAsync(run!["artifacts_url"]!.ToString()).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }

            if (!resp.IsSuccessStatusCode)
                return null;

            relStr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var artifactsJson = JsonNode.Parse(relStr)!["artifacts"]!.AsArray();

            var art64 = artifactsJson.FirstOrDefault(x => x!["name"]!.ToString() == "MelonLoader.Windows.x64.CI.Release");
            var art86 = artifactsJson.FirstOrDefault(x => x!["name"]!.ToString() == "MelonLoader.Windows.x86.CI.Release");

            if (art64 != null)
            {
                versionsList.Add(new()
                {
                    Id = (int)run["run_number"]!,
                    VersionName = "Nightly #" + run["run_number"]!,
                    IsArtifact = true,
                    IsX86 = false,
                    DownloadUrl = $"https://nightly.link/LavaGang/MelonLoader/suites/{run["id"]}/artifacts/{art64["id"]}"
                });
            }

            if (art86 != null)
            {
                versionsList.Add(new()
                {
                    Id = (int)run["run_number"]!,
                    VersionName = "Nightly #" + run["run_number"]!,
                    IsArtifact = true,
                    IsX86 = true,
                    DownloadUrl = $"https://nightly.link/LavaGang/MelonLoader/suites/{run["id"]}/artifacts/{art86["id"]}"
                });
            }
        }

        try
        {
            resp = await InstallerUtils.Http.GetAsync(Config.MelonLoaderReleasesApi).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }

        if (!resp.IsSuccessStatusCode)
            return null;

        relStr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var releasesJson = JsonNode.Parse(relStr)!.AsArray();

        foreach (var release in releasesJson)
        {
            if (release == null || release["tag_name"] == null || release["assets"] == null || release["assets"]!.AsArray().Count == 0)
                continue;

            var versionName = release["tag_name"]!.ToString();

            if (versionName.StartsWith("v0.1.") || versionName == "v0.2" || versionName.StartsWith("v0.2."))
                continue;

            var x64Asset = release["assets"]!.AsArray().FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x64.zip");
            var x86Asset = release["assets"]!.AsArray().FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x86.zip");

            if (x64Asset != null)
            {
                versionsList.Add(new()
                {
                    Id = (int)x64Asset["id"]!,
                    VersionName = versionName,
                    IsArtifact = false,
                    IsX86 = false,
                    DownloadUrl = x64Asset["browser_download_url"]!.ToString()
                });
            }

            if (x86Asset != null)
            {
                versionsList.Add(new()
                {
                    Id = (int)x86Asset["id"]!,
                    VersionName = versionName,
                    IsArtifact = false,
                    IsX86 = true,
                    DownloadUrl = x86Asset["browser_download_url"]!.ToString()
                });
            }
        }

        return versionsList;
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

    public static async Task InstallAsync(string gameDir, bool removeUserFiles, MLVersion version, bool x86, InstallProgressEventHandler? onProgress, InstallFinishedEventHandler? onFinished)
    {
        ZipArchive? zip = null;
        if (version.IsLocalZip)
        {
            if (!File.Exists(version.DownloadUrl))
            {
                onFinished?.Invoke("The selected zip archive no longer exists!");
                return;
            }

            try
            {
                var zipStr = File.OpenRead(version.DownloadUrl);
                zip ??= new ZipArchive(zipStr, ZipArchiveMode.Read);
            }
            catch (IOException)
            {
                onFinished?.Invoke("Failed to open the zip archive. Make sure the zip isn't being used by other programs.");
                return;
            }
            catch
            {
                onFinished?.Invoke("The selected zip archive contains invalid data.");
                return;
            }

            if (!zip.Entries.Any(x => x.FullName.StartsWith("MelonLoader/")))
            {
                onFinished?.Invoke("The selected zip archive does not contain a valid MelonLoader build.");
                zip.Dispose();
                return;
            }
        }

        var tasks = 1;
        var currentTask = 0;

        void SetProgress(double progress, string? newStatus = null)
        {
            onProgress?.Invoke(currentTask / (double)tasks + progress / tasks, newStatus);
        }

        SetProgress(0, "Uninstalling previous versions");

        if (!Uninstall(gameDir, removeUserFiles, out var error))
        {
            onFinished?.Invoke(error);
            zip?.Dispose();
            return;
        }

        MemoryStream? bufferStr = null;
        if (zip == null)
        {
            tasks++;

            SetProgress(0, "Downloading MelonLoader " + version.VersionName);

            bufferStr = new MemoryStream();
            var result = await InstallerUtils.DownloadFileAsync(version.DownloadUrl, bufferStr, SetProgress);
            if (result != null)
            {
                onFinished?.Invoke("Failed to download MelonLoader: " + result);
                return;
            }
            bufferStr.Seek(0, SeekOrigin.Begin);

            currentTask++;
        }

        SetProgress(0, "Installing " + version.VersionName);

        try
        {
            if (zip == null)
            {
                if (bufferStr != null)
                {
                    zip = new ZipArchive(bufferStr, ZipArchiveMode.Read);
                }
                else
                {
                    // This should never happen
                    onFinished?.Invoke("Lemon?");
                    return;
                }
            }

            var zipLength = zip.Entries.Count;
            for (var i = 0; i < zipLength; i++)
            {
                var entry = zip.Entries[i];
                var dest = Path.Combine(gameDir, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                entry.ExtractToFile(dest, true);

                SetProgress(i / (double)(zipLength - 1));
            }
        }
        catch (InvalidDataException)
        {
            onFinished?.Invoke("Failed to install MelonLoader: The downloaded data seems to be corrupt.");
            return;
        }
        catch
        {
            onFinished?.Invoke("Failed to install MelonLoader: Failed to extract all files.");
            return;
        }
        finally
        {
            zip?.Dispose();
        }

        Directory.CreateDirectory(Path.Combine(gameDir, "Mods"));
        Directory.CreateDirectory(Path.Combine(gameDir, "Plugins"));
        Directory.CreateDirectory(Path.Combine(gameDir, "UserData"));
        Directory.CreateDirectory(Path.Combine(gameDir, "UserLibs"));

        if (!CheckDotnetInstalled(x86))
        {
            // Reset the entire progress bar
            currentTask = 0;
            tasks = 1;

            SetProgress(0, "Downloading .NET 6.0");

            var installerPath = Path.GetTempFileName() + ".exe";
            using (var dnStr = File.Create(installerPath))
            {
                var dnResult = await InstallerUtils.DownloadFileAsync(x86 ? Config.DotnetRuntimeX86Download : Config.DotnetRuntimeX64Download, dnStr, SetProgress);
                if (dnResult != null)
                {
                    onFinished?.Invoke("Failed to download the .NET Runtime: " + dnResult);
                    return;
                }
            }

            SetProgress(1, "Installing .NET 6.0");

            try
            {
                await Process.Start(installerPath, "/install /passive /norestart").WaitForExitAsync();
            }
            catch (Exception ex)
            {
                onFinished?.Invoke("Failed to install .NET Runtime. " + ex.Message);
                return;
            }

            File.Delete(installerPath);

            if (!CheckDotnetInstalled(x86))
            {
                onFinished?.Invoke("Failed to install .NET Runtime. Make sure to give admin permissions if prompted.");
                return;
            }

            currentTask++;
        }

        onFinished?.Invoke(null);
    }

    private static bool CheckDotnetInstalled(bool x86)
    {
        var dotnetDir = $@"C:\Program Files{(x86 ? " (x86)" : string.Empty)}\dotnet\shared\Microsoft.NETCore.App";
        return Directory.Exists(dotnetDir) && Directory.EnumerateDirectories(dotnetDir, "6.*").Any();
    }
}