using Semver;
using System.IO.Compression;
#if LINUX || OSX
using System.Runtime.InteropServices;
#endif

namespace MelonLoader.Installer;

public static partial class InstallerUtils
{
    public static HttpClient Http { get; }

    static InstallerUtils()
    {
        Http = new();
        Http.DefaultRequestHeaders.Add("User-Agent", $"MelonLoader Installer v{Program.VersionName}");
    }

    public static async Task<string?> DownloadFileAsync(string url, Stream destination, InstallProgressEventHandler? onProgress)
    {
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

#if LINUX || OSX
    // user permissions
    public const int S_IRUSR = 0x100;
    public const int S_IWUSR = 0x80;
    public const int S_IXUSR = 0x40;

    // group permission
    public const int S_IRGRP = 0x20;
    public const int S_IXGRP = 0x8;

    // other permissions
    public const int S_IROTH = 0x4;
    public const int S_IXOTH = 0x1;
        
    [LibraryImport("libc", EntryPoint = "chmod", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int Chmod(string pathname, int mode);
#endif
}

public delegate void InstallProgressEventHandler(double progress, string? newStatus);

public delegate void InstallFinishedEventHandler(string? errorMessage);