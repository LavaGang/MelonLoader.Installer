using System;
using System.Linq;
using System.Threading;

#if !DEBUG
using System.Diagnostics;
using System.IO;
#endif

namespace MelonLoader.Managers
{
    internal static class SelfUpdate
    {
        internal static Interfaces.GitHub API = new Interfaces.GitHub(URLs.Repositories.Installer);
        internal static Interfaces.GitHub.ReleaseData.AssetData UpdateAssetData = null;

#if !DEBUG
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
            Program.Relaunch(new_exe_path);
            return true;
        }
#endif

        internal static void Check_Repo()
        {
            Program.CreateMessageBox("2", System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxButtons.OK, true);
            /*
            new Thread(() =>
            {
                if (!Check_UpdateAvailable())
                {
                    Form.Invoke(() =>
                    {
                        if (!Form.IsClosing)
                            Form.GetReleases();
                    });
                    return;
                }

                Form.Invoke(() =>
                {
                    Form.mainForm.InstallerUpdateNotice.Visible = true;
                    if (Form.IsClosing)
                        return;
#if DEBUG
                    Form.GetReleases();
#endif
                });
                if (Form.IsClosing)
                    return;
#if !DEBUG
                if (Config.AutoUpdate)
                    DownloadUpdate();
#endif
            }).Start();
            */
        }

        private static bool Check_UpdateAvailable()
        {
            API.Refresh(true);
            if (API.ReleasesTbl.Count <= 0)
                return false;
            Interfaces.GitHub.ReleaseData releasedata = API.ReleasesTbl.FirstOrDefault(x => 
                !x.IsPreRelease
                && (x.Installer != null) 
                && !string.IsNullOrEmpty(x.Installer.Download) 
                && !string.IsNullOrEmpty(x.Installer.SHA512));
            if (releasedata == null)
                return false;
            UpdateAssetData = releasedata.Installer;
#if DEBUG
            Form.mainForm.Debug_LatestInstallerRelease.Text = $"v{releasedata.Version}";
#endif
            Version currentVersion = new Version(BuildInfo.Version);
            Version releaseVersion = new Version(releasedata.Version.Replace("v", ""));
            return (currentVersion.CompareTo(releaseVersion) < 0);
        }

#if !DEBUG
        private static void DownloadUpdate()
        {
            Form.Invoke(() =>
            {
                Form.SetStage(Form.StageEnum.Output);
                Form.SetOutputCurrentOperation("Downloading Installer Update...", Theme.GetOutputOperationColor());
            });

            string exe_path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string temp_path = Path.Combine(exe_path, (Path.GetFileNameWithoutExtension(UpdateAssetData.Download) + ".tmp.exe"));
            
            Interfaces.DisposableFile disposableFile = new Interfaces.DisposableFile(temp_path);
            try
            {
                if (!disposableFile.Download(UpdateAssetData.Download, (percentage) =>
                {
                    Form.Invoke(() =>
                    {
                        Form.SetOutputCurrentPercentage(percentage);
                        Form.SetOutputTotalPercentage(percentage);
                    });
                }))
                {
                    disposableFile.Dispose();
                    DownloadFailed("TODO");
                    return;
                }

                if (!disposableFile.SHA512HashCheckFromURL(UpdateAssetData.SHA512))
                {
                    disposableFile.Dispose();
                    DownloadFailed("TODO");
                    return;
                }
            }
            catch (Exception ex)
            {
                disposableFile.Dispose();
                DownloadFailed(ex.ToString());
                return;
            }
            if (Form.IsClosing)
                return;
            Form.Invoke(() => Form.SetStage(Form.StageEnum.Output_Success));
            disposableFile.ShouldDisposeFileData = false;
            Program.Relaunch(temp_path);
        }
#endif

        private static void DownloadFailed(string reason)
        {
            if (Form.IsClosing)
                return;

            Form.Invoke(() => Form.SetStage(Form.StageEnum.Output_Failure));

            // Handle Failure
            Program.CreateMessageBox(reason, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxButtons.OK);

            Form.Invoke(Form.GetReleases);
        }
    }
}