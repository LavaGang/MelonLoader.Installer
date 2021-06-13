using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MetroFramework.Controls;

namespace MelonLoader.Managers
{
    internal static class Form
    {
        internal static MainForm mainForm;
        internal static bool IsClosing = false;
        internal static Interfaces.GitHub ReleasesAPI = new Interfaces.GitHub(URLs.Repositories.MelonLoader);

        internal enum StageEnum
        {
            None,
#if DEBUG
            Debug,
#endif
            SelfUpdate,
            Main,
            Automated_Failure,
            Automated_Success,
            Output,
            Output_Success,
            Output_Failure,
        }

        internal static void Run()
            => Application.Run(mainForm = new MainForm());

        internal static void Invoke(Action act) => mainForm.Invoke(act);
        internal static void SetOutputCurrentPercentage(int percentage) => mainForm.Output_Current_Progress_Text.Text = (mainForm.Output_Current_Progress_Display.Value = percentage).ToString();
        internal static void SetOutputTotalPercentage(int percentage) => mainForm.Output_Total_Progress_Text.Text = (mainForm.Output_Total_Progress_Display.Value = percentage).ToString();
        internal static void SetOutputCurrentOperation(string text, Color color) { mainForm.Output_Current_Operation.Text = text; mainForm.Output_Current_Operation.ForeColor = color; }
        internal static void SetOutputProgressBarColor(MetroFramework.MetroColorStyle color) => mainForm.Output_Current_Progress_Display.Style = mainForm.Output_Total_Progress_Display.Style = color;

        internal static void OnLoad()
        {
            mainForm.InstallerVersion.Text = $"Installer v{BuildInfo.Version}";
            mainForm.Settings_Theme_Selection.SelectedIndex = Config.Theme;
            mainForm.Settings_AutoUpdateInstaller.Checked = Config.AutoUpdate;
            mainForm.Settings_CloseAfterCompletion.Checked = Config.CloseAfterCompletion;
            mainForm.Settings_ShowALPHAPreReleases.Checked = Config.ShowALPHAPreReleases;
            mainForm.Settings_RememberLastSelectedGame.Checked = Config.RememberLastSelectedGame;
            mainForm.Settings_HighlightLogFileLocation.Checked = Config.HighlightLogFileLocation;
#if DEBUG
            SetStage(StageEnum.Debug);
#endif
            SetStage(StageEnum.SelfUpdate);
            SelfUpdate.Check_Repo();
        }

        internal static void OnClose()
        {
            IsClosing = true;
            Interfaces.WebRequest.CancelAsync();
            Thread.Sleep(100);
            Config.Save();
            Interfaces.DisposableFile.Cleanup();
        }

        internal static void OnLinkClick(object linkObject)
        {
            if (linkObject == mainForm.Link_Discord)
                Process.Start(URLs.ExternalLinks.Discord);
            else if (linkObject == mainForm.Link_GitHub)
                Process.Start(URLs.ExternalLinks.GitHub);
            else if (linkObject == mainForm.Link_Twitter)
                Process.Start(URLs.ExternalLinks.Twitter);
            else if (linkObject == mainForm.Link_Wiki)
                Process.Start(URLs.ExternalLinks.Wiki);
            else if (linkObject == mainForm.InstallerVersion)
                Process.Start(URLs.ExternalLinks.Installer);
            else if (linkObject == mainForm.InstallerUpdateNotice)
                Process.Start($"{URLs.ExternalLinks.Installer}/releases/latest");
            else if (linkObject == mainForm.LavaGangLogo)
                Process.Start(URLs.ExternalLinks.LavaGang);
        }

        internal static void OnComboBoxSelectedIndexChange(MetroComboBox comboBox)
        {
#if DEBUG
            if (comboBox == mainForm.Debug_AutomatedState)
                Debug.SetAutomatedState(comboBox.SelectedIndex);
            else if (comboBox == mainForm.Debug_OutputState)
                Debug.SetOutputState(comboBox.SelectedIndex);
#endif

            if (comboBox == mainForm.Settings_Theme_Selection)
                Config.Theme = comboBox.SelectedIndex;
        }

        internal static void OnCheckBoxCheckedChange(MetroCheckBox checkBox)
        {
            if (checkBox == mainForm.Settings_AutoUpdateInstaller)
                Config.AutoUpdate = checkBox.Checked;
            else if (checkBox == mainForm.Settings_CloseAfterCompletion)
                Config.CloseAfterCompletion = checkBox.Checked;
            else if (checkBox == mainForm.Settings_HighlightLogFileLocation)
                Config.HighlightLogFileLocation = checkBox.Checked;
            else if (checkBox == mainForm.Settings_RememberLastSelectedGame)
                Config.RememberLastSelectedGame = checkBox.Checked;
            else if (checkBox == mainForm.Settings_ShowALPHAPreReleases)
                Config.ShowALPHAPreReleases = checkBox.Checked;
        }

        internal static void OnButtonClick(MetroButton button)
        {
#if DEBUG
            if (button == mainForm.Debug_OutputTest)
                Debug.RunOutputTest(50, 4);
            else if (button == mainForm.Debug_OutputFailureTest)
                Debug.RunOutputTest(50, 4, 3);
            else if (button == mainForm.Debug_OutputSuccessTest)
                Debug.RunOutputTest(50, 4, testsuccess: true);
#endif

            if ((button == mainForm.Automated_Retry)
                || (button == mainForm.Settings_RefreshReleases))
                GetReleases();
            else if (button == mainForm.Automated_UnityGame_Select)
                Interfaces.UnityGame.OpenSelectionPrompt();
        }

