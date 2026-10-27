using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MouseClicker
{
    public partial class Form1 : Form
    {
        // --- 全局键盘钩子，用于监听停止热键 ---
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static Form1 _instance = null!;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // --- 核心逻辑字段 ---
        private readonly MouseController _mouseController;
        private List<ClickArea> _clickAreas = new List<ClickArea>();
        private OverlayForm _overlayForm;

        // --- 定时器和状态字段 ---
        private System.Windows.Forms.Timer _clickTimer;
        private Random _random = new Random();
        private bool _isClicking = false;
        private int _currentClickIndex = 0;

        // --- UI 控件字段 ---
        private MenuStrip menuStrip = null!;
        private ToolStripMenuItem fileMenuItem = null!, languageMenuItem = null!, helpMenuItem = null!, aboutMenuItem = null!;
        private ToolStripMenuItem saveConfigMenuItem = null!, loadConfigMenuItem = null!, viewHelpMenuItem = null!;
        private ListBox areasListBox = null!;
        private Button addAreaButton = null!, removeAreaButton = null!, moveUpButton = null!, moveDownButton = null!;
        private Label intervalLabel = null!, jitterLabel = null!, sequenceLabel = null!;
        private NumericUpDown intervalNumericUpDown = null!, jitterNumericUpDown = null!;
        private ComboBox sequenceComboBox = null!;
        private CheckBox showOverlayCheckBox = null!;
        private CheckBox hardwareModeCheckBox = null!;
        private Button startButton = null!;

        // 主窗口构造函数
        public Form1()
        {
            InitializeComponent();
            _instance = this;
            _mouseController = new MouseController(new Win32MouseInput());

            _clickTimer = new System.Windows.Forms.Timer();
            _clickTimer.Tick += new EventHandler(clickTimer_Tick);

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 设置窗口图标
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            }
            catch (Exception)
            {
                // 忽略图标加载失败
            }

            InitializeCustomComponents();
            UpdateUIStrings();

            _overlayForm = new OverlayForm();
            _overlayForm.Show();
            this.FormClosing += (s, e) => 
            {
                if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID);
                _overlayForm.Close(); 
            };
        }

        // 键盘钩子回调，用于监听停止热键 (ESC)。
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (_instance != null && _instance._isClicking && (Keys)vkCode == Keys.Escape)
                {
                    _instance.BeginInvoke(new Action(() => _instance.StopClicking()));
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // 安装全局键盘钩子。
        private void SetHook()
        {
            if (_hookID != IntPtr.Zero) return;
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule? curModule = curProcess.MainModule)
                {
                    if (curModule != null && curModule.ModuleName != null)
                    {
                        _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
            }
        }

        // 初始化通过代码创建的UI组件。
        private void InitializeCustomComponents()
        {
            // 创建控件
            this.menuStrip = new MenuStrip();
            fileMenuItem = new ToolStripMenuItem();
            saveConfigMenuItem = new ToolStripMenuItem();
            loadConfigMenuItem = new ToolStripMenuItem();
            languageMenuItem = new ToolStripMenuItem();
            var zhMenuItem = new ToolStripMenuItem("中文");
            var enMenuItem = new ToolStripMenuItem("English");
            var jaMenuItem = new ToolStripMenuItem("日本語");
            helpMenuItem = new ToolStripMenuItem();
            viewHelpMenuItem = new ToolStripMenuItem();
            aboutMenuItem = new ToolStripMenuItem();
            this.areasListBox = new ListBox();
            this.addAreaButton = new Button();
            this.removeAreaButton = new Button();
            this.moveUpButton = new Button();
            this.moveDownButton = new Button();
            this.intervalLabel = new Label { AutoSize = true };
            this.intervalNumericUpDown = new NumericUpDown { Maximum = 600000, Minimum = 10, Value = 1000 };
            this.jitterLabel = new Label { AutoSize = true };
            this.jitterNumericUpDown = new NumericUpDown { Maximum = 10000, Value = 100 };
            this.sequenceLabel = new Label { AutoSize = true };
            this.sequenceComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.showOverlayCheckBox = new CheckBox { AutoSize = true, Checked = true };
            this.hardwareModeCheckBox = new CheckBox { AutoSize = true, Checked = false };
            this.startButton = new Button { Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold) };

            // 将控件添加到窗体
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            fileMenuItem.DropDownItems.Add(saveConfigMenuItem);
            fileMenuItem.DropDownItems.Add(loadConfigMenuItem);
            languageMenuItem.DropDownItems.Add(zhMenuItem);
            languageMenuItem.DropDownItems.Add(enMenuItem);
            languageMenuItem.DropDownItems.Add(jaMenuItem);
            helpMenuItem.DropDownItems.Add(viewHelpMenuItem);
            this.menuStrip.Items.Add(fileMenuItem);
            this.menuStrip.Items.Add(languageMenuItem);
            this.menuStrip.Items.Add(helpMenuItem);
            this.menuStrip.Items.Add(aboutMenuItem);
            this.Controls.Add(this.areasListBox);
            this.Controls.Add(this.addAreaButton);
            this.Controls.Add(this.removeAreaButton);
            this.Controls.Add(this.moveUpButton);
            this.Controls.Add(this.moveDownButton);
            this.Controls.Add(this.intervalLabel);
            this.Controls.Add(this.intervalNumericUpDown);
            this.Controls.Add(this.jitterLabel);
            this.Controls.Add(this.jitterNumericUpDown);
            this.Controls.Add(this.sequenceLabel);
            this.Controls.Add(this.sequenceComboBox);
            this.Controls.Add(this.showOverlayCheckBox);
            this.Controls.Add(this.hardwareModeCheckBox);
            this.Controls.Add(this.startButton);

            // 附加事件处理器
            saveConfigMenuItem.Click += new EventHandler(saveConfigMenuItem_Click);
            loadConfigMenuItem.Click += new EventHandler(loadConfigMenuItem_Click);
            zhMenuItem.Click += (s, e) => { ChangeLanguage("zh-CN"); };
            enMenuItem.Click += (s, e) => { ChangeLanguage("en-US"); };
            jaMenuItem.Click += (s, e) => { ChangeLanguage("ja-JP"); };
            viewHelpMenuItem.Click += new EventHandler(viewHelpMenuItem_Click);
            aboutMenuItem.Click += new EventHandler(aboutMenuItem_Click);
            addAreaButton.Click += new EventHandler(addAreaButton_Click);
            removeAreaButton.Click += new EventHandler(removeAreaButton_Click);
            moveUpButton.Click += new EventHandler(moveUpButton_Click);
            moveDownButton.Click += new EventHandler(moveDownButton_Click);
            startButton.Click += new EventHandler(startButton_Click);
        }

        // 动态调整UI控件的布局。
        private void PerformLayoutLayout()
        {
            int controlPadding = 5;
            int rowHeight = 30;
            int formPadding = 10;
            int currentY = menuStrip.Height + formPadding;

            int formWidth = 430;

            areasListBox.Location = new Point(formPadding, currentY);
            areasListBox.Size = new Size(formWidth - (formPadding * 2), 120);
            currentY += areasListBox.Height + controlPadding;

            int buttonWidth = (formWidth - (formPadding * 2) - (controlPadding * 3)) / 4;
            addAreaButton.Location = new Point(formPadding, currentY);
            addAreaButton.Size = new Size(buttonWidth, rowHeight);
            removeAreaButton.Location = new Point(addAreaButton.Right + controlPadding, currentY);
            removeAreaButton.Size = new Size(buttonWidth, rowHeight);
            moveUpButton.Location = new Point(removeAreaButton.Right + controlPadding, currentY);
            moveUpButton.Size = new Size(buttonWidth, rowHeight);
            moveDownButton.Location = new Point(moveUpButton.Right + controlPadding, currentY);
            moveDownButton.Size = new Size(buttonWidth, rowHeight);
            currentY += rowHeight + (controlPadding * 2);

            intervalLabel.Location = new Point(formPadding, currentY + 4);
            intervalNumericUpDown.Location = new Point(intervalLabel.Right + controlPadding, currentY);
            intervalNumericUpDown.Size = new Size(100, rowHeight);

            jitterLabel.Location = new Point(intervalNumericUpDown.Right + controlPadding, currentY + 4);
            jitterNumericUpDown.Location = new Point(jitterLabel.Right + controlPadding, currentY);
            jitterNumericUpDown.Size = new Size(100, rowHeight);
            currentY += rowHeight + controlPadding;

            sequenceLabel.Location = new Point(formPadding, currentY + 4);
            sequenceComboBox.Location = new Point(sequenceLabel.Right + controlPadding, currentY);
            sequenceComboBox.Size = new Size(180, rowHeight);

            showOverlayCheckBox.Location = new Point(sequenceComboBox.Right + controlPadding, currentY + 4);
            currentY += rowHeight + controlPadding;

            hardwareModeCheckBox.Location = new Point(formPadding, currentY);
            currentY += rowHeight + controlPadding;

            int requiredWidth = Math.Max(moveDownButton.Right, jitterNumericUpDown.Right);
            requiredWidth = Math.Max(requiredWidth, showOverlayCheckBox.Right);
            requiredWidth = Math.Max(requiredWidth, hardwareModeCheckBox.Right);
            requiredWidth += formPadding;
            formWidth = Math.Max(formWidth, requiredWidth);

            areasListBox.Width = formWidth - (formPadding * 2);
            startButton.Width = formWidth - (formPadding * 2);
            int buttonWidthNew = (formWidth - (formPadding * 2) - (controlPadding * 3)) / 4;
            addAreaButton.Width = removeAreaButton.Width = moveUpButton.Width = moveDownButton.Width = buttonWidthNew;
            removeAreaButton.Left = addAreaButton.Right + controlPadding;
            moveUpButton.Left = removeAreaButton.Right + controlPadding;
            moveDownButton.Left = moveUpButton.Right + controlPadding;

            startButton.Location = new Point(formPadding, currentY);
            startButton.Size = new Size(formWidth - (formPadding * 2), 40);

            this.ClientSize = new Size(formWidth, startButton.Bottom + formPadding);
        }

        // 更改应用程序语言。
        private void ChangeLanguage(string lang)
        {
            Localization.Language = lang;
            UpdateUIStrings();
        }

        // 根据当前语言更新UI文本。
        private void UpdateUIStrings()
        {
            fileMenuItem.Text = Localization.Get("File");
            saveConfigMenuItem.Text = Localization.Get("SaveConfig");
            loadConfigMenuItem.Text = Localization.Get("LoadConfig");
            languageMenuItem.Text = Localization.Get("Language");
            helpMenuItem.Text = Localization.Get("Help");
            viewHelpMenuItem.Text = Localization.Get("ViewHelp");
            aboutMenuItem.Text = Localization.Get("About");
            addAreaButton.Text = Localization.Get("Add");
            removeAreaButton.Text = Localization.Get("Remove");
            moveUpButton.Text = Localization.Get("MoveUp");
            moveDownButton.Text = Localization.Get("MoveDown");
            intervalLabel.Text = Localization.Get("IntervalLabel");
            jitterLabel.Text = Localization.Get("JitterLabel");
            sequenceLabel.Text = Localization.Get("SequenceLabel");
            showOverlayCheckBox.Text = Localization.Get("ShowOverlay");
            hardwareModeCheckBox.Text = Localization.Get("HardwareMode");
            startButton.Text = _isClicking ? Localization.Get("StopClicking") : Localization.Get("StartClicking");
            this.Text = Localization.Get("WindowTitle");

            int selectedIndex = sequenceComboBox.SelectedIndex;
            sequenceComboBox.Items.Clear();
            sequenceComboBox.Items.AddRange(new string[] {
                Localization.Get("SequenceLoop"),
                Localization.Get("SequenceRandom"),
                Localization.Get("SequenceOnce")
            });
            if (selectedIndex >= 0) sequenceComboBox.SelectedIndex = selectedIndex; else if (sequenceComboBox.Items.Count > 0) { sequenceComboBox.SelectedIndex = 0; }

            PerformLayoutLayout();
        }

        // “查看帮助”菜单项的点击事件。
        private void viewHelpMenuItem_Click(object? sender, EventArgs e)
        {
            using (Form tutorialForm = new Form())
            {
                tutorialForm.Text = Localization.Get("HelpTutorialTitle");
                tutorialForm.Size = new Size(600, 500);
                tutorialForm.StartPosition = FormStartPosition.CenterParent;
                tutorialForm.FormBorderStyle = FormBorderStyle.Sizable;
                tutorialForm.MaximizeBox = true;
                tutorialForm.MinimizeBox = true;

                Panel buttonPanel = new Panel();
                buttonPanel.Height = 40;
                buttonPanel.Dock = DockStyle.Bottom;

                Button closeButton = new Button();
                closeButton.Text = Localization.Get("CloseButton");
                closeButton.DialogResult = DialogResult.OK;
                closeButton.Size = new Size(100, 25);
                closeButton.Location = new Point((buttonPanel.ClientSize.Width - closeButton.Width) / 2, (buttonPanel.ClientSize.Height - closeButton.Height) / 2);
                closeButton.Anchor = AnchorStyles.None;
                buttonPanel.Controls.Add(closeButton);

                RichTextBox helpTextBox = new RichTextBox();
                helpTextBox.Dock = DockStyle.Fill;
                helpTextBox.ReadOnly = true;
                helpTextBox.Text = Localization.Get("HelpTutorialContent");
                helpTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
                helpTextBox.Font = new Font("Microsoft YaHei UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                helpTextBox.BorderStyle = BorderStyle.None;

                tutorialForm.Controls.Add(helpTextBox);
                tutorialForm.Controls.Add(buttonPanel);
                tutorialForm.AcceptButton = closeButton;

                tutorialForm.ShowDialog(this);
            }
        }

        // “保存配置”菜单项的点击事件。
        private void saveConfigMenuItem_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = Localization.Get("JsonFilter");
                saveFileDialog.Title = Localization.Get("SaveConfigTitle");
                if (saveFileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    var configData = new ConfigData
                    {
                        ClickAreas = _clickAreas,
                        Interval = intervalNumericUpDown.Value,
                        Jitter = jitterNumericUpDown.Value,
                        SequenceIndex = sequenceComboBox.SelectedIndex,
                        ShowOverlay = showOverlayCheckBox.Checked,
                        HardwareMode = hardwareModeCheckBox.Checked
                    };

                    try
                    {
                        string jsonString = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(saveFileDialog.FileName, jsonString);
                        MessageBox.Show(Localization.Get("ConfigSavedSuccess"), Localization.Get("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format(Localization.Get("SaveConfigError"), ex.Message), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // “加载配置”菜单项的点击事件。
        private void loadConfigMenuItem_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = Localization.Get("JsonFilter");
                openFileDialog.Title = Localization.Get("LoadConfigTitle");
                if (openFileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
                {
                    try
                    {
                        string jsonString = File.ReadAllText(openFileDialog.FileName);
                        var configData = JsonSerializer.Deserialize<ConfigData>(jsonString);

                        if (configData != null)
                        {
                            // Validate the entire file before changing the current UI or configuration.
                            configData.Validate(
                                intervalNumericUpDown.Minimum,
                                intervalNumericUpDown.Maximum,
                                jitterNumericUpDown.Minimum,
                                jitterNumericUpDown.Maximum,
                                sequenceComboBox.Items.Count);

                            // Apply only after every field has passed validation.
                            _clickAreas = configData.ClickAreas;
                            intervalNumericUpDown.Value = configData.Interval;
                            jitterNumericUpDown.Value = configData.Jitter;
                            sequenceComboBox.SelectedIndex = configData.SequenceIndex;
                            showOverlayCheckBox.Checked = configData.ShowOverlay;
                            hardwareModeCheckBox.Checked = configData.HardwareMode;

                            RefreshAreaList();
                            MessageBox.Show(Localization.Get("ConfigLoadedSuccess"), Localization.Get("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(string.Format(Localization.Get("LoadConfigError"), "Invalid config file format."), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format(Localization.Get("LoadConfigError"), ex.Message), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // “关于”菜单项的点击事件。
        private void aboutMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(Localization.Get("AboutMessage"), Localization.Get("About"));
        }

        // 刷新点击区域列表的显示。
        private void RefreshAreaList()
        {
            int selectedIndex = areasListBox.SelectedIndex;
            areasListBox.Items.Clear();
            foreach (var area in _clickAreas)
            {
                areasListBox.Items.Add(area);
            }
            if(selectedIndex != -1 && selectedIndex < areasListBox.Items.Count)
            {
                areasListBox.SelectedIndex = selectedIndex;
            }
            _overlayForm.AreasToDraw = new List<ClickArea>(_clickAreas);
            _overlayForm.Invalidate();
        }

        // “添加”按钮的点击事件。
        private void addAreaButton_Click(object? sender, EventArgs e)
        {
            this.Hide();
            _overlayForm.Hide();
            System.Threading.Thread.Sleep(200);
            using (var selectionForm = new SelectionForm())
            {
                if (selectionForm.ShowDialog() == DialogResult.OK)
                {
                    this.Show();
                    string label = Interaction.InputBox(Localization.Get("SetLabelPrompt"), Localization.Get("SetLabelTitle"), Localization.Get("AreaLabelDefault") + (_clickAreas.Count + 1));
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        _clickAreas.Add(new ClickArea { Area = selectionForm.SelectedArea, Label = label });
                        RefreshAreaList();
                    }
                }
                else
                {
                    this.Show();
                }
            }
            _overlayForm.Show();
            _overlayForm.Invalidate();
        }

        // “移除”按钮的点击事件。
        private void removeAreaButton_Click(object? sender, EventArgs e)
        {
            if (areasListBox.SelectedItem != null)
            {
                _clickAreas.Remove((ClickArea)areasListBox.SelectedItem);
                RefreshAreaList();
            }
        }

        // “上移”按钮的点击事件。
        private void moveUpButton_Click(object? sender, EventArgs e)
        {
            int index = areasListBox.SelectedIndex;
            if (index > 0)
            {
                var item = _clickAreas[index];
                _clickAreas.RemoveAt(index);
                _clickAreas.Insert(index - 1, item);
                RefreshAreaList();
                areasListBox.SelectedIndex = index - 1;
            }
        }

        // “下移”按钮的点击事件。
        private void moveDownButton_Click(object? sender, EventArgs e)
        {
            int index = areasListBox.SelectedIndex;
            if (index != -1 && index < _clickAreas.Count - 1)
            {
                var item = _clickAreas[index];
                _clickAreas.RemoveAt(index);
                _clickAreas.Insert(index + 1, item);
                RefreshAreaList();
                areasListBox.SelectedIndex = index + 1;
            }
        }

        // “开始/停止”按钮的点击事件。
        private void startButton_Click(object? sender, EventArgs e)
        {
            if (_isClicking) { StopClicking(); } else { StartClicking(); }
        }

        // 开始自动点击。
        private void StartClicking()
        {
            if (_clickAreas.Count == 0) { MessageBox.Show(Localization.Get("AddAreaFirst")); return; }

            _isClicking = true;
            _currentClickIndex = 0;
            startButton.Text = Localization.Get("StopClicking");
            SetUIEnabled(false);
            
            if (!showOverlayCheckBox.Checked)
            {
                _overlayForm.Hide();
            }

            SetHook();

            clickTimer_Tick(this, EventArgs.Empty);
        }

        // 停止自动点击。
        private void StopClicking()
        {
            if (!_isClicking) return;

            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            _clickTimer.Stop();
            _isClicking = false;
            startButton.Text = Localization.Get("StartClicking");
            SetUIEnabled(true);
            if (showOverlayCheckBox.Checked)
            {
                _overlayForm.Show();
                _overlayForm.Invalidate();
            }
            else
            {
                _overlayForm.Hide();
            }
        }

        // 在点击过程中禁用或启用UI控件。
        private void SetUIEnabled(bool isEnabled)
        {
            areasListBox.Enabled = isEnabled;
            addAreaButton.Enabled = isEnabled;
            removeAreaButton.Enabled = isEnabled;
            moveUpButton.Enabled = isEnabled;
            moveDownButton.Enabled = isEnabled;
            intervalNumericUpDown.Enabled = isEnabled;
            jitterNumericUpDown.Enabled = isEnabled;
            sequenceComboBox.Enabled = isEnabled;
            showOverlayCheckBox.Enabled = isEnabled;
            hardwareModeCheckBox.Enabled = isEnabled;
            menuStrip.Enabled = isEnabled;
        }

        // 定时器事件，用于执行点击。
        private void clickTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isClicking) return;

            _clickTimer.Stop();

            ClickArea? areaToClick = GetNextArea();
            if (areaToClick == null) { StopClicking(); return; }

            try
            {
                _mouseController.PerformRandomClickInArea(areaToClick.Area, hardwareModeCheckBox.Checked);
            }
            catch (Exception ex)
            {
                // Surface native input failures without leaving the timer or global hook active.
                StopClicking();
                MessageBox.Show(
                    $"鼠标输入失败，自动点击已停止。\\n{ex.Message}",
                    this.Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (_isClicking)
            {
                int baseInterval = (int)intervalNumericUpDown.Value;
                int jitter = (int)jitterNumericUpDown.Value;
                int randomJitter = _random.Next(-jitter, jitter + 1);
                _clickTimer.Interval = Math.Max(10, baseInterval + randomJitter);
                _clickTimer.Start();
            }
        }
        
        // 根据序列模式获取下一个点击区域。
        private ClickArea? GetNextArea()
        {
            if (_clickAreas.Count == 0) return null;
            string mode = sequenceComboBox.Text;
            
            if (mode == Localization.Get("SequenceRandom"))
            {
                return _clickAreas[_random.Next(0, _clickAreas.Count)];
            }
            
            if (_currentClickIndex >= _clickAreas.Count)
            {
                if (mode == Localization.Get("SequenceLoop")) { _currentClickIndex = 0; } else { return null; }
            }
            
            var area = _clickAreas[_currentClickIndex];
            _currentClickIndex++;
            return area;
        }
    }
}
