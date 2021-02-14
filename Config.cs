using System;
using System.IO;
using MelonLoader.Tomlyn;
using MelonLoader.Tomlyn.Model;
using MelonLoader.Tomlyn.Syntax;

namespace MelonLoader
{
    internal static class Config
    {
        internal static string Repo_API_Installer = "https://api.github.com/repos/LavaGang/MelonLoader.Installer/releases";
        internal static string Repo_API_MelonLoader = "https://api.github.com/repos/LavaGang/MelonLoader/releases";
        internal static string Download_MelonLoader = "https://github.com/LavaGang/MelonLoader/releases/download";

        internal static string Link_Discord = "https://discord.gg/2Wn3N2P";
        internal static string Link_Twitter = "https://twitter.com/lava_gang";
        internal static string Link_GitHub = "https://github.com/LavaGang";
        internal static string Link_Wiki = "https://melonwiki.xyz";
        internal static string Link_Update = "https://github.com/LavaGang/MelonLoader.Installer/releases/latest";

        private static string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MelonLoader.Installer.cfg");

        private static int _theme = 0;
        internal static int Theme { get => _theme; set { _theme = value; Save(); } }

        private static bool _autoupdateinstaller = true;
        internal static bool AutoUpdateInstaller { get => _autoupdateinstaller; set { _autoupdateinstaller = value; Save(); } }

        private static bool _closeaftercompletion = true;
        internal static bool CloseAfterCompletion { get => _closeaftercompletion; set { _closeaftercompletion = value; Save(); } }

        private static bool _showalphaprereleases = false;
        internal static bool ShowAlphaPreReleases { get => _showalphaprereleases; set { _showalphaprereleases = value; Save(); } }

        private static bool _rememberlastselectedgame = false;
        internal static bool RememberLastSelectedGame { get => _rememberlastselectedgame; set { _rememberlastselectedgame = value; Save(); } }

        private static string _lastselectedgamepath = null;
        internal static string LastSelectedGamePath { get => _lastselectedgamepath; set { _lastselectedgamepath = value; Save(); } }

        private static bool _highlightlogfilelocation = true;
        internal static bool HighlightLogFileLocation { get => _highlightlogfilelocation; set { _highlightlogfilelocation = value; Save(); } }

        internal static void Load()
        {
            if (!File.Exists(FilePath))
                return;
            string filestr = File.ReadAllText(FilePath);
            if (string.IsNullOrEmpty(filestr))
                return;
            DocumentSyntax doc = Toml.Parse(filestr);
            if ((doc == null) || doc.HasErrors)
                return;
            TomlTable tbl = doc.ToModel();
            if ((tbl.Count <= 0) || !tbl.ContainsKey("Installer"))
                return;
            TomlTable installertbl = (TomlTable)tbl["Installer"];
            if ((installertbl == null) || (installertbl.Count <= 0))
                return;
            if (installertbl.ContainsKey("Theme"))
                Int32.TryParse(installertbl["Theme"].ToString(), out _theme);
            if (installertbl.ContainsKey("AutoUpdateInstaller"))
                Boolean.TryParse(installertbl["AutoUpdateInstaller"].ToString(), out _autoupdateinstaller);
            if (installertbl.ContainsKey("CloseAfterCompletion"))
                Boolean.TryParse(installertbl["CloseAfterCompletion"].ToString(), out _closeaftercompletion);
            if (installertbl.ContainsKey("ShowAlphaPreReleases"))
                Boolean.TryParse(installertbl["ShowAlphaPreReleases"].ToString(), out _showalphaprereleases);
            if (installertbl.ContainsKey("RememberLastSelectedGame"))
                Boolean.TryParse(installertbl["RememberLastSelectedGame"].ToString(), out _rememberlastselectedgame);
            if (installertbl.ContainsKey("LastSelectedGamePath"))
                _lastselectedgamepath = installertbl["LastSelectedGamePath"].ToString();
            if (installertbl.ContainsKey("HighlightLogFileLocation"))
                Boolean.TryParse(installertbl["HighlightLogFileLocation"].ToString(), out _highlightlogfilelocation);

        }

        internal static void Save()
        {
            DocumentSyntax doc = new DocumentSyntax();
            TableSyntax tbl = new TableSyntax("Installer");
            tbl.Items.Add(new KeyValueSyntax("Theme", new IntegerValueSyntax(_theme)));
            tbl.Items.Add(new KeyValueSyntax("AutoUpdateInstaller", new BooleanValueSyntax(_autoupdateinstaller)));
            tbl.Items.Add(new KeyValueSyntax("CloseAfterCompletion", new BooleanValueSyntax(_closeaftercompletion)));
            tbl.Items.Add(new KeyValueSyntax("ShowAlphaPreReleases", new BooleanValueSyntax(_showalphaprereleases)));
            tbl.Items.Add(new KeyValueSyntax("RememberLastSelectedGame", new BooleanValueSyntax(_rememberlastselectedgame)));
            tbl.Items.Add(new KeyValueSyntax("LastSelectedGamePath", new StringValueSyntax(string.IsNullOrEmpty(_lastselectedgamepath) ? "" : _lastselectedgamepath)));
            tbl.Items.Add(new KeyValueSyntax("HighlightLogFileLocation", new BooleanValueSyntax(_highlightlogfilelocation)));
            doc.Tables.Add(tbl);
            File.WriteAllText(FilePath, doc.ToString());
        }
    }
}
