using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MelonLoader
{
    internal static class Program
    {
        internal static string[] Arguments;

        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
            Managers.Config.Load();
        }

        [STAThread]
        private static int Main(string[] args)
        {
            Arguments = args;
            AutoUpdateConfirmation();

#if !DEBUG
            if (Managers.SelfUpdate.Check_FileName())
                return 0;
#endif

            Managers.Form.Run();
            return 0;
        }

        private static void AutoUpdateConfirmation()
        {
            if (Managers.Config.AutoUpdateFirstLaunchCheck)
                return;
            DialogResult result = CreateMessageBoxInternal("Would you like the Installer to Auto-Update itself for you upon Launch?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            Managers.Config.AutoUpdate = result == DialogResult.Yes;
            Managers.Config.AutoUpdateFirstLaunchCheck = true;
        }

        internal static void Relaunch(string new_exe_path)
        {
            ProcessStartInfo procinfo = new ProcessStartInfo(new_exe_path);
            if ((Arguments != null) && (Arguments.Length > 0))
                procinfo.Arguments = string.Join(" ", Arguments.Where(s => !string.IsNullOrEmpty(s)).Select(it => ("\"" + Regex.Replace(it, @"(\\+)$", @"$1$1") + "\""))); ;
            Process.Start(procinfo);
            Process.GetCurrentProcess().Kill();
        }

        internal static DialogResult CreateMessageBoxInternal(string text, MessageBoxButtons buttons, MessageBoxIcon icon)
            => MessageBox.Show(text, $"MelonLoader Installer v{BuildInfo.Version}", buttons, icon);
        internal static void CreateMessageBox(string text, MessageBoxIcon icon, MessageBoxButtons buttons, bool new_thread = false)
        {
            if (Managers.Form.IsClosing)
                return;
            if (new_thread)
            {
                new Thread(() => CreateMessageBoxInternal(text, buttons, icon)).Start();
                return;
            }
            CreateMessageBoxInternal(text, buttons, icon);
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) => MessageBox.Show((e.ExceptionObject as Exception).ToString());
    }
}
