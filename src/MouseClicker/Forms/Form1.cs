using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.Http;

namespace MouseClicker
{
    public partial class Form1 : Form
    {
        // 全局键盘处理已隔离在 GlobalHotkeyManager.cs 中。
        private GlobalHotkeyManager? _hotkeyManager;
        private Exception? _hotkeyInitializationError;
        private bool _hotkeyFailureShown;

        // --- 核心逻辑字段 ---
        private readonly MouseController _mouseController;
        private readonly SessionConfigStore? _sessionConfigStore;
        private Exception? _sessionRestoreError;
        private bool _sessionSaveBlocked;
        private List<ClickArea> _clickAreas = new List<ClickArea>();
        private OverlayForm _overlayForm = null!;
        private ClickArea? _hoveredArea;
        private Point? _hoverPoint;
        private int _hoverRevision;
        private ClickArea? _activeClickArea;
        private volatile bool _isSelectingArea;
        private volatile int _modalDepth;

        // --- 定时器和状态字段 ---
        private CancellationTokenSource? _clickCancellation;
        private Task? _clickSession;
        private long _clickSessionId;
        private bool _conditionSession;
        private ToolStripMenuItem conditionMenuItem = null!;
        private volatile bool _isClicking = false;

        private readonly HttpClient _updateHttpClient;
        private readonly bool _ownsUpdateHttpClient;
        private readonly GitHubUpdateService _updateService;
        private readonly UpdatePreferencesStore _updatePreferences;
        private readonly CancellationTokenSource _updateCancellation = new();
        private readonly Version _currentVersion;
        private Task? _updateCheckTask;
        private bool _updateCheckStarted;
        private bool _checkingForUpdates = true;
        private bool _updateCheckSucceeded;
        private volatile bool _isClosing;
        private bool _updateResourcesDisposed;
        private UpdateInfo? _availableUpdate;
        private Version? _skippedUpdateVersion;

        // --- UI 控件字段 ---
        private MenuStrip menuStrip = null!;
        private ToolStripMenuItem fileMenuItem = null!, languageMenuItem = null!, helpMenuItem = null!, aboutMenuItem = null!;
        private HelpForm? _helpForm;
        private ToolStripMenuItem saveConfigMenuItem = null!, loadConfigMenuItem = null!, viewHelpMenuItem = null!, hotkeySettingsMenuItem = null!;
        private ContextMenuStrip areaClickButtonMenu = null!;
        private ToolStripMenuItem leftClickMenuItem = null!, rightClickMenuItem = null!;
        private ListBox areasListBox = null!;
        private Button addAreaButton = null!, removeAreaButton = null!, moveUpButton = null!, moveDownButton = null!;
        private Label intervalLabel = null!, jitterLabel = null!, sequenceLabel = null!;
        private Label jitterExceedsIntervalHintLabel = null!;
        private NumericUpDown intervalNumericUpDown = null!, jitterNumericUpDown = null!;
        private ComboBox sequenceComboBox = null!;
        private CheckBox showOverlayCheckBox = null!;
        private CheckBox showDetectionOverlayCheckBox = null!;
        private CheckBox highlightCurrentAreaCheckBox = null!;
        private CheckBox hardwareModeCheckBox = null!;
        private Button startButton = null!;
        private LinkLabel updateAvailableLinkLabel = null!;

        // 主窗口构造函数
        public Form1() : this(null, null, sessionConfigStore: new SessionConfigStore()) { }

        internal Form1(HttpClient? updateClient, UpdatePreferencesStore? preferences, Version? currentVersion = null, SessionConfigStore? sessionConfigStore = null)
        {
            _sessionConfigStore = sessionConfigStore;
            _ownsUpdateHttpClient = updateClient == null;
            _updateHttpClient = updateClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _updateService = new GitHubUpdateService(_updateHttpClient);
            _updatePreferences = preferences ?? new UpdatePreferencesStore();
            _currentVersion = UpdateVersion.Normalize(currentVersion ?? typeof(Form1).Assembly.GetName().Version ?? new Version(0, 0, 0));
            InitializeComponent();
            _mouseController = new MouseController(new Win32MouseInput());


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

            try
            {
                _hotkeyManager = new GlobalHotkeyManager();
                _hotkeyManager.KeyPressed += HotkeyManager_KeyPressed;
                _hotkeyManager.ShouldSuppressEscape = () => _isClicking;
            }
            catch (Win32Exception ex)
            {
                // 钩子安装失败时保持其余功能可用，并在窗口显示后提示一次。
                _hotkeyManager = null;
                _hotkeyInitializationError = ex;
                hotkeySettingsMenuItem.Enabled = false;
            }

            UpdateAppearanceStrings();
            PerformLayoutLayout();
            _overlayForm = new OverlayForm();
            UpdateOverlayState();
            RestoreSessionConfig();
            this.Disposed += (_, _) => DisposeUpdateResources();
        }


