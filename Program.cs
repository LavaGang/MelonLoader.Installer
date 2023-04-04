using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MelonLoader
{
    internal static class Program
    {
        internal static MainForm mainForm = null;
        internal static WebClient webClient = null;
        internal static Version CurrentInstalledVersion = null;
        internal static bool Closing = false;
#if DEBUG
        internal static bool RunInstallerUpdateCheck = false;
#else
        internal static bool RunInstallerUpdateCheck = true;
#endif

        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
            webClient = new WebClient();
            webClient.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs info) => { SetCurrentPercentage(info.ProgressPercentage); SetTotalPercentage(info.ProgressPercentage / 2); };
            Config.Load();
        }

        [STAThread]
        private static int Main(string[] args)
        {
            if (FileNameCheck(args))
                return 0;
            //int commandlineval = 0;
            //if (CommandLine.Run(args, ref commandlineval))
            //    return commandlineval;
            mainForm = new MainForm();
            Application.Run(mainForm);
            return 0;
        }

        private static bool FileNameCheck(string[] args)
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
            ProcessStartInfo procinfo = new ProcessStartInfo(new_exe_path);
            if ((args != null) && (args.Length > 0))
                procinfo.Arguments = string.Join(" ", args.Where(s => !string.IsNullOrEmpty(s)).Select(it => ("\"" + Regex.Replace(it, @"(\\+)$", @"$1$1") + "\""))); ;
            Process.Start(procinfo);
            Process.GetCurrentProcess().Kill();
            return true;
        }

        internal static void SetCurrentOperation(string op)
        {
            mainForm.Invoke(new Action(() =>
            {
                mainForm.Output_Current_Operation.Text = op;
                mainForm.Output_Current_Operation.ForeColor = System.Drawing.SystemColors.Highlight;
                mainForm.Output_Current_Progress_Display.Style = MetroFramework.MetroColorStyle.Green;
                mainForm.Output_Total_Progress_Display.Style = MetroFramework.MetroColorStyle.Green;
                SetCurrentPercentage(0);
            }));
        }

        internal static void LogError(string msg)
        {
            TempFileCache.ClearCache();
            OperationError();

            try
            {
                string filePath = Directory.GetCurrentDirectory() + $@"\MLInstaller_{DateTime.Now:yy-M-dd_HH-mm-ss.fff}.log";
                File.WriteAllText(filePath, msg);
                if (Config.HighlightLogFileLocation)
                    Process.Start("explorer.exe", $"/select, {filePath}");
#if DEBUG
                FinishingMessageBox(msg, MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                FinishingMessageBox($"INTERNAL FAILURE! Please upload the log file \"{filePath}\" when requesting support.", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
            }
            catch (UnauthorizedAccessException)
            {
                FinishingMessageBox($"Couldn't create log file! Try running the Installer as Administrator or run the Installer from a different directory.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal static void OperationError()
        {
            mainForm.Invoke(new Action(() =>
            {
                mainForm.Output_Current_Operation.Text = "ERROR!";
                mainForm.Output_Current_Operation.ForeColor = System.Drawing.Color.Red;
                mainForm.Output_Current_Progress_Display.Style = MetroFramework.MetroColorStyle.Red;
                mainForm.Output_Total_Progress_Display.Style = MetroFramework.MetroColorStyle.Red;
            }));
        }

        internal static void OperationSuccess()
        {
            mainForm.Invoke(new Action(() =>
            {
                mainForm.Output_Current_Operation.Text = "SUCCESS!";
                mainForm.Output_Current_Operation.ForeColor = System.Drawing.Color.Lime;
                mainForm.Output_Current_Progress_Display.Value = 100;
                mainForm.Output_Current_Progress_Display.Style = MetroFramework.MetroColorStyle.Green;
                mainForm.Output_Total_Progress_Display.Style = MetroFramework.MetroColorStyle.Green;
                mainForm.Output_Current_Progress_Text.Text = mainForm.Output_Current_Progress_Display.Value.ToString();
                mainForm.Output_Total_Progress_Display.Value = mainForm.Output_Current_Progress_Display.Value;
                mainForm.Output_Total_Progress_Text.Text = mainForm.Output_Current_Progress_Display.Value.ToString();
            }));
        }

        internal static void SetCurrentPercentage(int percentage)
        {
            mainForm.Invoke(new Action(() =>
            {
                mainForm.Output_Current_Progress_Display.Value = percentage;
                mainForm.Output_Current_Progress_Text.Text = mainForm.Output_Current_Progress_Display.Value.ToString();
            }));
        }

        internal static void SetTotalPercentage(int percentage)
        {
            mainForm.Invoke(new Action(() =>
            {
                mainForm.Output_Total_Progress_Display.Value = percentage;
                mainForm.Output_Total_Progress_Text.Text = mainForm.Output_Total_Progress_Display.Value.ToString();
            }));
        }

        internal static void FinishingMessageBox(string msg, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            mainForm.Invoke(new Action(() =>
            {
                MessageBox.Show(msg, BuildInfo.Name, buttons, icon);
                if (icon != MessageBoxIcon.Error)
                {
                    if (Config.CloseAfterCompletion)
                    {
                        Process.GetCurrentProcess().Kill();
                        return;
                    }
                }
                mainForm.Automated_Install.Enabled = true;
                mainForm.CheckUnityGame();
                mainForm.PageManager.Controls.Clear();
                mainForm.PageManager.Controls.Add(mainForm.Tab_Automated);
                mainForm.PageManager.Controls.Add(mainForm.Tab_ManualZip);
                mainForm.PageManager.Controls.Add(mainForm.Tab_Settings);
                mainForm.PageManager.Cursor = Cursors.Hand;
                mainForm.PageManager.Select();
                SetTotalPercentage(0);
                OperationHandler.CurrentOperation = OperationHandler.Operations.NONE;
            }));
        }

        internal static void GetCurrentInstallVersion(string dirpath)
        {
            string folder_path = Path.Combine(dirpath, "MelonLoader");
            string legacy_file_path = Path.Combine(folder_path, "MelonLoader.ModHandler.dll");
            string file_path = Path.Combine(folder_path, "MelonLoader.dll");
            string new_file_path = Path.Combine(folder_path, "net35", "MelonLoader.dll");
            if (!File.Exists(legacy_file_path) && !(File.Exists(file_path) || File.Exists(new_file_path)) )
                return;

            string actual_file = File.Exists(legacy_file_path) ? legacy_file_path : ( File.Exists(new_file_path) ? new_file_path : file_path );

            string fileversion = null;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(actual_file);
            if (fileVersionInfo != null)
            {
                fileversion = fileVersionInfo.ProductVersion;
                if (string.IsNullOrEmpty(fileversion))
                    fileversion = fileVersionInfo.FileVersion;
            }
            if (string.IsNullOrEmpty(fileversion))
                fileversion = "0.0.0.0";
            CurrentInstalledVersion = new Version(fileversion);
        }

        internal static bool ValidateUnityGamePath(ref string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                return false;
            string file_extension = Path.GetExtension(filepath);
            if (string.IsNullOrEmpty(file_extension) || (!file_extension.Equals(".exe") && !file_extension.Equals(".lnk") && !file_extension.Equals(".url")))
                return false;
            if (file_extension.Equals(".lnk") || file_extension.Equals(".url"))
            {
                string newfilepath = GetFilePathFromShortcut(filepath);
                if (string.IsNullOrEmpty(newfilepath) || !newfilepath.EndsWith(".exe"))
                    return false;
                filepath = newfilepath;
            }

            // Verify Unity Game

            return true;
        }

        internal static bool ValidateZipPath(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                return false;
            string file_extension = Path.GetExtension(filepath);
            if (string.IsNullOrEmpty(file_extension) || !file_extension.Equals(".zip"))
                return false;
            return true;
        }

        private static string GetFilePathFromShortcut(string shortcut_path)
        {
            string shortcut_extension = Path.GetExtension(shortcut_path);
            if (shortcut_extension.Equals(".lnk"))
                return GetFilePathFromLNK(shortcut_path);
            else if (shortcut_extension.Equals(".url"))
                return GetFilePathFromURL(shortcut_path);
            return null;
        }
        private static string GetFilePathFromLNK(string shortcut_path) => ((IWshRuntimeLibrary.IWshShortcut)new IWshRuntimeLibrary.WshShell().CreateShortcut(shortcut_path)).TargetPath;
        private static string GetFilePathFromURL(string shortcut_path)
        {
            string[] file_lines = File.ReadAllLines(shortcut_path);
            if (file_lines.Length <= 0)
                return null;
            string urlstring = file_lines.First(x => (!string.IsNullOrEmpty(x) && x.StartsWith("URL=")));
            if (string.IsNullOrEmpty(urlstring))
                return null;
            urlstring = urlstring.Substring(4);
            if (string.IsNullOrEmpty(urlstring))
                return null;
            if (urlstring.StartsWith("steam://rungameid/"))
                return SteamHandler.GetFilePathFromAppId(urlstring.Substring(18));
            return null;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) => MessageBox.Show((e.ExceptionObject as Exception).ToString());
    }
}
