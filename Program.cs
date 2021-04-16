using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using MelonLoader.Managers;

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
            Config.Load();
        }

        [STAThread]
        private static int Main(string[] args)
        {
            Arguments = args;
#if !DEBUG
            if (SelfUpdate.Check_FileName())
                return 0;
#endif
            FormHandler.Run();
            return 0;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) => MessageBox.Show((e.ExceptionObject as Exception).ToString());
        internal static void EndItAll() => Process.GetCurrentProcess().Kill();
    }
}
