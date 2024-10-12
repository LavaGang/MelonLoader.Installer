using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer;

internal static class MLManager
{
    private static readonly HttpClient http = new();
    private static readonly string[] proxyNames = ["version", "winmm", "winhttp"];

    public static MLVersion[] Versions { get; private set; } = [];

    static MLManager()
    {
        http = new();
        http.DefaultRequestHeaders.Add("User-Agent", "MelonLoader Installer");
    }

    public static void Init()
    {
        RefreshVersions();
    }

    public static void RefreshVersions()
    {
        var versions = GetVersionsAsync().GetAwaiter().GetResult();
        if (versions == null)
        {
            Versions = [];
            return;
        }

        Versions = [.. versions];
    }

    private static async Task<List<MLVersion>?> GetVersionsAsync()
    {
        HttpResponseMessage resp;
        try
        {
            resp = await http.GetAsync(Config.MelonLoaderBuildWorkflowApi).ConfigureAwait(false);
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
                resp = await http.GetAsync(run!["artifacts_url"]!.ToString()).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }

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
            resp = await http.GetAsync(Config.MelonLoaderReleasesApi).ConfigureAwait(false);
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
                errorMessage = $"Failed to fully uninstall MelonLoader: Failed to remove proxy '{proxy}'.";
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
        onProgress?.Invoke(0, "Uninstalling previous versions");

        if (!Uninstall(gameDir, removeUserFiles, out var error))
        {
            onFinished?.Invoke(error);
            return;
        }

        var tasks = 2;
        var currentTask = 0;

        void SetProgress(double progress, string? newStatus = null)
        {
            onProgress?.Invoke(currentTask / (double)tasks + progress / tasks, newStatus);
        }

        if (!CheckDotnetInstalled(x86))
        {
            tasks++;

            SetProgress(0, "Downloading .NET 6.0");

            var installerPath = Path.GetTempFileName() + ".exe";
            string? dnResult;
            using (var dnStr = File.Create(installerPath))
            {
                dnResult = await DownloadFileAsync(x86 ? Config.DotnetRuntimeX86Download : Config.DotnetRuntimeX64Download, dnStr, SetProgress);
            }

            SetProgress(1, "Installing .NET 6.0");

            await Process.Start(installerPath, "/install /passive /norestart").WaitForExitAsync();

            File.Delete(installerPath);

            if (!CheckDotnetInstalled(x86))
            {
                onFinished?.Invoke("Failed to install .NET Runtime. Make sure to give admin permissions if prompted.");
                return;
            }

            currentTask++;
        }

        SetProgress(0, "Downloading MelonLoader " + version.VersionName);

        using var bufferStr = new MemoryStream();
        var result = await DownloadFileAsync(version.DownloadUrl, bufferStr, SetProgress);
        if (result != null)
        {
            onFinished?.Invoke("Failed to download MelonLoader: " + result);
            return;
        }
        bufferStr.Seek(0, SeekOrigin.Begin);

        currentTask++;

        SetProgress(0, "Installing MelonLoader " + version.VersionName);

        using var zip = new ZipArchive(bufferStr, ZipArchiveMode.Read);

        var zipLength = zip.Entries.Count;
        for (var i = 0; i < zipLength; i++)
        {
            var entry = zip.Entries[i];
            var dest = Path.Combine(gameDir, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, true);

            SetProgress(i / (double)(zipLength - 1));
        }

        Directory.CreateDirectory(Path.Combine(gameDir, "Mods"));
        Directory.CreateDirectory(Path.Combine(gameDir, "Plugins"));
        Directory.CreateDirectory(Path.Combine(gameDir, "UserData"));
        Directory.CreateDirectory(Path.Combine(gameDir, "UserLibs"));

        onFinished?.Invoke(null);
    }

    private static bool CheckDotnetInstalled(bool x86)
    {
        var dotnetDir = $@"C:\Program Files{(x86 ? " (x86)" : string.Empty)}\dotnet\shared\Microsoft.NETCore.App";
        return Directory.Exists(dotnetDir) && Directory.EnumerateDirectories(dotnetDir, "6.*").Any();
    }

    private static async Task<string?> DownloadFileAsync(string url, Stream destination, InstallProgressEventHandler? onProgress)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (HttpRequestException)
        {
            return "Could not establish a connection.";
        }
        catch
        {
            return "Something went wrong while requesting the download files.";
        }

        if (!response.IsSuccessStatusCode)
        {
            return response.ReasonPhrase;
        }

        using var content = await response.Content.ReadAsStreamAsync();

        var length = response.Content.Headers.ContentLength ?? 0;

        if (length > 0)
        {
            destination.SetLength(length);
        }
        else
        {
            await content.CopyToAsync(destination);
            return null;
        }

        var position = 0;
        var buffer = new byte[1024 * 16];
        while (position < destination.Length - 1)
        {
            var read = await content.ReadAsync(buffer, 0, buffer.Length);
            await destination.WriteAsync(buffer, 0, read);

            position += read;

            onProgress?.Invoke(position / (double)(destination.Length - 1), null);
        }

        return null;
    }
}

public delegate void InstallProgressEventHandler(double progress, string? newStatus);

public delegate void InstallFinishedEventHandler(string? errorMessage);