using System.Collections.Generic;
using MelonLoader.LightJson;

namespace MelonLoader
{
    internal static class Releases
    {
        internal static List<string> Official = new List<string>();
        internal static List<string> All = new List<string>();

        internal static void RequestLists()
        {
            Program.webClient.Headers.Clear();
            Program.webClient.Headers.Add("User-Agent", "Unity web player");
            string response = null;
            try { response = Program.webClient.DownloadString(Config.Repo_API_MelonLoader); } catch { response = null; }
            if (string.IsNullOrEmpty(response))
                return;
            if (Program.Closing)
                return;
            JsonArray data = JsonValue.Parse(response).AsJsonArray;
            if (data.Count <= 0)
                return;
            Official.Clear();
            All.Clear();
            foreach (JsonValue release in data)
            {
                JsonArray assets = release["assets"].AsJsonArray;
                if (assets.Count <= 0)
                    continue;

                string version = release["tag_name"].AsString;
                if (version.StartsWith("v0.2") || version.StartsWith("v0.1"))
                    continue;

                if (!release["prerelease"].AsBoolean)
                    Official.Add(version);
                All.Add(version);
            }
            Official.Sort();
            Official.Reverse();
            All.Sort();
            All.Reverse();
        }
    }
}
