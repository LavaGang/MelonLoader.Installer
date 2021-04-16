using System;
using System.IO;
using System.Net;

namespace MelonLoader.Managers
{
    internal static class WebClientInterface
    {
        private static WebClient webClient = new WebClient();
        private static Action<int> DownloadProgressCallback;
        static WebClientInterface()
        {
            SetUserAgent("request");
            webClient.DownloadProgressChanged +=
                (object sender, DownloadProgressChangedEventArgs info) =>
                    DownloadProgressCallback?.Invoke(info.ProgressPercentage);
        }
        internal static void SetUserAgent(string agent) { webClient.Headers.Clear(); webClient.Headers.Add("User-Agent", agent); }
        internal static void CancelAsync() => webClient.CancelAsync();

        internal static string DownloadString(string url)
        {
            string response = null;
            try { response = webClient.DownloadString(url); } catch (Exception ex) { HandleException(ex); response = null; }
            if (FormHandler.IsClosing
                || string.IsNullOrEmpty(response))
                response = null;
            return response;
        }

        internal static bool DownloadFile(string url, string filepath, Action<int> callback)
        {
            bool was_successful = false;
            DownloadProgressCallback = callback;
            try
            {
                webClient.DownloadFileAsync(new Uri(url), filepath);
                while (webClient.IsBusy) { }
                was_successful = !FormHandler.IsClosing && File.Exists(filepath);
            }
            catch (Exception ex) { HandleException(ex, filepath); was_successful = false; }
            DownloadProgressCallback = null;
            return was_successful;
        }

        private static void HandleException(Exception ex, string filepath = null)
        {
            if (!string.IsNullOrEmpty(filepath) 
                && File.Exists(filepath))
                File.Delete(filepath);

            // Handle Exception Error
        }
    }
}
