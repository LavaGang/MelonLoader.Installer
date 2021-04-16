using System.Drawing;
using MetroFramework;
using MetroFramework.Controls;
using MelonLoader.Properties;

namespace MelonLoader.Managers
{
    internal static class ThemeHandler
    {
        private class PalletData
        {
            internal MetroThemeStyle ThemeStyle;
            internal MetroColorStyle ColorStyle;
            internal MetroColorStyle ColorStyleAlt;
            internal Bitmap GitHubImage;
            internal Bitmap MLLogoImage = Resources.ML_Logo;
            internal Bitmap MLTextImage = Resources.ML_Text;
            internal Color InstallerUpdateNoticeForeColor;
            internal Color TabBackColor;
            internal Color CheckBoxForeColor;
            internal Color CheckBoxHoverForeColor;
            internal MetroColorStyle OutputProgressBarColor = MetroColorStyle.Green;
            internal Color NoticesColor = Color.FromKnownColor(KnownColor.Highlight);
        }

        private static PalletData[] PalletTbl = new PalletData[]
        {
            // Dark
            new PalletData()
            {
                ThemeStyle = MetroThemeStyle.Dark,
                ColorStyle = MetroColorStyle.Green,
                ColorStyleAlt = MetroColorStyle.Red,
                GitHubImage = Resources.GitHub_Dark,
                InstallerUpdateNoticeForeColor = Color.Green,
                TabBackColor = Color.Transparent,
                CheckBoxForeColor = Color.FromKnownColor(KnownColor.ControlDark),
                CheckBoxHoverForeColor = Color.White
            },

            // Light
            new PalletData()
            {
                ThemeStyle = MetroThemeStyle.Light,
                ColorStyle = MetroColorStyle.Red,
                ColorStyleAlt = MetroColorStyle.Green,
                GitHubImage = Resources.GitHub_Light,
                InstallerUpdateNoticeForeColor = Color.Red,
                TabBackColor = Color.White,
                CheckBoxForeColor = Color.FromArgb(153, 153, 153),
                CheckBoxHoverForeColor = Color.Black
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
                ThemeStyle = MetroThemeStyle.Dark,
                ColorStyle = MetroColorStyle.Yellow,
                ColorStyleAlt = MetroColorStyle.Yellow,
                GitHubImage = Resources.GitHub_Dark,
                MLLogoImage = Resources.LL_Logo,
                MLTextImage = Resources.LL_Text,
                InstallerUpdateNoticeForeColor = Color.YellowGreen,
                TabBackColor = Color.Transparent,
                CheckBoxForeColor = Color.FromKnownColor(KnownColor.ControlDark),
                CheckBoxHoverForeColor = Color.White,
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

            // MainForm
            FormHandler.mainForm.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Style = pallet.ColorStyle;

            // StyleManager
            FormHandler.mainForm.StyleManager.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.StyleManager.Style = pallet.ColorStyleAlt;

            // Images
            FormHandler.mainForm.Link_GitHub.Image = pallet.GitHubImage;
            FormHandler.mainForm.ML_Logo.Image = pallet.MLLogoImage;
            FormHandler.mainForm.ML_Text.Image = pallet.MLTextImage;

            // InstallerVersion
            FormHandler.mainForm.InstallerVersion.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.InstallerUpdateNotice.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.InstallerUpdateNotice.ForeColor = pallet.InstallerUpdateNoticeForeColor;

            // PageManager
            FormHandler.mainForm.PageManager.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.PageManager.Style = pallet.ColorStyleAlt;

            // Tabs
            FormHandler.mainForm.Tab_SelfUpdate.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Tab_SelfUpdate.BackColor = pallet.TabBackColor;
            FormHandler.mainForm.Tab_Automated.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Tab_Automated.BackColor = pallet.TabBackColor;
            FormHandler.mainForm.Tab_ManualZip.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Tab_ManualZip.BackColor = pallet.TabBackColor;
            FormHandler.mainForm.Tab_Settings.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Tab_Settings.BackColor = pallet.TabBackColor;
            FormHandler.mainForm.Tab_Output.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Tab_Output.BackColor = pallet.TabBackColor;

            // Divider
            FormHandler.mainForm.Divider.Theme = pallet.ThemeStyle;

            // SelfUpdate
            FormHandler.mainForm.SelfUpdate_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.SelfUpdate_Text.ForeColor = pallet.NoticesColor;

            // Automated
            FormHandler.mainForm.Automated_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Automated_Text.ForeColor = pallet.NoticesColor;
            FormHandler.mainForm.Automated_Text_Failure.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Automated_Retry.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Automated_Retry.Style = pallet.ColorStyle;

            // Settings
            FormHandler.mainForm.Settings_Theme_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Settings_Theme_Selection.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Settings_Theme_Selection.Style = pallet.ColorStyle;

            FormHandler.mainForm.Settings_AutoUpdateInstaller.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Settings_AutoUpdateInstaller.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(FormHandler.mainForm.Settings_AutoUpdateInstaller);
            FormHandler.mainForm.Settings_CloseAfterCompletion.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Settings_CloseAfterCompletion.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(FormHandler.mainForm.Settings_CloseAfterCompletion);
            FormHandler.mainForm.Settings_ShowAlphaPreReleases.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Settings_ShowAlphaPreReleases.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(FormHandler.mainForm.Settings_ShowAlphaPreReleases);
            FormHandler.mainForm.Settings_RememberLastSelectedGame.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Settings_RememberLastSelectedGame.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(FormHandler.mainForm.Settings_RememberLastSelectedGame);
            FormHandler.mainForm.Settings_HighlightLogFileLocation.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Settings_HighlightLogFileLocation.Style = pallet.ColorStyle;
            OnCheckBoxMouseLeave(FormHandler.mainForm.Settings_HighlightLogFileLocation);

            // Output
            FormHandler.mainForm.Divider.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Current_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Current_Operation.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Current_Progress_Display.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Current_Progress_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Current_Progress_Text_Label.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Total_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Total_Progress_Display.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Total_Progress_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Total_Progress_Text_Label.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Output_Current_Operation.ForeColor = pallet.NoticesColor;
            FormHandler.SetOutputProgressBarColor(pallet.OutputProgressBarColor);

#if DEBUG
            // Debug
            FormHandler.mainForm.Tab_Debug.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Tab_Debug.BackColor = pallet.TabBackColor;
            FormHandler.mainForm.Debug_LatestInstallerRelease.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_LatestInstallerRelease_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_LatestInstallerRelease_Text.ForeColor = pallet.NoticesColor;
            FormHandler.mainForm.Debug_AutomatedState.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_AutomatedState.Style = pallet.ColorStyle;
            FormHandler.mainForm.Debug_AutomatedState_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_AutomatedState_Text.ForeColor = pallet.NoticesColor;
            FormHandler.mainForm.Debug_OutputState.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_OutputState.Style = pallet.ColorStyle;
            FormHandler.mainForm.Debug_OutputState_Text.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_OutputState_Text.ForeColor = pallet.NoticesColor;
            FormHandler.mainForm.Debug_OutputTest.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_OutputTest.Style = pallet.ColorStyle;
            FormHandler.mainForm.Debug_OutputFailureTest.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_OutputFailureTest.Style = pallet.ColorStyle;
            FormHandler.mainForm.Debug_OutputSuccessTest.Theme = pallet.ThemeStyle;
            FormHandler.mainForm.Debug_OutputSuccessTest.Style = pallet.ColorStyle;
#endif
        }
    }
}