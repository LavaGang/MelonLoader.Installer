namespace MelonLoader.Installer;

public static class InstallerUtils
{
    public static HttpClient Http { get; private set; }

    static InstallerUtils()
    {
        Http = new();
        Http.DefaultRequestHeaders.Add("User-Agent", "MelonLoader Installer");
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
}

public delegate void InstallProgressEventHandler(double progress, string? newStatus);

public delegate void InstallFinishedEventHandler(string? errorMessage);