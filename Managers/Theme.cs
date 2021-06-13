using System.Drawing;
using MetroFramework;
using MetroFramework.Controls;
using MelonLoader.Properties;

namespace MelonLoader.Managers
{
    internal static class Theme
    {
        internal class PalletData
        {
            internal MetroThemeStyle ThemeStyle = MetroThemeStyle.Dark;
            internal MetroColorStyle ColorStyle = MetroColorStyle.Green;
            internal MetroColorStyle ColorStyleAlt = MetroColorStyle.Red;
            internal Bitmap GitHubImage = Resources.GitHub_Dark;
            internal Bitmap MLLogoImage = Resources.ML_Logo;
            internal Bitmap MLTextImage = Resources.ML_Text;
            internal Bitmap LGLogoImage = Resources.LavaGang_Dark;
            internal Color InstallerUpdateNoticeForeColor = Color.Green;
            internal Color TabBackColor = Color.Transparent;
            internal Color CheckBoxForeColor = Color.FromKnownColor(KnownColor.ControlDark);
            internal Color CheckBoxHoverForeColor = Color.White;
            internal MetroColorStyle OutputProgressBarColor = MetroColorStyle.Green;
            internal Color NoticesColor = Color.FromKnownColor(KnownColor.Highlight);
        }

        private static PalletData[] PalletTbl = new PalletData[]
        {
            // Dark
            new PalletData(),

            // Light
            new PalletData()
            {
                ThemeStyle = MetroThemeStyle.Light,
                ColorStyle = MetroColorStyle.Red,
                ColorStyleAlt = MetroColorStyle.Green,
                GitHubImage = Resources.GitHub_Light,
                LGLogoImage = Resources.LavaGang_Light,
                InstallerUpdateNoticeForeColor = Color.Red,
                TabBackColor = Color.White,
                CheckBoxForeColor = Color.FromArgb(153, 153, 153),
                CheckBoxHoverForeColor = Color.Black,
            },

            // LightLemon

            new PalletData()
            {
                ThemeStyle = MetroThemeStyle.Light,
                ColorStyle = MetroColorStyle.Yellow,
                ColorStyleAlt = MetroColorStyle.Yellow,
                GitHubImage = Resources.GitHub_Light,
                MLLogoImage = Resources.LL_Logo,
                MLTextImage = Resources.LL_Text,
                LGLogoImage = Resources.LavaGang_Light,
                InstallerUpdateNoticeForeColor = Color.YellowGreen,
                TabBackColor = Color.White,
                CheckBoxForeColor = Color.FromArgb(153, 153, 153),
                CheckBoxHoverForeColor = Color.Black,
                OutputProgressBarColor = MetroColorStyle.Yellow,
                NoticesColor = Color.YellowGreen
            },

            // DarkLemon
            new PalletData()
            {
                ColorStyle = MetroColorStyle.Yellow,
                ColorStyleAlt = MetroColorStyle.Yellow,
                MLLogoImage = Resources.LL_Logo,
                MLTextImage = Resources.LL_Text,
                InstallerUpdateNoticeForeColor = Color.YellowGreen,
                OutputProgressBarColor = MetroColorStyle.Yellow,
                NoticesColor = Color.YellowGreen
            },
        };

        internal static MetroColorStyle GetOutputProgressBarColor() => PalletTbl[Config.Theme].OutputProgressBarColor;
        internal static Color GetOutputOperationColor() => PalletTbl[Config.Theme].NoticesColor;

        internal static void OnCheckBoxMouseEnter(MetroCheckBox checkBox) { checkBox.CustomForeColor = true; checkBox.ForeColor = PalletTbl[Config.Theme].CheckBoxHoverForeColor; }
        internal static void OnCheckBoxMouseLeave(MetroCheckBox checkBox) { checkBox.CustomForeColor = true; checkBox.ForeColor = PalletTbl[Config.Theme].CheckBoxForeColor; }

