using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MouseClicker
{
    /// <summary>用于选择全局启动/停止热键的对话框。</summary>
    internal sealed class HotkeySettingsForm : Form
    {
        private const int ContentMargin = 14;
        private const int ContentWidth = 392;
        private readonly GlobalHotkeyManager _manager;
        private readonly Label _label;
        private readonly TextBox _hotkeyTextBox;
        private readonly Label _hintLabel;
        private readonly Label _statusLabel;
        private readonly Button _applyButton;
        private readonly Button _cancelButton;
        private Keys? _pendingHotkey;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        public HotkeySettingsForm(GlobalHotkeyManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Text = Localization.Get("HotkeySettings");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            _label = new Label { AutoSize = true, Text = Localization.Get("HotkeyLabel") };
            _hotkeyTextBox = new TextBox
            {
                ReadOnly = true,
                TabStop = true,
                TextAlign = HorizontalAlignment.Center,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
                Text = _manager.Hotkey.ToString()
            };
            _hotkeyTextBox.KeyDown += HotkeyTextBox_KeyDown;
            // AutoSize 与 MaximumSize 让标签按实际文本自动换行并决定高度。
            _hintLabel = new Label { AutoSize = true, MaximumSize = new Size(ContentWidth, 0), Padding = new Padding(0, 0, 4, 0), Text = Localization.Get("HotkeyHint") };
            _statusLabel = new Label { AutoSize = true, MaximumSize = new Size(ContentWidth, 0), Padding = new Padding(0, 0, 4, 0), ForeColor = Color.Red, Visible = false };
            _applyButton = new Button { Text = Localization.Get("HotkeyApply") };
            _cancelButton = new Button { Text = Localization.Get("CloseButton"), DialogResult = DialogResult.Cancel };
            _applyButton.Click += ApplyButton_Click;

            Controls.AddRange(new Control[] { _label, _hotkeyTextBox, _hintLabel, _statusLabel, _applyButton, _cancelButton });
            AcceptButton = _applyButton;
            CancelButton = _cancelButton;
            LayoutContent();
        }

        // 仅在输入框获得焦点时捕获按键，避免误触发其他控件。
        private void HotkeyTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                case Keys.Enter:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ApplyHotkey();
                    return;
                case Keys.Tab:
                    // 让 Tab 正常切换焦点。
                    return;
            }

            if (TryGetCapturedKey(e.KeyData, out Keys key) && !IsWindowsKeyDown())
            {
                _pendingHotkey = key;
                _hotkeyTextBox.Text = key.ToString();
                SetStatus(null);
            }
            else
            {
                SetStatus(Localization.Get("HotkeyInvalidKey"));
            }

            // 阻止快捷键继续传递，避免提示音或触发其他控件。
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        // 只接受受支持且不带 Ctrl/Alt/Shift 的按键；Win 组合由调用方另行检查，便于在没有消息循环时测试。
        internal static bool TryGetCapturedKey(Keys keyData, out Keys key)
        {
            key = keyData & Keys.KeyCode;
            if ((keyData & Keys.Modifiers) != Keys.None) return false;
            return GlobalHotkeyManager.SupportedKeys.Contains(key);
        }

        // Win 键不属于 Keys.Modifiers，只能通过系统状态判断，避免接受 Win+F6 一类的组合。
        private static bool IsWindowsKeyDown() =>
            (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;

        private void ApplyButton_Click(object? sender, EventArgs e) => ApplyHotkey();

        private void ApplyHotkey()
        {
            try
            {
                _manager.SetHotkey(_pendingHotkey ?? _manager.Hotkey);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(Localization.Get("SaveConfigError"), ex.Message), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetStatus(string? message)
        {
            _statusLabel.Text = message ?? string.Empty;
            _statusLabel.Visible = !string.IsNullOrEmpty(message);
            LayoutContent();
        }

        // 根据当前语言的实际文本宽度和行数重新排列控件，避免提示文字被裁切。
        private void LayoutContent()
        {
            int y = ContentMargin;
            _label.Location = new Point(ContentMargin, y);
            y = _label.Bottom + 8;

            _hotkeyTextBox.Height = _hotkeyTextBox.PreferredHeight;
            _hotkeyTextBox.Location = new Point(ContentMargin, y);
            _hotkeyTextBox.Width = ContentWidth;
            y = _hotkeyTextBox.Bottom + 10;

            _hintLabel.Location = new Point(ContentMargin, y);
            y = _hintLabel.Bottom + 4;

            if (_statusLabel.Visible)
            {
                _statusLabel.Location = new Point(ContentMargin, y);
                y = _statusLabel.Bottom + 4;
            }

            const int buttonHeight = 28;
            int buttonTop = y + 6;
            int applyWidth = Math.Max(85, TextRenderer.MeasureText(_applyButton.Text, _applyButton.Font).Width + 24);
            int cancelWidth = Math.Max(85, TextRenderer.MeasureText(_cancelButton.Text, _cancelButton.Font).Width + 24);
            _applyButton.Size = new Size(applyWidth, buttonHeight);
            _cancelButton.Size = new Size(cancelWidth, buttonHeight);
            int cancelX = ContentMargin + ContentWidth - cancelWidth;
            _cancelButton.Location = new Point(cancelX, buttonTop);
            _applyButton.Location = new Point(cancelX - 8 - applyWidth, buttonTop);

            ClientSize = new Size(ContentMargin * 2 + ContentWidth, buttonTop + buttonHeight + ContentMargin);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 字体在创建窗口句柄后才最终确定，加载时再排列一次。
            LayoutContent();
        }
    }
}
