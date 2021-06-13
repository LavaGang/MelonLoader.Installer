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
#if !DEBUG
            if (Managers.SelfUpdate.Check_FileName())
                return 0;
#endif
            Managers.Form.Run();
            return 0;
        }

        internal static void Relaunch(string new_exe_path)
        {
            ProcessStartInfo procinfo = new ProcessStartInfo(new_exe_path);
            if ((Arguments != null) && (Arguments.Length > 0))
                procinfo.Arguments = string.Join(" ", Arguments.Where(s => !string.IsNullOrEmpty(s)).Select(it => ("\"" + Regex.Replace(it, @"(\\+)$", @"$1$1") + "\""))); ;
            Process.Start(procinfo);
            Process.GetCurrentProcess().Kill();
        }

        private static void InternalCreateMessageBox(string text, MessageBoxButtons buttons, MessageBoxIcon icon)
            => MessageBox.Show(text, $"MelonLoader {Managers.Form.mainForm.InstallerVersion.Text}", buttons, icon);
        internal static void CreateMessageBox(string text, MessageBoxIcon icon, MessageBoxButtons buttons, bool new_thread = false)
        {
            if (Managers.Form.IsClosing)
                return;
            if (new_thread)
            {
                new Thread(() => InternalCreateMessageBox(text, buttons, icon)).Start();
                return;
            }
            InternalCreateMessageBox(text, buttons, icon);
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) => MessageBox.Show((e.ExceptionObject as Exception).ToString());
    }
}
