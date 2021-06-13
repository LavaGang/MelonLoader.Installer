using System.IO;
using System.Linq;

namespace MelonLoader.Interfaces
{
    internal static class Shortcut
    {
        internal static bool IsFilePathShortcut(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                return false;

            string fileextension = Path.GetExtension(filepath);
            if (string.IsNullOrEmpty(fileextension))
                return false;

            switch (fileextension)
            {
                case ".lnk":
                case ".url":
                    return true;
                default:
                    return false;
            };
        }

        internal static string ParseToFilePath(string shortcut_path)
        {
            if (string.IsNullOrEmpty(shortcut_path))
                return null;

            string shortcut_extension = Path.GetExtension(shortcut_path);
            if (string.IsNullOrEmpty(shortcut_extension))
                return null;

            return shortcut_extension.ToLower() switch
            {
                ".lnk" => ParseLNK(shortcut_path),
                ".url" => ParseURL(shortcut_path),
                _ => null
            };
        }

        private static string ParseLNK(string shortcut_path)
            => ((IWshRuntimeLibrary.IWshShortcut)new IWshRuntimeLibrary.WshShell().CreateShortcut(shortcut_path)).TargetPath;

        private static string ParseURL(string shortcut_path)
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

            if (Steam.IsSteamURL(urlstring))
                return Steam.GetFilePathFromAppId(Steam.GetAppIdFromURL(urlstring));

            return null;
        }
    }
}