        internal static void GetReleases()
        {
            SetStage(StageEnum.Main);
            new Thread(() =>
            {
                ReleasesAPI.Refresh();
                Invoke(() =>
                {
                    if (ReleasesAPI.ReleasesTbl.Count <= 0)
                    {
                        SetStage(StageEnum.Automated_Failure);
                        return;
                    }
                    SetStage(StageEnum.Automated_Success);
                    UpdateReleasesList();
                });
            }).Start();
        }

        internal static void UpdateReleasesList()
        {
            /*
            Interfaces.GitHub.ReleaseData[] releaseData = ReleasesAPI.ReleasesTbl.Where(x => Config.ShowALPHAPreReleases ? true : !x.IsPreRelease).ToArray();
            if (releaseData.Length <= 0)
            {
                SetStage(StageEnum.Automated_Failure);
                return;
            }
            */

            // Cache Selected Release
            // Clear Current Releases List
            // Add Releases to List
            // Check and/or Set Selected Release Back
            // SetStage
        }

        internal static void SetStage(StageEnum stage
#if DEBUG
            , bool set_debug_index = true
#endif
            )
        {
            switch (stage)
            {
#if DEBUG
                case StageEnum.Debug:
                    Debug.SetFormStage();
                    goto select;
#endif

                case StageEnum.Main:
                    mainForm.Settings_RefreshReleases.Enabled = false;
                    mainForm.Automated_Text.Visible = true;
                    mainForm.Automated_Text_Failure.Visible = false;
                    mainForm.Automated_Retry.Visible = false;
                    mainForm.Automated_Install.Visible = false;
                    mainForm.Automated_Uninstall.Visible = false;
                    mainForm.Automated_UnityGame_Text.Visible = false;
                    mainForm.Automated_UnityGame_Select.Visible = false;
                    mainForm.Automated_Divider.Visible = false;
#if DEBUG
                    if (set_debug_index)
                        mainForm.Debug_AutomatedState.SelectedIndex = 0;
                    goto default;
#else
                    goto main;
#endif

                case StageEnum.Automated_Failure:
                    mainForm.Settings_RefreshReleases.Enabled = true;
                    mainForm.Automated_Text.Visible = false;
                    mainForm.Automated_Text_Failure.Visible = true;
                    mainForm.Automated_Retry.Visible = true;
#if DEBUG
                    if (set_debug_index)
                        mainForm.Debug_AutomatedState.SelectedIndex = 1;
#endif
                    goto default;

                case StageEnum.Automated_Success:
                    mainForm.Settings_RefreshReleases.Enabled = true;
                    mainForm.Automated_Text.Visible = false;
                    mainForm.Automated_Text_Failure.Visible = false;
                    mainForm.Automated_Retry.Visible = false;
                    mainForm.Automated_Install.Visible = true;
                    mainForm.Automated_Uninstall.Visible = true;
                    mainForm.Automated_UnityGame_Text.Visible = true;
                    mainForm.Automated_UnityGame_Select.Visible = true;
                    mainForm.Automated_Divider.Visible = true;
#if DEBUG
                    if (set_debug_index)
                        mainForm.Debug_AutomatedState.SelectedIndex = 2;
#endif
                    goto default;

                case StageEnum.Output:
                    SetOutputCurrentOperation("Current Operation", Theme.GetOutputOperationColor());
                    SetOutputProgressBarColor(Theme.GetOutputProgressBarColor());
                    SetOutputCurrentPercentage(0);
                    SetOutputTotalPercentage(0);
#if DEBUG
                    if (set_debug_index)
                        mainForm.Debug_OutputState.SelectedIndex = 0;
#endif
                    goto output;

                case StageEnum.Output_Failure:
                    SetOutputCurrentOperation("ERROR!", Color.Red);
                    SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Red);
#if DEBUG
                    if (set_debug_index)
                        mainForm.Debug_OutputState.SelectedIndex = 1;
#endif
                    goto default;

                case StageEnum.Output_Success:
                    SetOutputCurrentOperation("SUCCESS!", Color.Green);
                    SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Green);
                    SetOutputCurrentPercentage(100);
                    SetOutputTotalPercentage(100);
#if DEBUG
                    if (set_debug_index)
                        mainForm.Debug_OutputState.SelectedIndex = 2;
#endif
                    goto default;

                case StageEnum.SelfUpdate:
#if DEBUG
                    goto default;
#else
                    mainForm.PageManager.Controls.Clear();
                    mainForm.PageManager.Controls.Add(mainForm.Tab_SelfUpdate);
                    goto select;

                    main:
                    mainForm.PageManager.Controls.Clear();
                    mainForm.PageManager.Controls.Add(mainForm.Tab_Automated);
                    mainForm.PageManager.Controls.Add(mainForm.Tab_ManualZip);
                    mainForm.PageManager.Controls.Add(mainForm.Tab_Settings);
                    goto select;
#endif

                    output:
                    mainForm.PageManager.Controls.Clear();
                    mainForm.PageManager.Controls.Add(mainForm.Tab_Output);
                    goto select;

                    select:
                    mainForm.PageManager.SelectedIndex = 0;
                    goto default;

                default:
                    break;
            }
        }
    }
}
