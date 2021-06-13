using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MelonLoader.Interfaces
{
    internal static class Steam
    {
        internal static bool IsSteamURL(string url)
            => (!string.IsNullOrEmpty(url) && url.StartsWith("steam://rungameid/"));

        internal static string GetAppIdFromURL(string url)
            => url.Substring(18);

        internal static string GetFilePathFromAppId(string appid)
        {
            if (string.IsNullOrEmpty(appid))
                return null;

            string steaminstallpath = GetSteamInstallPath();
            if (string.IsNullOrEmpty(steaminstallpath) || !Directory.Exists(steaminstallpath))
                return null;

            string steamappspath = Path.Combine(steaminstallpath, "steamapps");
            if (!Directory.Exists(steamappspath))
                return null;

            string appmanifestfilename = ("appmanifest_" + appid + ".acf");
            string appmanifestpath = Path.Combine(steamappspath, appmanifestfilename);
            string installdir = ReadAppManifestInstallDir(appmanifestpath);
            if (string.IsNullOrEmpty(installdir))
            {
                installdir = ReadLibraryFolders(appmanifestfilename, ref steamappspath);
                if (string.IsNullOrEmpty(installdir))
                    return null;
            }

            if (!ScanForExe(steamappspath, installdir, out string filepath))
                return null;

            return filepath;
        }

        private static string ReadAppManifestInstallDir(string appmanifestpath)
        {
            if (!File.Exists(appmanifestpath))
                return null;

            string[] file_lines = File.ReadAllLines(appmanifestpath);
            if (file_lines.Length <= 0)
                return null;

            string output = null;
            foreach (string line in file_lines)
            {
                Match match = new Regex(@"""installdir""\s+""(.+)""").Match(line);
                if (!match.Success)
                    continue;

                output = match.Groups[1].Value;
                break;
            }
            return output;
        }

        private static string ReadLibraryFolders(string appmanifestfilename, ref string steamappspath)
        {
            string libraryfoldersfilepath = Path.Combine(steamappspath, "libraryfolders.vdf");
            if (!File.Exists(libraryfoldersfilepath))
                return null;
            string[] file_lines = File.ReadAllLines(libraryfoldersfilepath);
            if (file_lines.Length <= 0)
                return null;
            string output = null;
            foreach (string line in file_lines)
            {
                Match match = new Regex(@"""\d+""\s+""(.+)""").Match(line);
                if (!match.Success)
                    continue;

                string steamappspath2 = Path.Combine(match.Groups[1].Value.Replace(":\\\\", ":\\"), "steamapps");
                if (!Directory.Exists(steamappspath2))
                    continue;

                string installdir = ReadAppManifestInstallDir(Path.Combine(steamappspath2, appmanifestfilename));
                if (string.IsNullOrEmpty(installdir))
                    continue;

                steamappspath = steamappspath2;
                output = installdir;
            }
            return output;
        }

        private static bool ScanForExe(string steamappspath, string installdir, out string filepath)
        {
            filepath = null;
            string installpath = Path.Combine(steamappspath, "common", installdir);
            if (!Directory.Exists(installpath))
                return false;

            string[] potential_directories = Directory.GetDirectories(installpath, "*_Data");
            if (potential_directories.Length <= 0)
            {
                potential_directories = Directory.GetDirectories(Path.Combine(installpath, installdir), "*_Data");
                if (potential_directories.Length <= 0)
                    return false;
            }

            string data_directory = potential_directories.First();
            string exe_name = data_directory.Substring(0, data_directory.Length - 5);
            string exe_path = Path.Combine(installpath, $"{exe_name}.exe");
            if (!File.Exists(exe_path))
            {
                exe_path = Path.Combine(Path.Combine(installpath, installdir), $"{exe_name}.exe");
                if (!File.Exists(exe_path))
                    return false;
            }

            filepath = exe_path;
            return true;
        }

        private static string GetSteamInstallPath()
            => Registry.LocalMachine.OpenSubKey(
                Environment.Is64BitOperatingSystem 
                ? "SOFTWARE\\Wow6432Node\\Valve\\Steam"
                : "SOFTWARE\\Valve\\Steam"
            )?.GetValue("InstallPath")?.ToString();
    }
}
