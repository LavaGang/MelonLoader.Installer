using System;
using System.Linq;
using System.Threading;

#if !DEBUG
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
#endif

namespace MelonLoader.Managers
{
    internal static class SelfUpdate
    {
        internal static GitHubAPI API = new GitHubAPI(URLs.Repositories.Installer);
        internal static GitHubAPI.ReleaseData.AssetData UpdateAssetData = null;

#if !DEBUG
        private static void Relaunch(string exepath)
        {
            ProcessStartInfo procinfo = new ProcessStartInfo(exepath);
            if ((Program.Arguments != null) && (Program.Arguments.Length > 0))
                procinfo.Arguments = string.Join(" ", Program.Arguments.Where(s => !string.IsNullOrEmpty(s)).Select(it => ("\"" + Regex.Replace(it, @"(\\+)$", @"$1$1") + "\""))); ;
            Process.Start(procinfo);
            Program.EndItAll();
        }

        internal static bool Check_FileName()
        {
            string exe_fullpath = Process.GetCurrentProcess().MainModule.FileName;
            string exe_path = Path.GetDirectoryName(exe_fullpath);
            string exe_name = Path.GetFileNameWithoutExtension(exe_fullpath);
            if (!exe_name.EndsWith(".tmp"))
            {
                string tmp_exe_path = Path.Combine(exe_path, (exe_name + ".tmp.exe"));
                if (File.Exists(tmp_exe_path))
                    File.Delete(tmp_exe_path);
                return false;
            }
            string new_exe_name = exe_name.Substring(0, (exe_name.Length - 4));
            string new_exe_path = Path.Combine(exe_path, (new_exe_name + ".exe"));
            if (File.Exists(new_exe_path))
                File.Delete(new_exe_path);
            File.Copy(exe_fullpath, new_exe_path);
            Relaunch(new_exe_path);
            return true;
        }
#endif

        internal static void Check_Repo()
        {
            new Thread(() =>
            {
                if (!Check_UpdateAvailable())
                {
                    TempFileCache.ClearCache();
                    FormHandler.Invoke(() =>
                    {
                        if (!FormHandler.IsClosing)
                            FormHandler.GetReleases();
                    });
                    return;
                }

                FormHandler.Invoke(() =>
                {
                    FormHandler.ShowInstallerUpdateNotice();
                    if (FormHandler.IsClosing)
                        return;
#if DEBUG
                    FormHandler.GetReleases();
#endif
                });
                if (FormHandler.IsClosing)
                    return;
#if !DEBUG
                if (Config.AutoUpdateInstaller)
                    DownloadUpdate();
#endif
            }).Start();
        }

        private static bool Check_UpdateAvailable()
        {
            API.Refresh(true);
            if (API.ReleasesTbl.Count <= 0)
                return false;
            GitHubAPI.ReleaseData releasedata = API.ReleasesTbl.FirstOrDefault(x => 
                !x.IsPreRelease
                && (x.Installer != null) 
                && !string.IsNullOrEmpty(x.Installer.Download) 
                && !string.IsNullOrEmpty(x.Installer.SHA512));
            if (releasedata == null)
                return false;
            UpdateAssetData = releasedata.Installer;
#if DEBUG
            FormHandler.mainForm.Debug_LatestInstallerRelease.Text = $"v{releasedata.Version}";
#endif
            Version currentVersion = new Version(BuildInfo.Version);
            Version releaseVersion = new Version(releasedata.Version.Replace("v", ""));
            return (currentVersion.CompareTo(releaseVersion) < 0);
        }

#if !DEBUG
        private static void DownloadUpdate()
        {
            FormHandler.Invoke(() =>
            {
                FormHandler.SetStage(FormHandler.StageEnum.Output);
                FormHandler.SetOutputCurrentOperation("Downloading Installer Update...", ThemeHandler.GetOutputOperationColor());
            });
            string temp_path = TempFileCache.CreateFile();
            bool was_download_successful = WebClientInterface.DownloadFile(UpdateAssetData.Download, temp_path, (percentage) =>
            {
                FormHandler.Invoke(() =>
                {
                    FormHandler.SetOutputCurrentPercentage(percentage);
                    FormHandler.SetOutputTotalPercentage(percentage);
                });
            });
            if (!was_download_successful)
            {
                TempFileCache.ClearCache();
                if (!FormHandler.IsClosing)
                    DownloadFailed("TODO");
                return;
            }
            string download_sha512 = WebClientInterface.DownloadString(UpdateAssetData.SHA512);
            if (string.IsNullOrEmpty(download_sha512))
            {
                TempFileCache.ClearCache();
                if (!FormHandler.IsClosing)
                    DownloadFailed("TODO");
                return;
            }
            SHA512Managed sha512 = new SHA512Managed();
            byte[] checksum = sha512.ComputeHash(File.ReadAllBytes(temp_path));
            if ((checksum == null)
                || (checksum.Length <= 0))
            {
                TempFileCache.ClearCache();
                if (!FormHandler.IsClosing)
                    DownloadFailed("TODO");
                return;
            }
            string file_hash = BitConverter.ToString(checksum).Replace("-", string.Empty);
            if (string.IsNullOrEmpty(file_hash)
                || !file_hash.Equals(download_sha512))
            {
                TempFileCache.ClearCache();
                if (!FormHandler.IsClosing)
                    DownloadFailed("TODO");
                return;
            }
            string exe_path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string tmp_file_path = Path.Combine(exe_path, (Path.GetFileNameWithoutExtension(UpdateAssetData.Download) + ".tmp.exe"));
            if (File.Exists(tmp_file_path))
                File.Delete(tmp_file_path);
            File.Move(temp_path, tmp_file_path);
            FormHandler.Invoke(() => FormHandler.SetStage(FormHandler.StageEnum.Output_Success));
            Relaunch(tmp_file_path);
        }
#endif

        private static void DownloadFailed(string reason)
        {
            FormHandler.Invoke(() => FormHandler.SetStage(FormHandler.StageEnum.Output_Failure));

            // Handle Failure
            FormHandler.SpawnMessageBox(reason, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxButtons.OK);

            FormHandler.Invoke(FormHandler.GetReleases);
        }
    }
}