        internal event EventHandler? UpdateStateChanged;
        internal Version CurrentVersion => _currentVersion;
        internal string DisplayVersion => _currentVersion.ToString(_currentVersion.Revision > 0 ? 4 : 3);
        internal bool CheckingForUpdates => _checkingForUpdates;
        internal bool UpdateCheckSucceeded => _updateCheckSucceeded;
        internal UpdateInfo? AvailableUpdate => _availableUpdate;
        internal Version? SkippedUpdateVersion => _skippedUpdateVersion;
        internal Task? UpdateCheckTask => _updateCheckTask;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_sessionRestoreError != null)
            {
                var error = _sessionRestoreError;
                _sessionRestoreError = null;
                ShowMessageBox(string.Format(Localization.Get("LoadConfigError"), error.Message),
                    Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            var hotkeyError = _hotkeyInitializationError;
            if (hotkeyError != null && !_hotkeyFailureShown)
            {
                _hotkeyFailureShown = true;
                using (BeginModalScope())
                {
                    MessageBox.Show(this,
                        string.Format(Localization.Get("HotkeyUnavailable"), hotkeyError.Message),
                        Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            if (_updateCheckStarted) return;
            _updateCheckStarted = true;
            _updateCheckTask = CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            bool succeeded = false;
            UpdateInfo? availableUpdate = null;
            Version? skippedVersion = null;
            try
            {
                var result = await _updateService.CheckForUpdateAsync(_currentVersion, _updateCancellation.Token);
                succeeded = result.Succeeded;
                if (succeeded && result.Update != null)
                {
                    if (_updatePreferences.IsSkipped(result.Update.Version))
                        skippedVersion = result.Update.Version;
                    else
                        availableUpdate = result.Update;
                }
            }
            catch (OperationCanceledException) when (_isClosing || _updateCancellation.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                // 启动检查是可选功能：失败时只更新“关于”中的状态，绝不弹窗。
                succeeded = false;
            }

            if (_isClosing || IsDisposed || Disposing) return;
            _checkingForUpdates = false;
            _updateCheckSucceeded = succeeded;
            _availableUpdate = availableUpdate;
            _skippedUpdateVersion = skippedVersion;
            RefreshUpdateState();
        }

        private void RefreshUpdateState()
        {
            updateAvailableLinkLabel.Text = Localization.Get("UpdateAvailable");
            updateAvailableLinkLabel.Enabled = !_isClicking;
            PerformLayoutLayout();
            UpdateStateChanged?.Invoke(this, EventArgs.Empty);
        }

        internal bool SkipAvailableUpdate(IWin32Window owner)
        {
            if (_availableUpdate == null) return false;
            var version = _availableUpdate.Version;
            try
            {
                // 只有成功保存偏好设置后，才隐藏该提示。
                _updatePreferences.Skip(version);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner,
                    string.Format(Localization.Get("UpdatePreferencesError"), ex.Message),
                    Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            _availableUpdate = null;
            _skippedUpdateVersion = version;
            RefreshUpdateState();
            return true;
        }

        internal static void OpenExternalLink(Uri uri, IWin32Window owner)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, string.Format(Localization.Get("OpenLinkError"), ex.Message),
                    Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.Cancel) return;
            SaveSessionConfig();
            _isClosing = true;
            _updateCancellation.Cancel();
            StopClicking();
            _clickCancellation?.Cancel();
            _overlayForm.Close();
        }

        private void DisposeUpdateResources()
        {
            if (_updateResourcesDisposed) return;
            _updateResourcesDisposed = true;
            _isClosing = true;
            _updateCancellation.Cancel();
            _updateCancellation.Dispose();
            if (_ownsUpdateHttpClient) _updateHttpClient.Dispose();
            _clickCancellation?.Cancel();
            _helpForm?.Dispose();
            areaClickButtonMenu?.Dispose();
            if (!_overlayForm.IsDisposed) _overlayForm.Dispose();
            _hotkeyManager?.Dispose();
        }

        private void HotkeyManager_KeyPressed(Keys key)
        {
            if (_hotkeyManager == null || _isClosing || IsDisposed || Disposing || !IsHandleCreated) return;
            // 在按下瞬间记录模态状态；否则按键排队期间对话框若被关闭，排队的启动动作会漏过保护。
            bool modalAtPress = _isSelectingArea || _modalDepth > 0 ||
                                (!InvokeRequired && HasOpenModalForm());
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (_isClosing || IsDisposed || Disposing) return;
                    if (key == Keys.Escape && _isClicking)
                    {
                        StopClicking();
                        return;
                    }
                    var manager = _hotkeyManager;
                    if (manager == null || key != manager.Hotkey) return;
                    switch (ResolveHotkeyAction(_isClicking, _isSelectingArea, _modalDepth,
                        modalAtPress || HasOpenModalForm()))
                    {
                        case HotkeyAction.Start:
                            StartClicking();
                            break;
                        case HotkeyAction.Stop:
                            StopClicking();
                            break;
                    }
                }));
            }
            catch (InvalidOperationException)
            {
                // 窗口关闭时，全局热键事件可能仍在排队。
            }
        }

        internal enum HotkeyAction
        {
            None,
            Start,
            Stop
        }

        /// <summary>
        /// 根据当前界面状态决定热键应开始、停止还是不处理。
        /// 应用自身的模态界面打开时只允许停止，避免在对话框后面开始点击。
        /// </summary>
        internal static HotkeyAction ResolveHotkeyAction(bool isClicking, bool isSelectingArea, int modalDepth, bool hasOpenModalForm)
        {
            if (isClicking) return HotkeyAction.Stop;
            if (isSelectingArea || modalDepth > 0 || hasOpenModalForm) return HotkeyAction.None;
            return HotkeyAction.Start;
        }

        // 必须在 UI 线程调用：Application.OpenForms 不是线程安全的。
        private static bool HasOpenModalForm()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Modal) return true;
            }
            return false;
        }

        /// <summary>
        /// 记录应用自身模态界面的嵌套深度；进入时递增，释放时递减。
        /// 仅在 UI 线程使用。
        /// </summary>
        private readonly struct ModalScope : IDisposable
        {
            private readonly Form1 _owner;

            public ModalScope(Form1 owner)
            {
                _owner = owner;
                owner._modalDepth++;
            }

            public void Dispose() => _owner._modalDepth--;
        }

        private ModalScope BeginModalScope() => new ModalScope(this);

        // 显示消息框期间同样视为模态界面，阻止热键在后台开始点击。
        private void ShowMessageBox(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (BeginModalScope())
            {
                MessageBox.Show(message, caption, buttons, icon);
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
            hotkeySettingsMenuItem = new ToolStripMenuItem();
            settingsMenuItem = new ToolStripMenuItem();
            areaClickButtonMenu = new ContextMenuStrip();
            leftClickMenuItem = new ToolStripMenuItem();
            rightClickMenuItem = new ToolStripMenuItem();
            conditionMenuItem = new ToolStripMenuItem();
            conditionMenuItem.Click += (_, _) => EditSelectedCondition();
            areaClickButtonMenu.Items.AddRange(new ToolStripItem[] { leftClickMenuItem, rightClickMenuItem, new ToolStripSeparator(), conditionMenuItem });
            areaClickButtonMenu.Opening += (_, e) =>
            {
                if (areasListBox.SelectedItem is not ClickArea selectedArea)
                {
                    e.Cancel = true;
                    return;
                }
                leftClickMenuItem.Checked = !selectedArea.RightButton;
                rightClickMenuItem.Checked = selectedArea.RightButton;
                leftClickMenuItem.Enabled = rightClickMenuItem.Enabled = conditionMenuItem.Enabled = _clickSession == null;
            };
            leftClickMenuItem.Click += (_, _) => SetSelectedAreaRightButton(false);
            rightClickMenuItem.Click += (_, _) => SetSelectedAreaRightButton(true);
            languageMenuItem = new ToolStripMenuItem();
            foreach (var language in Localization.SupportedLanguages)
            {
                string languageCode = language.Key;
                var item = new ToolStripMenuItem(language.Value) { Tag = languageCode };
                item.Click += (_, _) => ChangeLanguage(languageCode);
                languageMenuItem.DropDownItems.Add(item);
            }
            helpMenuItem = new ToolStripMenuItem();
            viewHelpMenuItem = new ToolStripMenuItem();
            aboutMenuItem = new ToolStripMenuItem();
            var toggleSelectionListBox = new ToggleSelectionListBox();
            this.areasListBox = toggleSelectionListBox;
            this.addAreaButton = new Button();
            this.removeAreaButton = new Button();
            this.moveUpButton = new Button();
            this.moveDownButton = new Button();
            this.intervalLabel = new Label { AutoSize = true };
            this.intervalNumericUpDown = new NumericUpDown { Maximum = 600000, Minimum = 10, Value = 1000 };
            this.jitterLabel = new Label { AutoSize = true };
            this.jitterNumericUpDown = new NumericUpDown { Maximum = 10000, Value = 100 };
            this.jitterExceedsIntervalHintLabel = new Label { AutoSize = true, ForeColor = SystemColors.GrayText, Visible = false };
            this.sequenceLabel = new Label { AutoSize = true };
            this.sequenceComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.showOverlayCheckBox = new CheckBox { AutoSize = true, Checked = true };
            this.showDetectionOverlayCheckBox = new CheckBox { AutoSize = true };
            this.highlightCurrentAreaCheckBox = new CheckBox { AutoSize = true, Checked = true };
            this.hardwareModeCheckBox = new CheckBox { AutoSize = true, Checked = false };
            this.startButton = new Button { Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold) };
            this.updateAvailableLinkLabel = new LinkLabel
            {
                AutoSize = true, Visible = false,
                Font = new Font(this.Font.FontFamily, Math.Max(7.0f, this.Font.Size - 1), FontStyle.Regular)
            };
            updateAvailableLinkLabel.LinkClicked += (_, _) => ShowAbout();

            // 将控件添加到窗体
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            fileMenuItem.DropDownItems.Add(saveConfigMenuItem);
            fileMenuItem.DropDownItems.Add(loadConfigMenuItem);
            settingsMenuItem.DropDownItems.Add(hotkeySettingsMenuItem);
            areasListBox.ContextMenuStrip = areaClickButtonMenu;
            areasListBox.DoubleClick += (_, _) => EditSelectedCondition();
            helpMenuItem.DropDownItems.Add(viewHelpMenuItem);
            this.menuStrip.Items.Add(fileMenuItem);
            this.menuStrip.Items.Add(settingsMenuItem);
            this.menuStrip.Items.Add(languageMenuItem);
            this.menuStrip.Items.Add(helpMenuItem);
            this.menuStrip.Items.Add(aboutMenuItem);
            this.Controls.Add(this.updateAvailableLinkLabel);
            this.Controls.Add(this.areasListBox);
            this.Controls.Add(this.addAreaButton);
            this.Controls.Add(this.removeAreaButton);
            this.Controls.Add(this.moveUpButton);
            this.Controls.Add(this.moveDownButton);
            this.Controls.Add(this.intervalLabel);
            this.Controls.Add(this.intervalNumericUpDown);
            this.Controls.Add(this.jitterLabel);
            this.Controls.Add(this.jitterNumericUpDown);
            this.Controls.Add(this.jitterExceedsIntervalHintLabel);
            this.Controls.Add(this.sequenceLabel);
            this.Controls.Add(this.sequenceComboBox);
            this.Controls.Add(this.showOverlayCheckBox);
            this.Controls.Add(this.showDetectionOverlayCheckBox);
            this.Controls.Add(this.highlightCurrentAreaCheckBox);
            this.Controls.Add(this.hardwareModeCheckBox);
            this.Controls.Add(this.startButton);

            // 附加事件处理器
            InitializeAppearance();
            saveConfigMenuItem.Click += new EventHandler(saveConfigMenuItem_Click);
            loadConfigMenuItem.Click += new EventHandler(loadConfigMenuItem_Click);
            hotkeySettingsMenuItem.Click += new EventHandler(hotkeySettingsMenuItem_Click);
            areasListBox.MouseDown += areasListBox_MouseDown;
            viewHelpMenuItem.Click += new EventHandler(viewHelpMenuItem_Click);
            aboutMenuItem.Click += new EventHandler(aboutMenuItem_Click);
            addAreaButton.Click += new EventHandler(addAreaButton_Click);
            removeAreaButton.Click += new EventHandler(removeAreaButton_Click);
            moveUpButton.Click += new EventHandler(moveUpButton_Click);
            moveDownButton.Click += new EventHandler(moveDownButton_Click);
            startButton.Click += new EventHandler(startButton_Click);
            intervalNumericUpDown.ValueChanged += TimingValueChanged;
            jitterNumericUpDown.ValueChanged += TimingValueChanged;
            toggleSelectionListBox.SelectionClearedByClick += (s, e) => ClearHoveredArea();
            areasListBox.MouseMove += areasListBox_MouseMove;
            areasListBox.MouseWheel += areasListBox_MouseWheel;
            areasListBox.MouseLeave += (s, e) => ClearHoveredArea();
            areasListBox.SelectedIndexChanged += (s, e) =>
            {
                UpdateOverlayState();
                QueueHoverRefresh();
            };
            showOverlayCheckBox.CheckedChanged += (s, e) => UpdateOverlayState();
            showDetectionOverlayCheckBox.CheckedChanged += (s, e) => UpdateOverlayState();
            highlightCurrentAreaCheckBox.CheckedChanged += (s, e) => UpdateOverlayState();
        }

        // 动态调整UI控件的布局。
        private void PerformLayoutLayout()
        {
            LayoutAppearance();
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
            UpdateAppearanceStrings();
            saveConfigMenuItem.Text = Localization.Get("SaveConfig");
            loadConfigMenuItem.Text = Localization.Get("LoadConfig");
            hotkeySettingsMenuItem.Text = Localization.Get("HotkeySettings");
            conditionMenuItem.Text = Localization.Get("ConditionEdit");
            leftClickMenuItem.Text = Localization.Get("LeftMouseButton");
            rightClickMenuItem.Text = Localization.Get("RightMouseButton");
            languageMenuItem.Text = Localization.Get("Language");
            helpMenuItem.Text = Localization.Get("Help");
            viewHelpMenuItem.Text = Localization.Get("ViewHelp");
            _helpForm?.ReloadCatalog(HelpContent.GetCatalog(Localization.Language));
            aboutMenuItem.Text = Localization.Get("About");
            updateAvailableLinkLabel.Text = Localization.Get("UpdateAvailable");
            foreach (ToolStripMenuItem item in languageMenuItem.DropDownItems)
                item.Checked = string.Equals(item.Tag as string, Localization.Language, StringComparison.OrdinalIgnoreCase);
            addAreaButton.Text = Localization.Get("Add");
            removeAreaButton.Text = Localization.Get("Remove");
            moveUpButton.Text = Localization.Get("MoveUp");
            moveDownButton.Text = Localization.Get("MoveDown");
            intervalLabel.Text = Localization.Get("IntervalLabel");
            jitterLabel.Text = Localization.Get("JitterLabel");
            jitterExceedsIntervalHintLabel.Text = Localization.Get("JitterExceedsIntervalHint");
            sequenceLabel.Text = Localization.Get("SequenceLabel");
            showOverlayCheckBox.Text = Localization.Get("ShowOverlay");
            showDetectionOverlayCheckBox.Text = Localization.Get("ShowDetectionOverlay");
            highlightCurrentAreaCheckBox.Text = Localization.Get("HighlightCurrentArea");
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
            UpdateStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void hotkeySettingsMenuItem_Click(object? sender, EventArgs e)
        {
            var manager = _hotkeyManager;
            if (manager == null) return;
            using var settings = new HotkeySettingsForm(manager);
            string previousHint = runHint.Text;
            using (BeginModalScope()) settings.ShowDialog(this);
            UpdateAppearanceStrings();
            if (runHint.Text != previousHint) PerformLayoutLayout();
        }

        private void areasListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            int index = areasListBox.IndexFromPoint(e.Location);
            if (index >= 0 && index < areasListBox.Items.Count)
                areasListBox.SelectedIndex = index;
        }

        private void SetSelectedAreaRightButton(bool rightButton)
        {
            if (areasListBox.SelectedItem is not ClickArea area || _isClicking) return;
            area.RightButton = rightButton;
            RefreshAreaList();
        }

        // “查看帮助”菜单项的点击事件。
        private void viewHelpMenuItem_Click(object? sender, EventArgs e) => ShowHelp(HelpTopic.QuickStart);

        internal HelpForm? OpenHelpForm => _helpForm;

        internal void ShowHelp(HelpTopic topic)
        {
            if (_isClosing || IsDisposed || Disposing) return;
            if (_helpForm == null || _helpForm.IsDisposed)
            {
                _helpForm = new HelpForm(HelpContent.GetCatalog(Localization.Language), topic, () => _isClicking);
                _helpForm.Show(this);
            }
            else
            {
                _helpForm.ShowTopic(topic);
                if (_helpForm.WindowState == FormWindowState.Minimized) _helpForm.WindowState = FormWindowState.Normal;
                _helpForm.Show();
                _helpForm.Activate();
            }
        }


        // “保存配置”菜单项的点击事件。
        private void saveConfigMenuItem_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = Localization.Get("JsonFilter");
                saveFileDialog.Title = Localization.Get("SaveConfigTitle");
                DialogResult dialogResult;
                using (BeginModalScope()) dialogResult = saveFileDialog.ShowDialog();
                if (dialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    var configData = CaptureConfig();

                    try
                    {
                        configData.Validate(intervalNumericUpDown.Minimum, intervalNumericUpDown.Maximum,
                            jitterNumericUpDown.Minimum, jitterNumericUpDown.Maximum, sequenceComboBox.Items.Count);
                        string jsonString = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(saveFileDialog.FileName, jsonString);
                        ShowMessageBox(Localization.Get("ConfigSavedSuccess"), Localization.Get("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox(string.Format(Localization.Get("SaveConfigError"), ex.Message), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                DialogResult dialogResult;
                using (BeginModalScope()) dialogResult = openFileDialog.ShowDialog();
                if (dialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
                {
                    try
                    {
                        string jsonString = File.ReadAllText(openFileDialog.FileName);
                        var configData = JsonSerializer.Deserialize<ConfigData>(jsonString);

                        if (configData != null)
                        {
                            // 在修改当前界面或配置之前，先校验整个文件。
                            ApplyConfig(configData);
                            ShowMessageBox(Localization.Get("ConfigLoadedSuccess"), Localization.Get("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            ShowMessageBox(string.Format(Localization.Get("LoadConfigError"), "Invalid config file format."), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox(string.Format(Localization.Get("LoadConfigError"), ex.Message), Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private ConfigData CaptureConfig() => new ConfigData
        {
            ClickAreas = _clickAreas,
            Interval = intervalNumericUpDown.Value,
            Jitter = jitterNumericUpDown.Value,
            SequenceIndex = sequenceComboBox.SelectedIndex,
            ShowOverlay = showOverlayCheckBox.Checked,
            ShowDetectionOverlay = showDetectionOverlayCheckBox.Checked,
            HighlightCurrentArea = highlightCurrentAreaCheckBox.Checked,
            HardwareMode = hardwareModeCheckBox.Checked
        };

        private void ApplyConfig(ConfigData configData)
        {
            configData.Validate(
                intervalNumericUpDown.Minimum,
                intervalNumericUpDown.Maximum,
                jitterNumericUpDown.Minimum,
                jitterNumericUpDown.Maximum,
                sequenceComboBox.Items.Count);

            // 只有所有字段都通过校验后才应用。
            ClearHoveredArea();
            _activeClickArea = null;
            _clickAreas = configData.ClickAreas;
            intervalNumericUpDown.Value = configData.Interval;
            jitterNumericUpDown.Value = configData.Jitter;
            sequenceComboBox.SelectedIndex = configData.SequenceIndex;
            showOverlayCheckBox.Checked = configData.ShowOverlay;
            showDetectionOverlayCheckBox.Checked = configData.ShowDetectionOverlay;
            highlightCurrentAreaCheckBox.Checked = configData.HighlightCurrentArea;
            hardwareModeCheckBox.Checked = configData.HardwareMode;

            RefreshAreaList();
        }

        private void RestoreSessionConfig()
        {
            if (_sessionConfigStore == null) return;
            try
            {
                var config = _sessionConfigStore.Load();
                if (config != null) ApplyConfig(config);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
            {
                _sessionRestoreError = ex;
                try { _sessionConfigStore.BackupUnreadableConfig(); }
                catch (Exception backupError) when (backupError is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    // 无法备份时保留原文件，不要覆盖。
                    _sessionSaveBlocked = true;
                    _sessionRestoreError = new IOException(ex.Message + Environment.NewLine + backupError.Message, ex);
                }
            }
        }

        private void SaveSessionConfig()
        {
            if (_sessionConfigStore == null || _sessionSaveBlocked) return;
            try
            {
                var config = CaptureConfig();
                config.Validate(intervalNumericUpDown.Minimum, intervalNumericUpDown.Maximum,
                    jitterNumericUpDown.Minimum, jitterNumericUpDown.Maximum, sequenceComboBox.Items.Count);
                _sessionConfigStore.Save(config);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
            {
                ShowMessageBox(string.Format(Localization.Get("SaveConfigError"), ex.Message),
                    Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAbout()
        {
            using var about = new AboutForm(this);
            using (BeginModalScope()) about.ShowDialog(this);
        }

        // “关于”菜单项的点击事件。
        private void aboutMenuItem_Click(object? sender, EventArgs e)
        {
            ShowAbout();
        }

        // 刷新点击区域列表的显示。
        private void RefreshAreaList()
        {
            int previousIndex = areasListBox.SelectedIndex;
            var selected = areasListBox.SelectedItem as ClickArea;
            ClearHoveredArea();
            if (!_clickAreas.Any(area => ReferenceEquals(area, _activeClickArea))) _activeClickArea = null;
            areasListBox.BeginUpdate();
            try
            {
                areasListBox.Items.Clear();
                foreach (var area in _clickAreas) areasListBox.Items.Add(area);
                int selectedIndex = _clickAreas.FindIndex(area => ReferenceEquals(area, selected));
                if (selectedIndex < 0 && previousIndex >= 0 && previousIndex < _clickAreas.Count) selectedIndex = previousIndex;
                areasListBox.SelectedIndex = selectedIndex;
            }
            finally { areasListBox.EndUpdate(); }
            _overlayForm.AreasToDraw = new List<ClickArea>(_clickAreas);
            _overlayForm.Invalidate();
            UpdateOverlayState();
        }

        private void areasListBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isClicking || _isClosing) { ClearHoveredArea(); return; }
            _hoverPoint = e.Location;
            _hoverRevision++;
            UpdateHoveredAreaAtPoint(e.Location);
        }

        private void areasListBox_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (_isClicking || _isClosing || !areasListBox.ClientRectangle.Contains(e.Location))
            {
                ClearHoveredArea();
                return;
            }
            _hoverPoint = e.Location;
            SetHoveredArea(null);
            QueueHoverRefresh();
        }

        private void ClearHoveredArea()
        {
            _hoverPoint = null;
            _hoverRevision++;
            SetHoveredArea(null);
        }

        private void QueueHoverRefresh()
        {
            if (!_hoverPoint.HasValue || _isClicking || _isClosing || IsDisposed || Disposing ||
                !IsHandleCreated || areasListBox.IsDisposed || !areasListBox.IsHandleCreated) return;
            int revision = ++_hoverRevision;
            Point point = _hoverPoint.Value;
            // 原生滚轮和键盘滚动会在其事件触发后才完成。
            BeginInvoke(new Action(() =>
            {
                if (revision != _hoverRevision || !_hoverPoint.HasValue || _isClicking || _isClosing ||
                    IsDisposed || Disposing || !IsHandleCreated || areasListBox.IsDisposed || !areasListBox.IsHandleCreated) return;
                UpdateHoveredAreaAtPoint(point);
            }));
        }

        private void UpdateHoveredAreaAtPoint(Point point)
        {
            int index = areasListBox.ClientRectangle.Contains(point) ? areasListBox.IndexFromPoint(point) : ListBox.NoMatches;
            SetHoveredArea(index >= 0 && index < areasListBox.Items.Count ? areasListBox.Items[index] as ClickArea : null);
        }

        private void SetHoveredArea(ClickArea? area)
        {
            if (ReferenceEquals(_hoveredArea, area)) return;
            _hoveredArea = area;
            UpdateOverlayState();
        }

        private void UpdateOverlayState()
        {
            RefreshAreaActions();
            highlightCurrentAreaCheckBox.Enabled = showOverlayCheckBox.Checked;
            if (_overlayForm == null || _overlayForm.IsDisposed) return;
            _overlayForm.RequireCaptureExclusion = _conditionSession;
            bool flagsChanged = _overlayForm.ShowClickAreas != showOverlayCheckBox.Checked ||
                _overlayForm.ShowDetectionAreas != showDetectionOverlayCheckBox.Checked;
            _overlayForm.ShowClickAreas = showOverlayCheckBox.Checked;
            _overlayForm.ShowDetectionAreas = showDetectionOverlayCheckBox.Checked;
            bool showOverlay = (showOverlayCheckBox.Checked || showDetectionOverlayCheckBox.Checked) && !_isSelectingArea && !_isClosing &&
                (!_conditionSession || _overlayForm.CanShowDuringCondition());
            ClickArea? preview = _hoveredArea ?? areasListBox.SelectedItem as ClickArea;
            if (!_clickAreas.Any(area => ReferenceEquals(area, preview))) preview = null;
            _overlayForm.PreviewArea = showOverlay && showOverlayCheckBox.Checked && !_isClicking ? preview : null;
            _overlayForm.ActiveArea = showOverlay && showOverlayCheckBox.Checked && _isClicking && highlightCurrentAreaCheckBox.Checked ? _activeClickArea : null;
            if (showOverlay)
            {
                if (!_overlayForm.Visible) _overlayForm.Show();
                else if (flagsChanged) _overlayForm.Invalidate();
            }
            else
            {
                _overlayForm.Hide();
            }
        }

        // “添加”按钮的点击事件。
        private void addAreaButton_Click(object? sender, EventArgs e)
        {
            _isSelectingArea = true;
            this.Hide();
            _overlayForm.Hide();
            try
            {
                System.Threading.Thread.Sleep(200);
                using (var selectionForm = new SelectionForm())
                {
                    DialogResult result;
                    using (BeginModalScope()) result = selectionForm.ShowDialog();
                    this.Show();
                    if (result == DialogResult.OK && selectionForm.SelectedArea.Width > 0 && selectionForm.SelectedArea.Height > 0)
                    {
                        string label;
                        using (BeginModalScope())
                        {
                            label = Interaction.InputBox(Localization.Get("SetLabelPrompt"), Localization.Get("SetLabelTitle"), Localization.Get("AreaLabelDefault") + (_clickAreas.Count + 1));
                        }
                        if (!string.IsNullOrWhiteSpace(label))
                        {
                            _clickAreas.Add(new ClickArea { Area = selectionForm.SelectedArea, Label = label });
                            RefreshAreaList();
                        }
                    }
                }
            }
            finally
            {
                _isSelectingArea = false;
                this.Show();
                UpdateOverlayState();
            }
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

        private void EditSelectedCondition()
        {
            if (_clickSession != null || areasListBox.SelectedItem is not ClickArea area) return;
            _isSelectingArea = true;
            UpdateOverlayState();
            try
            {
                using (BeginModalScope())
                using (var dialog = new ConditionSettingsForm(area.Condition, ShowHelp))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        area.Condition = dialog.ResultCondition;
                        RefreshAreaList();
                    }
                }
            }
            catch (Exception ex) { ShowMessageBox(ex.Message, Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { _isSelectingArea = false; UpdateOverlayState(); }
        }

        private void StartClicking()
        {
            if (_clickSession != null || _isClosing) return;
            if (_clickAreas.Count == 0)
            {
                using (BeginModalScope()) MessageBox.Show(Localization.Get("AddAreaFirst"));
                return;
            }
            ClickArea[] snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<ClickArea[]>(JsonSerializer.Serialize(_clickAreas))!;
                foreach (var area in snapshot) area.Condition?.Validate();
            }
            catch (Exception ex) { ShowMessageBox(ex.Message, Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            var options = new ClickSessionOptions { SequenceIndex = sequenceComboBox.SelectedIndex,
                IntervalMs = (int)intervalNumericUpDown.Value, JitterMs = (int)jitterNumericUpDown.Value };
            bool hardwareMode = hardwareModeCheckBox.Checked;
            var cancellation = new CancellationTokenSource();
            _clickCancellation = cancellation;
            var id = ++_clickSessionId;
            _conditionSession = snapshot.Any(area => area.Condition != null);
            _isClicking = true;
            ClearHoveredArea();
            _activeClickArea = null;
            startButton.Text = Localization.Get("StopClicking");
            SetUIEnabled(false);
            UpdateOverlayState();
            _clickSession = Task.CompletedTask;
            _clickSession = RunSessionAsync(snapshot, options, hardwareMode, cancellation, id);
        }

        private async Task RunSessionAsync(ClickArea[] snapshot, ClickSessionOptions options, bool hardwareMode,
            CancellationTokenSource cancellation, long id)
        {
            await Task.Yield();
            Exception? failure = null;
            try
            {
                await Task.Run(async () =>
                {
                    using var detector = new OpenCvConditionDetector();
                    var runner = new ClickSessionRunner(detector);
                    await runner.RunAsync(snapshot, options, async (area, token) =>
                    {
                        token.ThrowIfCancellationRequested();
                        await this.InvokeAsync(() =>
                        {
                            token.ThrowIfCancellationRequested();
                            if (_isClosing || !_isClicking || id != _clickSessionId) throw new OperationCanceledException(token);
                            _mouseController.PerformRandomClickInArea(area.Area, hardwareMode, area.RightButton);
                            var index = Array.IndexOf(snapshot, area);
                            _activeClickArea = index >= 0 && index < _clickAreas.Count ? _clickAreas[index] : null;
                            UpdateOverlayState();
                        }, token);
                    }, (area, detected) =>
                    {
                        if (cancellation.IsCancellationRequested || _isClosing) return;
                        try
                        {
                            BeginInvoke(() =>
                            {
                                if (_isClosing || !_isClicking || id != _clickSessionId) return;
                                Text = Localization.Get("WindowTitle") + " — " + area.Label + " — " +
                                    Localization.Get(detected.HasValue && detected.Value.Score >= (area.Condition?.Threshold ?? 1)
                                    ? "ConditionMatched" : "ConditionWaiting") + (detected.HasValue ? " " + detected.Value.Score.ToString("F3") : string.Empty);
                            });
                        }
                        catch (InvalidOperationException) when (_isClosing || IsDisposed || Disposing) { }
                    }, cancellation.Token);
                }, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested || _isClosing) { }
            catch (Exception ex) { failure = ex; }
            finally
            {
                cancellation.Dispose();
                if (id == _clickSessionId)
                {
                    _clickCancellation = null;
                    _clickSession = null;
                    _isClicking = false;
                    _conditionSession = false;
                    _activeClickArea = null;
                    if (!_isClosing && !IsDisposed && !Disposing)
                    {
                        Text = Localization.Get("WindowTitle");
                        startButton.Enabled = true;
                        startButton.Text = Localization.Get("StartClicking");
                        SetUIEnabled(true);
                    }
                }
            }
            if (failure != null && !_isClosing && !IsDisposed && !Disposing)
                ShowMessageBox(failure.Message, Localization.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void StopClicking()
        {
            _isClicking = false;
            _activeClickArea = null;
            _clickCancellation?.Cancel();
            if (_clickSession != null && !_isClosing)
            {
                startButton.Enabled = false;
                startButton.Text = Localization.Get("ConditionStopped");
            }
            UpdateOverlayState();
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
            showOverlayCheckBox.Enabled = true;
            hardwareModeCheckBox.Enabled = isEnabled;
            menuStrip.Enabled = isEnabled;
            updateAvailableLinkLabel.Enabled = isEnabled;
            RefreshAreaActions();
            UpdateOverlayState();
        }

        // 负等待时间取绝对值，同时保留 10 毫秒下限。
        internal static int CalculateClickInterval(int baseInterval, int randomJitter)
        {
            return Math.Max(10, Math.Abs(baseInterval + randomJitter));
        }

    }
}
