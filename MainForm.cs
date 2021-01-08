using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MelonLoader
{
    public partial class MainForm : MetroFramework.Forms.MetroForm
    {
        private int GameArch = 0;
        internal Version CurrentInstalledVersion = null;

        public MainForm()
        {
            InitializeComponent();
            InstallerVersion.Text = "Installer v" + BuildInfo.Version;
            PageManager.SelectedIndex = 0;
            Automated_Arch_Selection.SelectedIndex = 0;
            Settings_Theme_Selection.SelectedIndex = Config.Theme;
            Settings_AutoUpdateInstaller.Checked = Config.AutoUpdateInstaller;
            Settings_CloseAfterCompletion.Checked = Config.CloseAfterCompletion;
            Settings_ShowAlphaPreReleases.Checked = Config.ShowAlphaPreReleases;
            PageManager.Controls.Clear();
            PageManager.Controls.Add(Tab_Automated);
            Tab_Automated.Text = "Please Wait    ";
            Automated_Install.Size = new Size(419, 44);
            ManualZip_Install.Size = new Size(419, 44);
            Output_Current_Progress_Display.Value = 0;
            Output_Current_Progress_Text.Text = "0";
            Output_Total_Progress_Display.Value = 0;
            Output_Total_Progress_Text.Text = "0";
        }

        private void SelectUnityGame()
        {
            using (OpenFileDialog opd = new OpenFileDialog())
            {
                opd.Filter = "Unity Game (*.exe)|*.exe|Shortcut (*.lnk)|*.lnk";
                opd.RestoreDirectory = true;
                opd.Multiselect = false;
                opd.DereferenceLinks = false;
                if ((opd.ShowDialog() != DialogResult.OK) || string.IsNullOrEmpty(opd.FileName))
                    return;
                string filepath = opd.FileName;
                string file_extension = Path.GetExtension(filepath);
                if (string.IsNullOrEmpty(file_extension) || (!file_extension.Equals(".exe") && !file_extension.Equals(".lnk") && !file_extension.Equals(".url")))
                {
                    MessageBox.Show("Invalid File Selected!", BuildInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SelectUnityGame();
                    return;
                }
                if (file_extension.Equals(".lnk") || file_extension.Equals(".url"))
                {
                    string newfilepath = Program.GetFilePathFromShortcut(filepath);
                    if (string.IsNullOrEmpty(newfilepath) || !newfilepath.EndsWith(".exe"))
                    {
                        MessageBox.Show("Invalid File Selected!", BuildInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SelectUnityGame();
                        return;
                    }
                    filepath = newfilepath;
                }

                // Verify Unity Game

                Automated_UnityGame_Display.Text = filepath;
                ManualZip_UnityGame_Display.Text = filepath;
                Automated_Install.Enabled = true;
                CheckUnityGame();
            }
        }

        private void SelectZipArchive()
        {
            using (OpenFileDialog opd = new OpenFileDialog())
            {
                opd.Filter = "MelonLoader Zip Archive (*.zip)|*.zip";
                opd.RestoreDirectory = true;
                opd.Multiselect = false;
                opd.DereferenceLinks = false;
                if ((opd.ShowDialog() != DialogResult.OK) || string.IsNullOrEmpty(opd.FileName))
                    return;
                string filepath = opd.FileName;
                string file_extension = Path.GetExtension(filepath);
                if (string.IsNullOrEmpty(file_extension) || !file_extension.Equals(".zip"))
                {
                    MessageBox.Show("Invalid File Selected!", BuildInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SelectZipArchive();
                    return;
                }
                ManualZip_ZipArchive_Display.Text = filepath;
                if (!string.IsNullOrEmpty(ManualZip_UnityGame_Display.Text)
                && !ManualZip_UnityGame_Display.Text.Equals("Please Select your Unity Game..."))
                    ManualZip_Install.Enabled = true;
            }
        }

        private void CheckUnityGame()
        {
            if (string.IsNullOrEmpty(Automated_UnityGame_Display.Text) || Automated_UnityGame_Display.Text.Equals("Please Select your Unity Game..."))
                return;
            byte[] filedata = File.ReadAllBytes(Automated_UnityGame_Display.Text);
            if ((filedata == null)
                || (filedata.Length <= 0)
                || (BitConverter.ToUInt16(filedata, (BitConverter.ToInt32(filedata, 60) + 4)) != 34404))
                GameArch = 0;
            else
                GameArch = 1;
            if (!string.IsNullOrEmpty(ManualZip_ZipArchive_Display.Text)
                && !ManualZip_ZipArchive_Display.Text.Equals("Please Select your MelonLoader Zip Archive..."))
                ManualZip_Install.Enabled = true;
            if (Automated_Arch_AutoDetect.Checked)
            {
                Automated_Arch_Selection.SelectedIndex = GameArch;
                Automated_Arch_Selection.Select();
            }
            string folder_path = Path.Combine(Path.GetDirectoryName(Automated_UnityGame_Display.Text), "MelonLoader");
            string legacy_file_path = Path.Combine(folder_path, "MelonLoader.ModHandler.dll");
            string file_path = Path.Combine(folder_path, "MelonLoader.dll");
            if (!File.Exists(legacy_file_path) || !File.Exists(file_path))
            {
                ManualZip_Install.Text = "INSTALL";
                Automated_Install.Text = "INSTALL";
                Automated_Install.Size = new Size(419, 44);
                ManualZip_Install.Size = new Size(419, 44);
                Automated_Uninstall.Visible = false;
                ManualZip_Uninstall.Visible = false;
                return;
            }
            string fileversion = null;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo((File.Exists(legacy_file_path) ? legacy_file_path : file_path));
            if (fileVersionInfo != null)
            {
                fileversion = fileVersionInfo.ProductVersion;
                if (string.IsNullOrEmpty(fileversion))
                    fileversion = fileVersionInfo.FileVersion;
            }
            if (string.IsNullOrEmpty(fileversion))
                fileversion = "0.0.0.0";
            CurrentInstalledVersion = new Version(fileversion);
            Automated_Install.Size = new Size(206, 44);
            ManualZip_Install.Size = new Size(206, 44);
            Automated_Uninstall.Visible = true;
            ManualZip_Uninstall.Visible = true;
            ManualZip_Install.Text = "RE-INSTALL";
            Version selected_ver = new Version(Automated_Version_Selection.Text.Substring(1));
            int compare_ver = selected_ver.CompareTo(CurrentInstalledVersion);
            if (compare_ver < 0)
                Automated_Install.Text = "DOWNGRADE";
            else if (compare_ver > 0)
                Automated_Install.Text = "UPDATE";
            else
                Automated_Install.Text = "RE-INSTALL";
        }

        private void ThemeChanged(object sender, EventArgs e)
        {
            bool lightmode = (Settings_Theme_Selection.SelectedIndex == 1);
            Config.Theme = (lightmode ? 1 : 0);
            MetroFramework.MetroThemeStyle themeStyle = (lightmode ? MetroFramework.MetroThemeStyle.Light : MetroFramework.MetroThemeStyle.Dark);
            StyleManager.Style = (lightmode ? MetroFramework.MetroColorStyle.Green : MetroFramework.MetroColorStyle.Red);
            StyleManager.Theme = themeStyle;
            Theme = themeStyle;
            Style = (lightmode ? MetroFramework.MetroColorStyle.Red : MetroFramework.MetroColorStyle.Green);
            Link_Discord.BackColor = (lightmode ? Color.White : Color.FromArgb(17, 17, 17));
            Link_Twitter.BackColor = Link_Discord.BackColor;
            Link_Wiki.BackColor = Link_Discord.BackColor;
            Link_GitHub.BackColor = Link_Discord.BackColor;
            Link_GitHub.Image = (lightmode ? Properties.Resources.GitHub_Light : Properties.Resources.GitHub_Dark);
            ML_Logo.BackColor = Link_Discord.BackColor;
            ML_Text.BackColor = Link_Discord.BackColor;
            InstallerVersion.Theme = themeStyle;
            PageManager.Style = StyleManager.Style;
            PageManager.Theme = themeStyle;
            Tab_Automated.Theme = themeStyle;
            Tab_ManualZip.Theme = themeStyle;
            Tab_Settings.Theme = themeStyle;
            Settings_Theme_Text.Theme = themeStyle;
            Settings_Theme_Selection.Style = Style;
            Settings_Theme_Selection.Theme = MetroFramework.MetroThemeStyle.Light;
            Settings_AutoUpdateInstaller.Style = Style;
            Settings_AutoUpdateInstaller.Theme = themeStyle;
            Settings_AutoUpdateInstaller.ForeColor = (lightmode ? Color.FromKnownColor(KnownColor.ControlText) : Color.FromKnownColor(KnownColor.ControlDark));
            Settings_AutoUpdateInstaller.CustomForeColor = true;
            Settings_CloseAfterCompletion.Style = Style;
            Settings_CloseAfterCompletion.Theme = themeStyle;
            Settings_CloseAfterCompletion.ForeColor = Settings_AutoUpdateInstaller.ForeColor;
            Settings_CloseAfterCompletion.CustomForeColor = true;
            Settings_ShowAlphaPreReleases.Style = Style;
            Settings_ShowAlphaPreReleases.Theme = themeStyle;
            Settings_ShowAlphaPreReleases.ForeColor = Settings_AutoUpdateInstaller.ForeColor;
            Settings_ShowAlphaPreReleases.CustomForeColor = true;
            Automated_UnityGame_Text.Theme = themeStyle;
            Automated_UnityGame_Select.Theme = themeStyle;
            Automated_UnityGame_Display.BackColor = (lightmode ? Color.White : Color.FromArgb(34, 34, 34));
            Automated_UnityGame_Display.ForeColor = (lightmode ? Color.Black : Color.FromArgb(204, 204, 204));
            Automated_Version_Text.Theme = themeStyle;
            Automated_Version_Selection.Style = Style;
            Automated_Version_Selection.Theme = themeStyle;
            Automated_Version_Latest.Style = Style;
            Automated_Version_Latest.Theme = themeStyle;
            Automated_Version_Latest.ForeColor = Settings_AutoUpdateInstaller.ForeColor;
            Automated_Version_Latest.CustomForeColor = true;
            Automated_Arch_Text.Theme = themeStyle;
            Automated_Arch_Selection.Theme = themeStyle;
            Automated_Arch_AutoDetect.Style = Style;
            Automated_Arch_AutoDetect.Theme = themeStyle;
            Automated_Arch_AutoDetect.ForeColor = Settings_AutoUpdateInstaller.ForeColor;
            Automated_Arch_AutoDetect.CustomForeColor = true;
            Automated_Divider.Theme = themeStyle;
            ManualZip_Divider.Theme = themeStyle;
            Automated_Install.Theme = themeStyle;
            Automated_Uninstall.Theme = themeStyle;
            Tab_Output.Theme = themeStyle;
            ManualZip_Install.Theme = themeStyle;
            ManualZip_Uninstall.Theme = themeStyle;
            ManualZip_UnityGame_Text.Theme = themeStyle;
            ManualZip_UnityGame_Select.Theme = themeStyle;
            ManualZip_UnityGame_Display.BackColor = Automated_UnityGame_Display.BackColor;
            ManualZip_UnityGame_Display.ForeColor = Automated_UnityGame_Display.ForeColor;
            ManualZip_ZipArchive_Text.Theme = themeStyle;
            ManualZip_ZipArchive_Select.Theme = themeStyle;
            ManualZip_ZipArchive_Display.BackColor = Automated_UnityGame_Display.BackColor;
            ManualZip_ZipArchive_Display.ForeColor = Automated_UnityGame_Display.ForeColor;
            InstallerUpdateNotice.Theme = themeStyle;
            InstallerUpdateNotice.ForeColor = (lightmode ? Color.Red : Color.Green);
            Output_Divider.Theme = themeStyle;
            Output_Current_Text.Theme = themeStyle;
            Output_Current_Operation.Theme = themeStyle;
            Output_Current_Progress_Display.Theme = themeStyle;
            Output_Current_Progress_Text.Theme = themeStyle;
            Output_Current_Progress_Text_Label.Theme = themeStyle;
            Output_Total_Text.Theme = themeStyle;
            Output_Total_Progress_Display.Theme = themeStyle;
            Output_Total_Progress_Text.Theme = themeStyle;
            Output_Total_Progress_Text_Label.Theme = themeStyle;
            PleaseWait_PleaseWait.Theme = themeStyle;
            PleaseWait_Text.Theme = themeStyle;
            Automated_x64Only.Theme = themeStyle;
        }
        private void Settings_AutoUpdateInstaller_MouseEnter(object sender, EventArgs e) => Settings_AutoUpdateInstaller.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlDarkDark) : Color.White);
        private void Settings_AutoUpdateInstaller_MouseLeave(object sender, EventArgs e) => Settings_AutoUpdateInstaller.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlText) : Color.FromKnownColor(KnownColor.ControlDark));
        private void Settings_CloseAfterCompletion_MouseEnter(object sender, EventArgs e) => Settings_CloseAfterCompletion.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlDarkDark) : Color.White);
        private void Settings_CloseAfterCompletion_MouseLeave(object sender, EventArgs e) => Settings_CloseAfterCompletion.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlText) : Color.FromKnownColor(KnownColor.ControlDark));
        private void Settings_ShowAlphaReleases_MouseEnter(object sender, EventArgs e) => Settings_ShowAlphaPreReleases.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlDarkDark) : Color.White);
        private void Settings_ShowAlphaReleases_MouseLeave(object sender, EventArgs e) => Settings_ShowAlphaPreReleases.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlText) : Color.FromKnownColor(KnownColor.ControlDark));
        private void Automated_Version_Latest_MouseEnter(object sender, EventArgs e) => Automated_Version_Latest.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlDarkDark) : Color.White);
        private void Automated_Version_Latest_MouseLeave(object sender, EventArgs e) => Automated_Version_Latest.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlText) : Color.FromKnownColor(KnownColor.ControlDark));
        private void Automated_Arch_AutoDetect_MouseEnter(object sender, EventArgs e) => Automated_Arch_AutoDetect.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlDarkDark) : Color.White);
        private void Automated_Arch_AutoDetect_MouseLeave(object sender, EventArgs e) => Automated_Arch_AutoDetect.ForeColor = ((Settings_Theme_Selection.SelectedIndex == 1) ? Color.FromKnownColor(KnownColor.ControlText) : Color.FromKnownColor(KnownColor.ControlDark));

        private void Automated_Version_Latest_CheckedChanged(object sender, EventArgs e)
        {
            Automated_Version_Selection.Enabled = !Automated_Version_Latest.Checked;
            if (Automated_Version_Selection.Enabled || (Automated_Version_Selection.Items.Count <= 0))
                return;
            Automated_Version_Selection.SelectedIndex = 0;
            Automated_Version_Selection.Select();
        }

        private void Automated_Version_Selection_SelectedValueChanged(object sender, EventArgs e)
        {
            bool legacy_version = (Automated_Version_Selection.Text.StartsWith("v0.2") || Automated_Version_Selection.Text.StartsWith("v0.1"));
            Automated_x64Only.Visible = legacy_version;
            Automated_Arch_Selection.Visible = !legacy_version;
            Automated_Arch_AutoDetect.Visible = !legacy_version;
            if ((CurrentInstalledVersion == null) || string.IsNullOrEmpty(Automated_Version_Selection.Text))
                Automated_Install.Text = "INSTALL";
            else
            {
                Version selected_ver = new Version(Automated_Version_Selection.Text.Substring(1));
                int compare_ver = selected_ver.CompareTo(CurrentInstalledVersion);
                if (compare_ver < 0)
                    Automated_Install.Text = "DOWNGRADE";
                else if (compare_ver > 0)
                    Automated_Install.Text = "UPDATE";
                else
                    Automated_Install.Text = "RE-INSTALL";
            }
        }

        private void Automated_Arch_AutoDetect_CheckedChanged(object sender, EventArgs e)
        {
            Automated_Arch_Selection.Enabled = !Automated_Arch_AutoDetect.Checked;
            if (Automated_Arch_Selection.Enabled)
                return;
            Automated_Arch_Selection.SelectedIndex = GameArch;
            Automated_Arch_Selection.Select();
        }

        private void Automated_Install_Click(object sender, EventArgs e)
        {
            if ((CurrentInstalledVersion == null) || string.IsNullOrEmpty(Automated_Version_Selection.Text))
            {
                OperationHandler.CurrentOperation = OperationHandler.Operations.INSTALL;
                Tab_Output.Text = "INSTALL  ";
            }
            else
            {
                Version selected_ver = new Version(Automated_Version_Selection.Text.Substring(1));
                int compare_ver = selected_ver.CompareTo(CurrentInstalledVersion);
                if (compare_ver < 0)
                {
                    OperationHandler.CurrentOperation = OperationHandler.Operations.DOWNGRADE;
                    Tab_Output.Text = "DOWNGRADE   ";
                }
                else if (compare_ver > 0)
                {
                    OperationHandler.CurrentOperation = OperationHandler.Operations.UPDATE;
                    Tab_Output.Text = "UPDATE   ";
                }
                else
                {
                    OperationHandler.CurrentOperation = OperationHandler.Operations.REINSTALL;
                    Tab_Output.Text = "RE-INSTALL   ";
                }
            }
            bool legacy_version = (Automated_Version_Selection.Text.StartsWith("v0.2") || Automated_Version_Selection.Text.StartsWith("v0.1"));
            string selected_version = Automated_Version_Selection.Text;
            new Thread(() => { OperationHandler.Automated_Install(Path.GetDirectoryName(Automated_UnityGame_Display.Text), selected_version, (legacy_version ? false : (Automated_Arch_Selection.SelectedIndex == 0)), legacy_version); }).Start();
            Program.SetTotalPercentage(0);
            PageManager.Cursor = Cursors.Default;
            PageManager.Controls.Clear();
            PageManager.Controls.Add(Tab_Output);
        }

        private void ManualZip_Install_Click(object sender, EventArgs e)
        {
            if (CurrentInstalledVersion == null)
            {
                OperationHandler.CurrentOperation = OperationHandler.Operations.INSTALL;
                Tab_Output.Text = "INSTALL  ";
            }
            else
            {
                OperationHandler.CurrentOperation = OperationHandler.Operations.REINSTALL;
                Tab_Output.Text = "RE-INSTALL   ";
            }
            new Thread(() => { OperationHandler.ManualZip_Install(ManualZip_ZipArchive_Display.Text, Path.GetDirectoryName(Automated_UnityGame_Display.Text)); }).Start();
            Program.SetTotalPercentage(0);
            PageManager.Cursor = Cursors.Default;
            PageManager.Controls.Clear();
            PageManager.Controls.Add(Tab_Output);
        }

        private void ClickedUninstall()
        {
            DialogResult result = MessageBox.Show("Are you sure you wish to Uninstall MelonLoader?", BuildInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;
            OperationHandler.CurrentOperation = OperationHandler.Operations.UNINSTALL;
            Tab_Output.Text = "UN-INSTALL   ";
            new Thread(() => { OperationHandler.Uninstall(Path.GetDirectoryName(Automated_UnityGame_Display.Text)); }).Start();
            Program.SetTotalPercentage(0);
            PageManager.Cursor = Cursors.Default;
            PageManager.Controls.Clear();
            PageManager.Controls.Add(Tab_Output);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Closing = true;
            if ((Program.webClient != null) && Program.webClient.IsBusy)
                Program.webClient.CancelAsync();
            if (OperationHandler.CurrentOperation != OperationHandler.Operations.NONE)
                Thread.Sleep(1000);
            TempFileCache.ClearCache();
            Program.OperationError();
            if (OperationHandler.CurrentOperation <= OperationHandler.Operations.INSTALLER_UPDATE)
                return;

            // Sanatize Operation

            MessageBox.Show((OperationHandler.CurrentOperationName + " was Cancelled!"), BuildInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Main_Load(object sender, EventArgs e) => new Thread(ThreadHandler.CheckForInstallerUpdate).Start();
        private void Error_Retry_Click(object sender, EventArgs e) => new Thread(ThreadHandler.RefreshReleases).Start();
        private void Link_Discord_Click(object sender, EventArgs e) => Process.Start(Config.Link_Discord);
        private void Link_Twitter_Click(object sender, EventArgs e) => Process.Start(Config.Link_Twitter);
        private void Link_GitHub_Click(object sender, EventArgs e) => Process.Start(Config.Link_GitHub);
        private void Link_Wiki_Click(object sender, EventArgs e) => Process.Start(Config.Link_Wiki);
        private void InstallerUpdateNotice_Click(object sender, EventArgs e) => Process.Start(Config.Link_Update);
        private void Settings_AutoUpdateInstaller_CheckedChanged(object sender, EventArgs e) => Config.AutoUpdateInstaller = Settings_AutoUpdateInstaller.Checked;
        private void Settings_CloseAfterCompletion_CheckedChanged(object sender, EventArgs e) => Config.CloseAfterCompletion = Settings_CloseAfterCompletion.Checked;
        private void ManualZip_UnityGame_Select_Click(object sender, EventArgs e) => SelectUnityGame();
        private void Automated_UnityGame_Select_Click(object sender, EventArgs e) => SelectUnityGame();
        private void Automated_Uninstall_Click(object sender, EventArgs e) => ClickedUninstall();
        private void ManualZip_Uninstall_Click(object sender, EventArgs e) => ClickedUninstall();
        private void ManualZip_ZipArchive_Select_Click(object sender, EventArgs e) => SelectZipArchive();

        internal void RefreshReleasesListing()
        {
            if (Automated_Version_Selection == null)
                return;
            Automated_Version_Selection.Items.Clear();
            Automated_Version_Selection.Items.AddRange((Config.ShowAlphaPreReleases
                ? ThreadHandler.releasesList_All.ToArray()
                : ThreadHandler.releasesList.ToArray()));
            if (Automated_Version_Selection.Items.Count > 0)
                Automated_Version_Selection.SelectedIndex = 0;
        }
        private void Settings_ShowAlphaPreReleases_CheckedChanged(object sender, EventArgs e)
        {
            Config.ShowAlphaPreReleases = Settings_ShowAlphaPreReleases.Checked;
            RefreshReleasesListing();
        }
    }
}