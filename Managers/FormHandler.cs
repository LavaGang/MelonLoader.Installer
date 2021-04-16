using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace MelonLoader.Managers
{
    internal static class FormHandler
    {
        internal static MainForm mainForm;
        internal static bool IsClosing = false;
        internal static GitHubAPI ReleasesAPI = new GitHubAPI(URLs.Repositories.MelonLoader);
        internal static bool ShowDividerOnAutomated = false;

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

        internal static void Invoke(Action act) => mainForm.Invoke(act);
        internal static void ShowInstallerUpdateNotice() => mainForm.InstallerUpdateNotice.Visible = true;
        internal static void SetOutputCurrentPercentage(int percentage) => mainForm.Output_Current_Progress_Text.Text = (mainForm.Output_Current_Progress_Display.Value = percentage).ToString();
        internal static void SetOutputTotalPercentage(int percentage) => mainForm.Output_Total_Progress_Text.Text = (mainForm.Output_Total_Progress_Display.Value = percentage).ToString();
        internal static void SetOutputCurrentOperation(string text, Color color) { mainForm.Output_Current_Operation.Text = text; mainForm.Output_Current_Operation.ForeColor = color; }
        internal static void SetOutputProgressBarColor(MetroFramework.MetroColorStyle color) => mainForm.Output_Current_Progress_Display.Style = mainForm.Output_Total_Progress_Display.Style = color;

        internal static void Run()
        {
            mainForm = new MainForm();
            Application.Run(mainForm);
        }

        internal static void OnClose()
        {
            IsClosing = true;
            WebClientInterface.CancelAsync();
            Thread.Sleep(100);
            TempFileCache.ClearCache();
            Config.Save();
        }

        internal static void OnTabChange(TabPage page)
        {
            mainForm.Divider.Visible = (page == mainForm.Tab_Automated)
            ? ShowDividerOnAutomated
            : ((page != mainForm.Tab_SelfUpdate)
                && (page != mainForm.Tab_Settings)
#if DEBUG
                && (page != mainForm.Tab_Debug)
#endif
            );
            mainForm.Divider.Location = ((page == mainForm.Tab_Automated)
                    || (page == mainForm.Tab_ManualZip))
                        ? new Point(29, 347) 
                        : new Point(29, 320);
        }

        internal static void OnLoad()
        {
            mainForm.InstallerVersion.Text = $"Installer v{BuildInfo.Version}";
            mainForm.Settings_Theme_Selection.SelectedIndex = Config.Theme;
            mainForm.Settings_AutoUpdateInstaller.Checked = Config.AutoUpdateInstaller;
            mainForm.Settings_CloseAfterCompletion.Checked = Config.CloseAfterCompletion;
            mainForm.Settings_ShowAlphaPreReleases.Checked = Config.ShowAlphaPreReleases;
            mainForm.Settings_RememberLastSelectedGame.Checked = Config.RememberLastSelectedGame;
            mainForm.Settings_HighlightLogFileLocation.Checked = Config.HighlightLogFileLocation;
            mainForm.Divider.BringToFront();
#if DEBUG
            SetStage(StageEnum.Debug);
#endif
            SetStage(StageEnum.SelfUpdate);
            SelfUpdate.Check_Repo();
        }

        internal static void SpawnMessageBox(string text, MessageBoxIcon icon, MessageBoxButtons buttons, bool new_thread = false)
        {
            if (IsClosing)
                return;
            if (new_thread)
            {
                new Thread(() => MessageBox.Show(text, $"MelonLoader {mainForm.InstallerVersion.Text}", buttons, icon)).Start();
                return;
            }
            MessageBox.Show(text, $"MelonLoader {mainForm.InstallerVersion.Text}", buttons, icon);
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

                    GitHubAPI.ReleaseData[] releaseData = ReleasesAPI.ReleasesTbl.Where(x => Config.ShowAlphaPreReleases ? true : !x.IsPreRelease).ToArray();
                    if (releaseData.Length <= 0)
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
            // Cache Selected Release
            // Clear Current Releases List
            // Add Releases to List
            // Check and/or Set Selected Release Back
            // SetStage
        }

        internal static void SetStage(StageEnum stage)
        {
            switch (stage)
            {
#if DEBUG
                case StageEnum.Debug:
                    mainForm.PageManager.Controls.Clear();
                    mainForm.PageManager.Controls.Add(mainForm.Tab_Debug);
                    mainForm.PageManager.Controls.Add(mainForm.Tab_Automated);
                    mainForm.PageManager.Controls.Add(mainForm.Tab_ManualZip);
                    mainForm.PageManager.Controls.Add(mainForm.Tab_Settings);
                    mainForm.PageManager.Controls.Add(mainForm.Tab_Output);
                    mainForm.Debug_AutomatedState.SelectedIndex = 0;
                    mainForm.Debug_OutputState.SelectedIndex = 0;
                    goto select;
#endif

                case StageEnum.Main:
                    ShowDividerOnAutomated = false;
                    mainForm.Automated_Text.Visible = true;
                    mainForm.Automated_Text_Failure.Visible = false;
                    mainForm.Automated_Retry.Visible = false;
#if DEBUG
                    mainForm.Debug_AutomatedState.SelectedIndex = 0;
                    goto default;
#else
                    goto main;
#endif

                case StageEnum.Automated_Failure:
                    ShowDividerOnAutomated = false;
                    if (mainForm.PageManager.SelectedTab == mainForm.Tab_Automated)
                        mainForm.Divider.Visible = true;
                    mainForm.Automated_Text.Visible = false;
                    mainForm.Automated_Text_Failure.Visible = true;
                    mainForm.Automated_Retry.Visible = true;
#if DEBUG
                    mainForm.Debug_AutomatedState.SelectedIndex = 1;
#endif
                    goto default;

                case StageEnum.Automated_Success:
                    ShowDividerOnAutomated = true;
                    if (mainForm.PageManager.SelectedTab == mainForm.Tab_Automated)
                        mainForm.Divider.Visible = true;
                    mainForm.Automated_Text.Visible = false;
                    mainForm.Automated_Text_Failure.Visible = false;
                    mainForm.Automated_Retry.Visible = false;
                    // Show Automated Controls
#if DEBUG
                    mainForm.Debug_AutomatedState.SelectedIndex = 2;
#endif
                    goto default;

                case StageEnum.Output:
                    SetOutputCurrentOperation("Current Operation", ThemeHandler.GetOutputOperationColor());
                    SetOutputProgressBarColor(ThemeHandler.GetOutputProgressBarColor());
                    SetOutputCurrentPercentage(0);
                    SetOutputTotalPercentage(0);
#if DEBUG
                    mainForm.Debug_OutputState.SelectedIndex = 0;
#endif
                    goto output;

                case StageEnum.Output_Failure:
                    SetOutputCurrentOperation("ERROR!", Color.Red);
                    SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Red);
#if DEBUG
                    mainForm.Debug_OutputState.SelectedIndex = 1;
#endif
                    goto default;

                case StageEnum.Output_Success:
                    SetOutputCurrentOperation("SUCCESS!", Color.Green);
                    SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Green);
                    SetOutputCurrentPercentage(100);
                    SetOutputTotalPercentage(100);
#if DEBUG
                    mainForm.Debug_OutputState.SelectedIndex = 2;
#endif
                    goto default;

                case StageEnum.SelfUpdate:
                    mainForm.Divider.Visible = false;
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
            OnTabChange(mainForm.PageManager.SelectedTab);
        }
    }
}
