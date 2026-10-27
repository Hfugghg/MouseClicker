using System;
using System.Drawing;
using System.Windows.Forms;

namespace MouseClicker
{
    /// <summary>Dialog for choosing the global start/stop key.</summary>
    internal sealed class HotkeySettingsForm : Form
    {
        private readonly GlobalHotkeyManager _manager;
        private readonly ComboBox _keysComboBox;
        public HotkeySettingsForm(GlobalHotkeyManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Text = Localization.Get("HotkeySettings");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(390, 142);
            var label = new Label { AutoSize = true, Text = Localization.Get("HotkeyLabel"), Location = new Point(14, 18) };
            _keysComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(14, 45), Width = 360 };
            foreach (Keys key in GlobalHotkeyManager.SupportedKeys) _keysComboBox.Items.Add(key.ToString());
            _keysComboBox.SelectedItem = _manager.Hotkey.ToString();
            var hint = new Label { AutoSize = false, Text = Localization.Get("HotkeyHint"), Location = new Point(14, 75), Size = new Size(360, 28) };
            var apply = new Button { Text = Localization.Get("HotkeyApply"), Location = new Point(194, 108), Size = new Size(85, 25) };
            var cancel = new Button { Text = Localization.Get("CloseButton"), DialogResult = DialogResult.Cancel, Location = new Point(289, 108), Size = new Size(85, 25) };
            apply.Click += ApplyButton_Click;
            Controls.AddRange(new Control[] { label, _keysComboBox, hint, apply, cancel });
            AcceptButton = apply; CancelButton = cancel;
        }

        private void ApplyButton_Click(object? sender, EventArgs e)
        {
            if (_keysComboBox.SelectedItem is not string selected || !Enum.TryParse(selected, out Keys key)) return;
            try { _manager.SetHotkey(key); DialogResult = DialogResult.OK; Close(); }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(Localization.Get("SaveConfigError"), ex.Message), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
