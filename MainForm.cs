using System;
using System.Windows.Forms;
using MetroFramework.Controls;

namespace MelonLoader
{
    internal partial class MainForm : MetroFramework.Forms.MetroForm
    {
        internal MainForm() => InitializeComponent();
        private void MainForm_Load(object sender, EventArgs e) => Managers.Form.OnLoad();
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) => Managers.Form.OnClose();
        private void Link_Click(object sender, EventArgs e) => Managers.Form.OnLinkClick(sender);
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e) => Managers.Form.OnComboBoxSelectedIndexChange((MetroComboBox)sender);
        private void Button_Click(object sender, EventArgs e) => Managers.Form.OnButtonClick((MetroButton)sender);
        private void CheckBox_CheckedChanged(object sender, EventArgs e) => Managers.Form.OnCheckBoxCheckedChange((MetroCheckBox)sender);
        private void CheckBox_MouseEnter(object sender, EventArgs e) => Managers.Theme.OnCheckBoxMouseEnter((MetroCheckBox)sender);
        private void CheckBox_MouseEnter(object sender, MouseEventArgs e) => Managers.Theme.OnCheckBoxMouseEnter((MetroCheckBox)sender);
        private void CheckBox_MouseLeave(object sender, EventArgs e) => Managers.Theme.OnCheckBoxMouseLeave((MetroCheckBox)sender);
        private void CheckBox_MouseLeave(object sender, MouseEventArgs e) => Managers.Theme.OnCheckBoxMouseLeave((MetroCheckBox)sender);
    }
}