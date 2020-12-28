using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using MelonLoader.LightJson;

namespace MelonLoader
{
    internal static class ThreadHandler
    {
        internal static void CheckForInstallerUpdate()
        {
            Program.webClient.Headers.Add("User-Agent", "request");
            string response = null;
            try { response = Program.webClient.DownloadString(Config.Repo_API_Installer); } catch { response = null; }
            if (string.IsNullOrEmpty(response))
            {
                GetReleases();
                return;
            }
            if (Program.Closing)
                return;
            JsonArray data = JsonValue.Parse(response).AsJsonArray;
            if (data.Count <= 0)
            {
                GetReleases();
                return;
            }
            JsonValue release = data[0];
            JsonArray assets = release["assets"].AsJsonArray;
            if (assets.Count <= 0)
            {
                GetReleases();
                return;
            }
            string version = release["tag_name"].AsString;
            if (version.Equals(BuildInfo.Version))
            {
                GetReleases();
                return;
            }
            Program.mainForm.Invoke(new Action(() => { Program.mainForm.InstallerUpdateNotice.Visible = true; }));
            if (!Config.AutoUpdateInstaller)
            {
                GetReleases();
                return;
            }
            OperationHandler.CurrentOperation = OperationHandler.Operations.INSTALLER_UPDATE;
            Program.mainForm.Invoke(new Action(() => {
                Program.mainForm.Tab_PleaseWait.Text = "UPDATE   ";
                Program.mainForm.PleaseWait_Text.Text = "Downloading Update...";
            }));
            string downloadurl = assets[0]["browser_download_url"].AsString;
            string temp_path = TempFileCache.CreateFile();
            try { Program.webClient.DownloadFileAsync(new Uri(downloadurl), temp_path); while (Program.webClient.IsBusy) { } }
            catch
            {
                TempFileCache.ClearCache();
                GetReleases();
                return;
            }
            if (Program.Closing)
                return;
            string repo_hash = null;
            try { repo_hash = Program.webClient.DownloadString(assets[1]["browser_download_url"].AsString); } catch { repo_hash = null; }
            if (string.IsNullOrEmpty(repo_hash))
            {
                TempFileCache.ClearCache();
                GetReleases();
                return;
            }
            if (Program.Closing)
                return;
            SHA512Managed sha512 = new SHA512Managed();
            byte[] checksum = sha512.ComputeHash(File.ReadAllBytes(temp_path));
            if ((checksum == null) || (checksum.Length <= 0))
            {
                TempFileCache.ClearCache();
                GetReleases();
                return;
            }
            string file_hash = BitConverter.ToString(checksum).Replace("-", string.Empty);
            if (string.IsNullOrEmpty(file_hash) || !file_hash.Equals(repo_hash))
            {
                TempFileCache.ClearCache();
                GetReleases();
                return;
            }
            string exe_path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string tmp_file_path = Path.Combine(exe_path, (Path.GetFileNameWithoutExtension(downloadurl) + ".tmp.exe"));
            if (File.Exists(tmp_file_path))
                File.Delete(tmp_file_path);
            File.Move(temp_path, tmp_file_path);
            Process.Start(tmp_file_path);
            Process.GetCurrentProcess().Kill();
        }

        internal static void GetReleases()
        {
            Program.webClient.Headers.Clear();
            Program.webClient.Headers.Add("User-Agent", "Unity web player");
            Program.mainForm.Invoke(new Action(() => {
                Program.mainForm.Tab_PleaseWait.Text = Program.mainForm.Tab_Automated.Text;
                Program.mainForm.PleaseWait_Text.Text = "Getting List of Releases from GitHub...";
                Program.mainForm.PleaseWait_Text.Location = new Point(105, 79);
                Program.mainForm.PleaseWait_Text.Size = new Size(250, 22);
                int current_index = Program.mainForm.PageManager.SelectedIndex;
                Program.mainForm.PageManager.Controls.Clear();
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_PleaseWait);
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_ManualZip);
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_Settings);
                Program.mainForm.PageManager.SelectedIndex = current_index;
                Program.mainForm.PageManager.Cursor = Cursors.Hand;
            }));
            ParseReleasesURL();
            Program.mainForm.Invoke(new Action(() =>
            {
                int current_index = Program.mainForm.PageManager.SelectedIndex;
                Program.mainForm.PageManager.Controls.Clear();
                if (Program.mainForm.Automated_Version_Selection.Items.Count <= 0)
                    Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_Error);
                else
                {
                    Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_Automated);
                    Program.mainForm.Automated_Version_Selection.SelectedIndex = 0;
                }
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_ManualZip);
                Program.mainForm.PageManager.Controls.Add(Program.mainForm.Tab_Settings);
                Program.mainForm.PageManager.SelectedIndex = current_index;
            }));
        }

        internal static void ParseReleasesURL()
        {
            string response = null;
            try { response = Program.webClient.DownloadString(Config.Repo_API_MelonLoader); } catch { response = null; }
            if (string.IsNullOrEmpty(response))
                return;
            JsonArray data = JsonValue.Parse(response).AsJsonArray;
            if (data.Count <= 0)
                return;
            List<string> releasesList = new List<string>();
            foreach (JsonValue release in data)
            {
                JsonArray assets = release["assets"].AsJsonArray;
                if (assets.Count <= 0)
                    continue;
                if (!Config.ShowAlphaReleases && release["prerelease"].AsBoolean)
                    continue;
                string version = release["tag_name"].AsString;
                releasesList.Add(version);
            }
            releasesList.Sort();
            releasesList.Reverse();
            Program.mainForm.Invoke(new Action(() => { Program.mainForm.Automated_Version_Selection.Items.Clear(); Program.mainForm.Automated_Version_Selection.Items.AddRange(releasesList.ToArray()); }));
        }
    }
}
