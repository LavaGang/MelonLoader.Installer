using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MelonLoader
{
    internal static class CommandLine
    {
        internal static bool IsCMD = false;
        internal static bool IsSilent = false;
        internal static int CmdMode = 0;
        internal static string ExePath = null;
        internal static string ZipPath = null;
        internal static string RequestedVersion = null;
        internal static bool AutoDetectArch = true;
        internal static bool Requested32Bit = false;

        internal static bool Run(string[] args, ref int returnval)
        {
            if (args.Length <= 0)
                return false;
            if (string.IsNullOrEmpty(args[0]))
                return false;
            ExePath = string.Copy(args[0]);
            if (args.Length == 1)
                return false;
            bool breakforhelp = false;
            foreach (string arg in args)
            {
                if (string.IsNullOrEmpty(arg))
                    continue;
                switch (arg)
                {
                    case "/silent":
                        IsSilent = true;
                        break;
                    case "/i":
                        if (CmdMode == 0)
                            CmdMode = 1;
                        break;
                    case "/u":
                        if (CmdMode == 0)
                            CmdMode = 2;
                        break;
                    case "/x86":
                        Requested32Bit = true;
                        break;
                    case "/zip":
                        // Grab Zip Path
                        break;
                    case "/version":
                        // Grab Requested Version
                        break;
                    default:
                        breakforhelp = true;
                        break;
                }
                if (breakforhelp)
                    break;
            }
            if (breakforhelp)
                PrintHelp();
            else
                switch(CmdMode)
                {
                    case 1:
                        Install(ref returnval);
                        break;
                    case 2:
                        Uninstall(ref returnval);
                        break;
                    default:
                        PrintHelp();
                        break;
                }
            return true;
        }

        private static void PrintHelp()
        {

        }

        private static void Install(ref int returnval)
        {
            if (!Program.ValidateUnityGamePath(ref ExePath))
            {
                // Output Error
                return;
            }
            Program.GetCurrentInstallVersion(Path.GetDirectoryName(ExePath));
            if (!string.IsNullOrEmpty(ZipPath))
            {
                InstallFromZip(ref returnval);
                return;
            }
            Releases.RequestLists();
            if (Releases.All.Count <= 0)
            {
                // Output Error
                return;
            }

            // Pull Latest Version

            string selected_version = "v0.0.0.0";
            if (Program.CurrentInstalledVersion == null)
                OperationHandler.CurrentOperation = OperationHandler.Operations.INSTALL;
            else
            {
                Version selected_ver = new Version(selected_version);
                int compare_ver = selected_ver.CompareTo(Program.CurrentInstalledVersion);
                if (compare_ver < 0)
                    OperationHandler.CurrentOperation = OperationHandler.Operations.DOWNGRADE;
                else if (compare_ver > 0)
                    OperationHandler.CurrentOperation = OperationHandler.Operations.UPDATE;
                else
                    OperationHandler.CurrentOperation = OperationHandler.Operations.REINSTALL;
            }
            OperationHandler.Automated_Install(Path.GetDirectoryName(ExePath), selected_version, Requested32Bit, (selected_version.StartsWith("v0.2") || selected_version.StartsWith("v0.1")));
        }

        private static void InstallFromZip(ref int returnval)
        {
            if (!Program.ValidateZipPath(ZipPath))
            {
                // Output Error
                return;
            }
            OperationHandler.ManualZip_Install(ZipPath, Path.GetDirectoryName(ExePath));
        }


        private static void Uninstall(ref int returnval)
        {
            if (!Program.ValidateUnityGamePath(ref ExePath))
            {
                // Output Error
                return;
            }
            string folderpath = Path.GetDirectoryName(ExePath);
            Program.GetCurrentInstallVersion(folderpath);
            if (Program.CurrentInstalledVersion == null)
            {
                // Output Error
                return;
            }
            OperationHandler.CurrentOperation = OperationHandler.Operations.UNINSTALL;
            OperationHandler.Uninstall(folderpath);
        }
    }
}
