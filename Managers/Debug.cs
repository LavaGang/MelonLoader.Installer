#if DEBUG
using System.Threading;
using System.Windows.Forms;

namespace MelonLoader.Managers
{
    internal static class Debug
    {
        internal static void SetFormStage()
        {
            Form.mainForm.PageManager.Controls.Clear();
            Form.mainForm.PageManager.Controls.Add(Form.mainForm.Tab_Debug);
            //Form.mainForm.PageManager.Controls.Add(Form.mainForm.Tab_Automated);
            //Form.mainForm.PageManager.Controls.Add(Form.mainForm.Tab_ManualZip);
            //Form.mainForm.PageManager.Controls.Add(Form.mainForm.Tab_Settings);
            //Form.mainForm.PageManager.Controls.Add(Form.mainForm.Tab_Output);
            Form.mainForm.Debug_AutomatedState.SelectedIndex = 0;
            Form.mainForm.Debug_OutputState.SelectedIndex = 0;
        }

        internal static void SetAutomatedState(int state)
        {
            switch (state)
            {
                default:
                case 0:
                    Form.SetStage(Form.StageEnum.Main);
                    break;

                case 1:
                    Form.SetStage(Form.StageEnum.Automated_Failure);
                    break;

                case 2:
                    Form.SetStage(Form.StageEnum.Automated_Success);
                    break;
            }
        }

        internal static void SetOutputState(int state)
        {
            switch (state)
            {
                default:
                case 0:
                    Form.SetOutputCurrentOperation("Current Operation", Theme.GetOutputOperationColor());
                    Form.SetOutputProgressBarColor(Theme.GetOutputProgressBarColor());
                    Form.SetOutputCurrentPercentage(100);
                    Form.SetOutputTotalPercentage(100);
                    break;

                case 1:
                    Form.SetOutputCurrentOperation("ERROR!", System.Drawing.Color.Red);
                    Form.SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Red);
                    Form.SetOutputCurrentPercentage(50);
                    Form.SetOutputTotalPercentage(50);
                    break;

                case 2:
                    Form.SetOutputCurrentOperation("SUCCESS!", System.Drawing.Color.Green);
                    Form.SetOutputProgressBarColor(MetroFramework.MetroColorStyle.Green);
                    Form.SetOutputCurrentPercentage(100);
                    Form.SetOutputTotalPercentage(100);
                    break;
            }
        }

        internal static void RunOutputTest(int sleepms, int max_operations, int operations_until_failure = 0, bool testsuccess = false)
        {
            Form.SetStage(Form.StageEnum.Output);
            Form.mainForm.PageManager.SelectedTab = Form.mainForm.Tab_Output;
            new Thread(() =>
            {
                int currentoperation = 1;
                for (currentoperation = 1; currentoperation <= max_operations; currentoperation++)
                {
                    if (Form.IsClosing)
                        break;
                    Form.SetOutputCurrentPercentage(0);
                    int totalpercentage = (100 / max_operations) * (currentoperation - 1);
                    Form.SetOutputTotalPercentage(totalpercentage);
                    if ((operations_until_failure != 0)
                        && (currentoperation == operations_until_failure))
                    {
                        if (!Form.IsClosing)
                            Form.Invoke(() => Form.SetStage(Form.StageEnum.Output_Failure));
                        Program.CreateMessageBox($"Test Operation {currentoperation} ERROR", MessageBoxIcon.Error, MessageBoxButtons.OK);
                        break;
                    }
                    for (int currentpercentage = 0; currentpercentage <= 100; currentpercentage++)
                    {
                        if (Form.IsClosing)
                            break;
                        Form.Invoke(() =>
                        {
                            Form.SetOutputCurrentOperation($"Test Operation {currentoperation}", Theme.GetOutputOperationColor());
                            Form.SetOutputCurrentPercentage(currentpercentage);
                            Form.SetOutputTotalPercentage(totalpercentage + (int)(25 * (currentpercentage * 0.01)));
                        });
                        Thread.Sleep(sleepms);
                    }
                }
                if (!Form.IsClosing)
                    Form.Invoke(() => Form.SetStage(Form.StageEnum.Output_Success));
                if ((operations_until_failure == 0) && testsuccess)
                    Program.CreateMessageBox($"Test SUCCESS", MessageBoxIcon.Asterisk, MessageBoxButtons.OK);
                if (!Form.IsClosing)
                    Form.Invoke(() =>
                    {
                        Form.SetStage(Form.StageEnum.Output);
                        Form.SetOutputCurrentPercentage(100);
                        Form.SetOutputTotalPercentage(100);

                        int i = Form.mainForm.Debug_AutomatedState.SelectedIndex;
                        Form.SetStage(Form.StageEnum.Debug);
                        Form.mainForm.Debug_AutomatedState.SelectedIndex = i;
                    });
            }).Start();
        }

        internal static void OnThemeChange(Theme.PalletData pallet)
        {
            Form.mainForm.Tab_Debug.Theme = pallet.ThemeStyle;
            Form.mainForm.Tab_Debug.BackColor = pallet.TabBackColor;
            Form.mainForm.Debug_LatestInstallerRelease.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_LatestInstallerRelease_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_LatestInstallerRelease_Text.ForeColor = pallet.NoticesColor;
            Form.mainForm.Debug_AutomatedState.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_AutomatedState.Style = pallet.ColorStyle;
            Form.mainForm.Debug_AutomatedState_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_AutomatedState_Text.ForeColor = pallet.NoticesColor;
            Form.mainForm.Debug_OutputState.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_OutputState.Style = pallet.ColorStyle;
            Form.mainForm.Debug_OutputState_Text.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_OutputState_Text.ForeColor = pallet.NoticesColor;
            Form.mainForm.Debug_OutputTest.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_OutputTest.Style = pallet.ColorStyle;
            Form.mainForm.Debug_OutputFailureTest.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_OutputFailureTest.Style = pallet.ColorStyle;
            Form.mainForm.Debug_OutputSuccessTest.Theme = pallet.ThemeStyle;
            Form.mainForm.Debug_OutputSuccessTest.Style = pallet.ColorStyle;
        }
    }
}
#endif