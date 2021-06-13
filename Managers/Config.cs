using System;
using System.IO;
using Tomlet;

namespace MelonLoader.Managers
{
    internal class Config
    {
        private static string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MelonLoader.Installer.cfg");
        private static FileValues Values = new FileValues();

        internal static int Theme { get => Values.Theme; set { Values.Theme = value; Save(); Managers.Theme.OnThemeChange(); } }
        internal static bool AutoUpdate { get => Values.AutoUpdate; set { Values.AutoUpdate = value; Save(); } }
        internal static bool AutoUpdateFirstLaunchCheck { get => Values.AutoUpdateFirstLaunchCheck; set { Values.AutoUpdateFirstLaunchCheck = value; Save(); } }
        internal static bool CloseAfterCompletion { get => Values.CloseAfterCompletion; set { Values.CloseAfterCompletion = value; Save(); } }
        internal static bool ShowALPHAPreReleases { get => Values.ShowALPHAPreReleases; set { Values.ShowALPHAPreReleases = value; Save(); } }
        internal static bool RememberLastSelectedGame { get => Values.RememberLastSelectedGame; set { Values.RememberLastSelectedGame = value; Save(); } }
        internal static string LastSelectedGamePath { get => Values.LastSelectedGamePath; set { Values.LastSelectedGamePath = value; Save(); } }
        internal static bool HighlightLogFileLocation { get => Values.HighlightLogFileLocation; set { Values.HighlightLogFileLocation = value; Save(); } }

        internal static void Load()
        {
            if (!File.Exists(FilePath))
                return;
            string filestr = File.ReadAllText(FilePath);
            if (string.IsNullOrEmpty(filestr))
                return;
            try { Values = TomletMain.To<FileValues>(filestr); } catch { }
        }

        internal static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, TomletMain.TomlStringFrom(Values));
            } catch { }
        }

        private class FileValues
        {
            internal int Theme = 0;
            internal bool AutoUpdate = true;
            internal bool AutoUpdateFirstLaunchCheck = false;
            internal bool CloseAfterCompletion = true;
            internal bool ShowALPHAPreReleases = false;
            internal bool RememberLastSelectedGame = false;
            internal string LastSelectedGamePath = null;
            internal bool HighlightLogFileLocation = true;
        }
    }
}
