using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader.LightJson;

namespace MelonLoader.Interfaces
{
    internal class GitHub
    {
        internal List<ReleaseData> ReleasesTbl = new List<ReleaseData>();
        private string API_URL = null;
        internal GitHub(string url) => API_URL = url;

        internal void Refresh(bool is_installer = false)
        {
            ReleasesTbl.Clear();
            if (is_installer)
                WebRequest.SetUserAgent("request");
            else
                WebRequest.SetUserAgent("Unity web player");
            string response = WebRequest.DownloadString(API_URL);
            if (string.IsNullOrEmpty(response))
                return;
            JsonValue responseval = JsonValue.Parse(response);
            if (responseval.IsNull)
                return;
            JsonArray data = responseval.AsJsonArray;
            if (data.Count <= 0)
                return;
            foreach (JsonValue release in data)
            {
                if (release.IsNull)
                    continue;
                JsonValue assetsval = release["assets"].AsJsonArray;
                if (assetsval.IsNull)
                    continue;
                JsonArray assets = assetsval.AsJsonArray;
                if (assets.Count <= 0)
                    continue;
                JsonValue releaseVersionval = release["tag_name"];
                if (releaseVersionval.IsNull)
                    continue;
                string releaseVersion = releaseVersionval.AsString;
                if (string.IsNullOrEmpty(releaseVersion))
                    continue;
                bool has_windows_x86 = (!releaseVersion.StartsWith("v0.2") && !releaseVersion.StartsWith("v0.1"));
                //bool has_android_quest = (has_windows_x86 && !releaseVersion.StartsWith("v0.3"));
                ReleaseData releaseData = new ReleaseData()
                {
                    Version = releaseVersion,
                    IsPreRelease = release["prerelease"].AsBoolean,
                    Installer = GetAssetDataWithFileName(assets, "MelonLoader.Installer.exe", is_installer),
                    Windows_x86 = GetAssetDataWithFileName(assets, "MelonLoader.x86.zip", !is_installer && has_windows_x86),
                    Windows_x64 = GetAssetDataWithFileName(assets, "MelonLoader.x64.zip", !is_installer),
                    //Windows_x86 = GetAssetDataWithFileName(assets, has_android_quest ? "MelonLoader.Windows.x86.zip" : "MelonLoader.x86.zip", !is_installer && has_windows_x86),
                    //Windows_x64 = GetAssetDataWithFileName(assets, has_android_quest ? "MelonLoader.Windows.x64.zip" : "MelonLoader.x64.zip", !is_installer),
                    //Android_Quest = GetAssetDataWithFileName(assets, "MelonLoader.Android_Quest.zip", !is_installer && has_android_quest)
                };
                ReleasesTbl.Add(releaseData);
            }
            ReleasesTbl = ReleasesTbl.OrderBy(x => x.Version).ToList();
            ReleasesTbl.Reverse();
        }

        internal static ReleaseData.AssetData GetAssetDataWithFileName(JsonArray assets, string filename, bool shouldcheck = true) =>
            new ReleaseData.AssetData()
            {
                Download = shouldcheck ? GetBrowserDownloadURLWithFileName(assets, $"{filename}") : null,
                SHA512 = shouldcheck ? GetBrowserDownloadURLWithFileName(assets, $"{Path.GetFileNameWithoutExtension(filename)}.sha512") : null
            };

        private static string GetBrowserDownloadURLWithFileName(JsonArray assets, string filename)
        {
            foreach (JsonValue asset in assets)
            {
                if (asset.IsNull)
                    continue;
                JsonValue nameval = asset["name"];
                if (nameval.IsNull)
                    continue;
                string name = nameval.AsString;
                if (string.IsNullOrEmpty(name))
                    continue;
                JsonValue downloadurlval = asset["browser_download_url"];
                if (downloadurlval.IsNull)
                    continue;
                string downloadurl = downloadurlval.AsString;
                if (string.IsNullOrEmpty(downloadurl))
                    continue;
                if (name.Equals(filename))
                    return downloadurl;
            }
            return null;
        }

        internal class ReleaseData
        {
            internal string Version;
            internal bool IsPreRelease;

            internal class AssetData
            {
                internal string Download;
                internal string SHA512;
            }
            internal AssetData Installer;
            internal AssetData Windows_x86;
            internal AssetData Windows_x64;
            //internal AssetData Android_Quest;
        }
    }
}