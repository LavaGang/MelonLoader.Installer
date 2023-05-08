using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MelonLoader
{
    internal static class SteamHandler
    {

        /// <summary>
        /// Given a Steam AppID, returns the full file path to the executable file associated with the game.
        /// </summary>
        /// <param name="appid">The Steam AppID to look up.</param>
        /// <returns>The full file path to the executable file associated with the game, or null if not found.</returns>
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

        /// <summary>
        /// Reads the installation directory from a given app manifest file.
        /// </summary>
        /// <param name="appmanifestpath">The path to the app manifest file.</param>
        /// <returns>The installation directory, or null if it could not be found.</returns>
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

        /// <summary>
        /// Reads the library folders from the specified app manifest file and updates the steamapps path if necessary.
        /// </summary>
        /// <param name="appmanifestfilename">The name of the app manifest file.</param>
        /// <param name="steamappspath">The path to the steamapps folder.</param>
        /// <returns>The install directory for the specified app manifest file, or null if it could not be found.</returns>
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

        /// <summary>
        /// Scans for an executable file in the given directory and its subdirectories
        /// </summary>
        /// <param name="steamappspath">Path to the steamapps directory</param>
        /// <param name="installdir">Name of the game's installation directory</param>
        /// <param name="filepath">Output parameter that will contain the full path to the executable file if found</param>
        /// <returns>True if an executable file is found, false otherwise</returns>
        private static bool ScanForExe(string steamappspath, string installdir, out string filepath)
        {
            filepath = null;
            string installpath = Path.Combine(steamappspath, "common", installdir);
            if (!Directory.Exists(installpath))
                return false;
            string newfilepath = Path.Combine(installpath, (installdir + ".exe"));
            if (File.Exists(newfilepath))
            {
                filepath = newfilepath;
                return true;
            }
            newfilepath = Path.Combine(installpath, (installdir.Replace(" ", "") + ".exe"));
            if (File.Exists(newfilepath))
            {
                filepath = newfilepath;
                return true;
            }
            newfilepath = Path.Combine(installpath, installdir, (installdir + ".exe"));
            if (File.Exists(newfilepath))
            {
                filepath = newfilepath;
                return true;
            }
            newfilepath = Path.Combine(installpath, installdir, (installdir.Replace(" ", "") + ".exe"));
            if (File.Exists(newfilepath))
            {
                filepath = newfilepath;
                return true;
            }
            // Improve Exe Scanning
            return false;
        }

        /// <summary>
        /// Retrieves the installation path of the Steam client from the Windows Registry.
        /// </summary>
        /// <returns>A string representing the installation path of the Steam client, or null if the path could not be retrieved.</returns>
        private static string GetSteamInstallPath() => Registry.LocalMachine.OpenSubKey(!Environment.Is64BitOperatingSystem ? "SOFTWARE\\Valve\\Steam" : "SOFTWARE\\Wow6432Node\\Valve\\Steam")?.GetValue("InstallPath")?.ToString();
    }
}