        internal static void OnThemeChange()
        {
            PalletData pallet = PalletTbl[Config.Theme];

#if DEBUG
            Debug.OnThemeChange(pallet);
#endif

            // MainForm
            Form.mainForm.Theme = pallet.ThemeStyle;
            Form.mainForm.Style = pallet.ColorStyle;

            // StyleManager
            Form.mainForm.StyleManager.Theme = pallet.ThemeStyle;
            Form.mainForm.StyleManager.Style = pallet.ColorStyleAlt;

            // Images
            Form.mainForm.Link_GitHub.Image = pallet.GitHubImage;
            Form.mainForm.ML_Logo.Image = pallet.MLLogoImage;
            Form.mainForm.ML_Text.Image = pallet.MLTextImage;
            Form.mainForm.LavaGangLogo.Image = pallet.LGLogoImage;

            // InstallerVersion
            Form.mainForm.InstallerVersion.Theme = pallet.ThemeStyle;
            Form.mainForm.InstallerUpdateNotice.Theme = pallet.ThemeStyle;
            Form.mainForm.InstallerUpdateNotice.ForeColor = pallet.InstallerUpdateNoticeForeColor;

            // PageManager
            Form.mainForm.PageManager.Theme = pallet.ThemeStyle;
            Form.mainForm.PageManager.Style = pallet.ColorStyleAlt;

            // Tabs
            Form.mainForm.Tab_SelfUpdate.Theme = pallet.ThemeStyle;
            Form.mainForm.Tab_SelfUpdate.BackColor = pallet.TabBackColor;
            Form.mainForm.Tab_Automated.Theme = pallet.ThemeStyle;
            Form.mainForm.Tab_Automated.BackColor = pallet.TabBackColor;
            Form.mainForm.Tab_ManualZip.Theme = pallet.ThemeStyle;
            Form.mainForm.Tab_ManualZip.BackColor = pallet.TabBackColor;
            Form.mainForm.Tab_Settings.Theme = pallet.ThemeStyle;
            Form.mainForm.Tab_Settings.BackColor = pallet.TabBackColor;
            Form.mainForm.Tab_Output.Theme = pallet.ThemeStyle;
            Form.mainForm.Tab_Output.BackColor = pallet.TabBackColor;

            // SelfUpdate
            Form.mainForm.SelfUpdate_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.SelfUpdate_Text.ForeColor = pallet.NoticesColor;

            // Automated
            Form.mainForm.Automated_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Automated_Text.ForeColor = pallet.NoticesColor;
            Form.mainForm.Automated_Text_Failure.Theme = pallet.ThemeStyle;
            Form.mainForm.Automated_Retry.Theme = pallet.ThemeStyle;
            Form.mainForm.Automated_Retry.Style = pallet.ColorStyle;
            Form.mainForm.Automated_Install.Theme = pallet.ThemeStyle;
            Form.mainForm.Automated_Install.Style = pallet.ColorStyle;
            Form.mainForm.Automated_Uninstall.Theme = pallet.ThemeStyle;
            Form.mainForm.Automated_Uninstall.Style = pallet.ColorStyle;
            Form.mainForm.Automated_Divider.Theme = pallet.ThemeStyle;

            // ManualZip
            Form.mainForm.ManualZip_Divider.Theme = pallet.ThemeStyle;

            // Settings
            Form.mainForm.Settings_Theme_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_Theme_Selection.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_Theme_Selection.Style = pallet.ColorStyle;
            Form.mainForm.Settings_RefreshReleases.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_RefreshReleases.Style = pallet.ColorStyle;

            Form.mainForm.Settings_AutoUpdateInstaller.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_AutoUpdateInstaller.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(Form.mainForm.Settings_AutoUpdateInstaller);
            Form.mainForm.Settings_CloseAfterCompletion.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_CloseAfterCompletion.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(Form.mainForm.Settings_CloseAfterCompletion);
            Form.mainForm.Settings_ShowALPHAPreReleases.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_ShowALPHAPreReleases.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(Form.mainForm.Settings_ShowALPHAPreReleases);
            Form.mainForm.Settings_RememberLastSelectedGame.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_RememberLastSelectedGame.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(Form.mainForm.Settings_RememberLastSelectedGame);
            Form.mainForm.Settings_HighlightLogFileLocation.Theme = pallet.ThemeStyle;
            Form.mainForm.Settings_HighlightLogFileLocation.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(Form.mainForm.Settings_HighlightLogFileLocation);

            // Output
            Form.mainForm.Output_Divider.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Current_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Current_Operation.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Current_Progress_Display.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Current_Progress_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Current_Progress_Text_Label.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Total_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Total_Progress_Display.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Total_Progress_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Total_Progress_Text_Label.Theme = pallet.ThemeStyle;
            Form.mainForm.Output_Current_Operation.ForeColor = pallet.NoticesColor;
            Form.SetOutputProgressBarColor(pallet.OutputProgressBarColor);
        }
    }
}