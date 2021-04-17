using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using MetroFramework.Controls;
using MelonLoader.Managers;

namespace MelonLoader
{
    internal partial class MainForm : MetroFramework.Forms.MetroForm
    {
#if DEBUG
        private void Debug_AutomatedState_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Debug_AutomatedState.SelectedIndex == 0)
                FormHandler.SetStage(FormHandler.StageEnum.Main);
            else if (Debug_AutomatedState.SelectedIndex == 1)
                FormHandler.SetStage(FormHandler.StageEnum.Automated_Failure);
            else if (Debug_AutomatedState.SelectedIndex == 2)
                FormHandler.SetStage(FormHandler.StageEnum.Automated_Success);
        }
        private void Debug_OutputState_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Debug_OutputState.SelectedIndex == 0)
            {
                FormHandler.SetOutputCurrentOperation("Current Operation", ThemeHandler.GetOutputOperationColor());
                FormHandler.SetOutputProgressBarColor(ThemeHandler.GetOutputProgressBarColor());
                FormHandler.SetOutputCurrentPercentage(100);
                FormHandler.SetOutputTotalPercentage(100);
            }
            else if (Debug_OutputState.SelectedIndex == 1)
            {
                FormHandler.SetOutputCurrentOperation("ERROR!", System.Drawing.Color.Red);
                FormHandler.SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Red);
                FormHandler.SetOutputCurrentPercentage(50);
                FormHandler.SetOutputTotalPercentage(50);
            }
            else if (Debug_OutputState.SelectedIndex == 2)
            {
                FormHandler.SetOutputCurrentOperation("SUCCESS!", System.Drawing.Color.Green);
                FormHandler.SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Green);
                FormHandler.SetOutputCurrentPercentage(100);
                FormHandler.SetOutputTotalPercentage(100);
            }
        }
        private void Debug_OutputTest_Click(object sender, EventArgs e) => DebugOutputTest(50, 4);
        private void Debug_OutputFailureTest_Click(object sender, EventArgs e) => DebugOutputTest(50, 4, 3);
        private void Debug_OutputSuccessTest_Click(object sender, EventArgs e) => DebugOutputTest(50, 4, testsuccess: true);
        private void DebugOutputTest(int sleepms, int max_operations, int operations_until_failure = 0, bool testsuccess = false)
        {
            FormHandler.SetStage(FormHandler.StageEnum.Output);
            PageManager.SelectedTab = Tab_Output;
            new Thread(() =>
            {
                int currentoperation = 1;
                for (currentoperation = 1; currentoperation <= max_operations; currentoperation++)
                {
                    if (FormHandler.IsClosing)
                        break;
                    FormHandler.SetOutputCurrentPercentage(0);
                    int totalpercentage = (100 / max_operations) * (currentoperation - 1);
                    FormHandler.SetOutputTotalPercentage(totalpercentage);
                    if ((operations_until_failure != 0)
                        && (currentoperation == operations_until_failure))
                    {
                        if (!FormHandler.IsClosing)
                            FormHandler.Invoke(() => FormHandler.SetStage(FormHandler.StageEnum.Output_Failure));
                        FormHandler.SpawnMessageBox($"Test Operation {currentoperation} ERROR", MessageBoxIcon.Error, MessageBoxButtons.OK);
                        break;
                    }
                    for (int currentpercentage = 0; currentpercentage <= 100; currentpercentage++)
                    {
                        if (FormHandler.IsClosing)
                            break;
                        FormHandler.Invoke(() =>
                        {
                            FormHandler.SetOutputCurrentOperation($"Test Operation {currentoperation}", ThemeHandler.GetOutputOperationColor());
                            FormHandler.SetOutputCurrentPercentage(currentpercentage);
                            FormHandler.SetOutputTotalPercentage(totalpercentage + (int)(25 * (currentpercentage * 0.01)));
                        });
                        Thread.Sleep(sleepms);
                    }
                }
                if (!FormHandler.IsClosing)
                    FormHandler.Invoke(() => FormHandler.SetStage(FormHandler.StageEnum.Output_Success));
                if ((operations_until_failure == 0) && testsuccess)
                    FormHandler.SpawnMessageBox($"Test SUCCESS", MessageBoxIcon.Asterisk, MessageBoxButtons.OK);
                if (!FormHandler.IsClosing)
                    FormHandler.Invoke(() =>
                    {
                        FormHandler.SetStage(FormHandler.StageEnum.Output);
                        FormHandler.SetOutputCurrentPercentage(100);
                        FormHandler.SetOutputTotalPercentage(100);

                        int i = FormHandler.mainForm.Debug_AutomatedState.SelectedIndex;
                        FormHandler.SetStage(FormHandler.StageEnum.Debug);
                        FormHandler.mainForm.Debug_AutomatedState.SelectedIndex = i;
                    });
            }).Start();
        }
#else
        private void Debug_AutomatedState_SelectedIndexChanged(object sender, EventArgs e) { }
        private void Debug_OutputState_SelectedIndexChanged(object sender, EventArgs e) { }
        private void Debug_OutputTest_Click(object sender, EventArgs e) { }
        private void Debug_OutputFailureTest_Click(object sender, EventArgs e) { }
        private void Debug_OutputSuccessTest_Click(object sender, EventArgs e) { }
#endif

        internal MainForm() => InitializeComponent();
        private void MainForm_Load(object sender, EventArgs e) => FormHandler.OnLoad();
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) => FormHandler.OnClose();
        private void Link_Discord_Click(object sender, EventArgs e) => Process.Start(URLs.ExternalLinks.Discord);
        private void Link_Twitter_Click(object sender, EventArgs e) => Process.Start(URLs.ExternalLinks.Twitter);
        private void Link_GitHub_Click(object sender, EventArgs e) => Process.Start(URLs.ExternalLinks.GitHub);
        private void Link_Wiki_Click(object sender, EventArgs e) => Process.Start(URLs.ExternalLinks.Wiki);
        private void InstallerVersion_Click(object sender, EventArgs e) => Process.Start(URLs.ExternalLinks.Installer);
        private void InstallerUpdateNotice_Click(object sender, EventArgs e) => Process.Start($"{URLs.ExternalLinks.Installer}/releases/latest");

        private void PageManager_SelectedIndexChanged(object sender, EventArgs e) => FormHandler.OnTabChange(PageManager.SelectedTab);
        private void Settings_Theme_Selection_SelectedIndexChanged(object sender, EventArgs e) { Config.Theme = Settings_Theme_Selection.SelectedIndex; ThemeHandler.OnThemeChange(); }
        private void CheckBox_MouseEnter(object sender, EventArgs e) => ThemeHandler.OnCheckBoxMouseEnter((MetroCheckBox)sender);
        private void CheckBox_MouseEnter(object sender, MouseEventArgs e) => ThemeHandler.OnCheckBoxMouseEnter((MetroCheckBox)sender);
        private void CheckBox_MouseLeave(object sender, EventArgs e) => ThemeHandler.OnCheckBoxMouseLeave((MetroCheckBox)sender);
        private void CheckBox_MouseLeave(object sender, MouseEventArgs e) => ThemeHandler.OnCheckBoxMouseLeave((MetroCheckBox)sender);
        private void Automated_Retry_Click(object sender, EventArgs e) => FormHandler.GetReleases();
    }
}