using System.IO.Compression;
using System.Security.Cryptography;

namespace MelonLoader.Installer;

public static class InstallerUtils
{
    public static HttpClient Http { get; }
    private static readonly SHA512 Hasher = SHA512.Create();

    static InstallerUtils()
    {
        Http = new();
        Http.DefaultRequestHeaders.Add("User-Agent", $"MelonLoader Installer v{Program.VersionName}");
    }

    private static async Task<string?> FetchFile(string url, Stream destination, bool useCache, InstallProgressEventHandler? onProgress)
    {
        // Cache preparation
        var parentDirectory = Path.GetFileName(Path.GetDirectoryName(url)) ?? "";
        var fileCache = Path.Combine(Config.CacheDir, "Cache", parentDirectory, Path.GetFileName(url));

        // Cache hits
        if (useCache && File.Exists(fileCache))
        {
            try
            {
                await using var fsOut = File.OpenRead(fileCache);
                await fsOut.CopyToAsync(destination);
                return null;
            }
            catch
            {
                return $"Failed to read the cache file {fileCache}";
            }
        }
        
        HttpResponseMessage response;
        try
        {
            response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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

        await using var content = await response.Content.ReadAsStreamAsync();

        var length = response.Content.Headers.ContentLength ?? 0;

        if (length > 0)
        {
            destination.SetLength(length);
            var position = 0;
            var buffer = new byte[1024 * 16];
            while (position < destination.Length - 1)
            {
                var read = await content.ReadAsync(buffer, 0, buffer.Length);
                await destination.WriteAsync(buffer, 0, read);

                position += read;

                onProgress?.Invoke(position / (double)(destination.Length - 1), null);
            }
        }
        else
        {
            await content.CopyToAsync(destination);
        }
        
        // Save cache
        if (useCache)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Config.CacheDir, "Cache", parentDirectory));
                await using var fsIn = File.OpenWrite(fileCache);
                destination.Seek(0, SeekOrigin.Begin);
                await destination.CopyToAsync(fsIn);
            }
            catch
            {
                // Failed to save cache
            }
        }
        return null;
    }

    public static async Task<string?> DownloadFileAsync(string url, Stream destination, bool useCache, InstallProgressEventHandler? onProgress)
    {
        // Get archive
        var result = await FetchFile(url, destination, useCache, onProgress);
        if (result != null)
            return $"Failed to fetch file from {url}: {result}";

        destination.Seek(0, SeekOrigin.Begin);
        
        // Get checksum
        var checksumUrl = url.Replace(".zip", ".sha512");
        using var checksumStr = new MemoryStream();
        result = await FetchFile(checksumUrl, checksumStr, useCache, onProgress);

        // Checksum fetch failed, skip verification
        if (result != null) return null;
        
        var checksumDownload = System.Text.Encoding.UTF8.GetString(checksumStr.ToArray());
        var checksumCompute = Convert.ToHexString(await Hasher.ComputeHashAsync(destination));
        
        // Verification successful
        if (checksumCompute == checksumDownload) return null;
        // Verification failed, remove corrupted files
        var parentDirectory = Path.GetFileName(Path.GetDirectoryName(url)) ?? "";
        File.Delete(Path.Combine(Config.CacheDir, "Cache", parentDirectory, Path.GetFileName(url)));
        File.Delete(Path.Combine(Config.CacheDir, "Cache", parentDirectory, Path.GetFileName(checksumUrl)));
        return "Fetched corrupted file (checksum mismatch)";
    }

    public static string? Extract(Stream archiveStream, string destination, InstallProgressEventHandler? onProgress)
    {
        Directory.CreateDirectory(destination);

        try
        {
            archiveStream.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(archiveStream, ZipArchiveMode.Read);

            var zipLength = zip.Entries.Count;
            for (var i = 0; i < zipLength; i++)
            {
                var entry = zip.Entries[i];
                if (entry.FullName.EndsWith('/'))
                    continue;

                var dest = Path.Combine(destination, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                entry.ExtractToFile(dest, true);

                onProgress?.Invoke(i / (double)(zipLength - 1), null);
            }
        }
        catch (InvalidDataException)
        {
            return "Failed to extract MelonLoader: The downloaded data seems to be corrupt.";
        }
        catch
        {
            return "Failed to extract MelonLoader: Failed to extract all files.";
        }

        return null;
    }
}

public delegate void InstallProgressEventHandler(double progress, string? newStatus);

public delegate void InstallFinishedEventHandler(string? errorMessage);