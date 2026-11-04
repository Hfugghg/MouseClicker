using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WpfMessageBox = System.Windows.MessageBox;
using MenuItem = System.Windows.Controls.MenuItem;

namespace MouseClicker.Wpf;

/// <summary>WPF 主窗口负责交互和线程调度，点击及识别仍由独立服务执行。</summary>
public partial class MainWindow : Window
{
    private readonly SessionConfigStore? _store;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly UpdatePreferencesStore _preferences;
    private readonly MouseController _mouse;
    private readonly OverlayWindow _overlay = new();
    private readonly CancellationTokenSource _updates = new();
    private GlobalHotkeyManager? _hotkeys;
    private List<ClickArea> _areas = [];
    private ClickArea? _active, _hover;
    private HelpWindow? _help;
    private CancellationTokenSource? _sessionCancellation;
    private Task? _session;
    private volatile bool _running, _closing, _selecting;
    private volatile int _modalDepth;
    private bool _conditionSession, _saveBlocked, _loaded;
    private Exception? _restoreError, _hotkeyError;

    public MainWindow() : this(new SessionConfigStore()) { }

    internal MainWindow(SessionConfigStore? store, HttpClient? http = null, UpdatePreferencesStore? preferences = null,
        bool enableHotkeys = true, IMouseInput? mouseInput = null)
    {
        _store = store;
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _ownsHttp = http == null;
        _preferences = preferences ?? new UpdatePreferencesStore();
        _mouse = new MouseController(mouseInput ?? new Win32MouseInput());
        InitializeComponent();
        try
        {
            if (enableHotkeys)
            {
                _hotkeys = new GlobalHotkeyManager();
                _hotkeys.KeyPressed += HandleHotkey;
                _hotkeys.ShouldSuppressEscape = () => _running;
            }
        }
        catch (Win32Exception ex) { _hotkeyError = ex; HotkeyMenu.IsEnabled = false; }
        foreach (var language in Localization.SupportedLanguages)
        {
            string code = language.Key;
            var item = new MenuItem { Header = language.Value, Tag = code };
            item.Click += (_, _) => { Localization.Language = code; Translate(); };
            LanguageMenu.Items.Add(item);
        }
        Translate();
        RestoreSession();
        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    internal event EventHandler? UpdateStateChanged;
    internal Version CurrentVersion { get; } = UpdateVersion.Normalize(typeof(MainWindow).Assembly.GetName().Version ?? new Version(1, 0, 0));
    internal string DisplayVersion => CurrentVersion.ToString(CurrentVersion.Revision > 0 ? 4 : 3);
    internal bool CheckingForUpdates { get; private set; } = true;
    internal bool UpdateCheckSucceeded { get; private set; }
    internal UpdateInfo? AvailableUpdate { get; private set; }
    internal Version? SkippedUpdateVersion { get; private set; }
    internal Task? UpdateCheckTask { get; private set; }
    internal Task? SessionTask => _session;
    internal bool IsRunning => _running;
    internal bool IsClosing => _closing;

    /// <summary>按键按下时及执行时均检查模态状态，防止队列中的启动操作穿过对话框。</summary>
    internal static bool CanStartFromHotkey(bool selecting, int modalDepth, bool blockedAtPress) =>
        !selecting && modalDepth == 0 && !blockedAtPress;

    private void Translate()
    {
        Title = Localization.Get("WindowTitle");
        Resources["LeftButtonDescription"] = Localization.Get("LeftMouseButton");
        Resources["RightButtonDescription"] = Localization.Get("RightMouseButton");
        Resources["ConditionDescription"] = Localization.Get("ConditionEdit");
        foreach (var pair in new (MenuItem Item, string Key)[] { (FileMenu,"File"),(SaveMenu,"SaveConfig"),(LoadMenu,"LoadConfig"),(SettingsMenu,"Settings"),(HotkeyMenu,"HotkeySettings"),(LanguageMenu,"Language"),(HelpMenu,"Help"),(GuideMenu,"ViewHelp"),(AboutMenu,"About"),(LeftMenu,"LeftMouseButton"),(RightMenu,"RightMouseButton"),(ConditionMenu,"ConditionEdit") }) pair.Item.Header = Localization.Get(pair.Key);
        foreach (MenuItem item in LanguageMenu.Items) item.IsChecked = Equals(item.Tag, Localization.Language);
        foreach (var pair in new (System.Windows.Controls.Button Item, string Key)[] { (AddButton,"Add"),(RemoveButton,"Remove"),(UpButton,"MoveUp"),(DownButton,"MoveDown"),(ConditionButton,"ConditionEdit"),(UpdateButton,"UpdateAvailable") }) pair.Item.Content = Localization.Get(pair.Key);
        foreach (var pair in new (TextBlock Item, string Key)[] { (AreasTitle,"MainAreasTitle"),(AreasHint,"MainAreasHint"),(TimingTitle,"MainTimingTitle"),(DisplayTitle,"MainDisplayTitle"),(IntervalLabel,"IntervalLabel"),(JitterLabel,"JitterLabel"),(SequenceLabel,"SequenceLabel") }) pair.Item.Text = Localization.Get(pair.Key);
        foreach (var pair in new (System.Windows.Controls.CheckBox Item, string Key)[] { (OverlayInput,"ShowOverlay"),(DetectionInput,"ShowDetectionOverlay"),(HighlightInput,"HighlightCurrentArea"),(HardwareInput,"HardwareMode") }) pair.Item.Content = Localization.Get(pair.Key);
        int index = Math.Max(0, SequenceInput.SelectedIndex);
        SequenceInput.ItemsSource = new[] { "SequenceLoop", "SequenceRandom", "SequenceOnce" }.Select(Localization.Get).ToArray();
        SequenceInput.SelectedIndex = index;
        RunHint.Text = string.Format(Localization.Get("MainRunHint"), _hotkeys?.Hotkey.ToString() ?? "F6");
        System.Windows.Automation.AutomationProperties.SetName(AreasList, Localization.Get("MainAreasTitle"));
        System.Windows.Automation.AutomationProperties.SetName(IntervalInput, Localization.Get("IntervalLabel"));
        System.Windows.Automation.AutomationProperties.SetName(JitterInput, Localization.Get("JitterLabel"));
        System.Windows.Automation.AutomationProperties.SetName(SequenceInput, Localization.Get("SequenceLabel"));
        _help?.ReloadCatalog(HelpContent.GetCatalog(Localization.Language));
        RefreshActions();
        RefreshTimingHint();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        if (_restoreError != null) ShowError(string.Format(Localization.Get("LoadConfigError"), _restoreError.Message));
        if (_hotkeyError != null) ShowError(string.Format(Localization.Get("HotkeyUnavailable"), _hotkeyError.Message));
        UpdateOverlay();
        UpdateCheckTask = CheckUpdatesAsync();
    }

    private async Task CheckUpdatesAsync()
    {
        try
        {
            var result = await new GitHubUpdateService(_http).CheckForUpdateAsync(CurrentVersion, _updates.Token);
            if (_closing) return;
            UpdateCheckSucceeded = result.Succeeded;
            if (result.Update is { } update)
            {
                if (_preferences.IsSkipped(update.Version)) SkippedUpdateVersion = update.Version;
                else AvailableUpdate = update;
            }
        }
        catch (OperationCanceledException) when (_updates.IsCancellationRequested) { return; }
        catch (Exception) { UpdateCheckSucceeded = false; }
        if (_closing) return;
        CheckingForUpdates = false;
        NotifyUpdate();
    }

    private void NotifyUpdate()
    {
        UpdateButton.Visibility = AvailableUpdate != null ? Visibility.Visible : Visibility.Collapsed;
        UpdateStateChanged?.Invoke(this, EventArgs.Empty);
    }

    internal bool SkipAvailableUpdate()
    {
        if (AvailableUpdate == null) return false;
        try { _preferences.Skip(AvailableUpdate.Version); }
        catch (Exception ex) { ShowError(string.Format(Localization.Get("UpdatePreferencesError"), ex.Message)); return false; }
        SkippedUpdateVersion = AvailableUpdate.Version;
        AvailableUpdate = null;
        NotifyUpdate();
        return true;
    }

    internal void OpenExternalLink(Uri uri)
    {
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose(); }
        catch (Exception ex) { ShowError(string.Format(Localization.Get("OpenLinkError"), ex.Message)); }
    }

    internal T Modal<T>(Func<T> action)
    {
        _modalDepth++;
        try { return action(); }
        finally { _modalDepth--; }
    }

    private void ShowError(string message) => Modal(() => WpfMessageBox.Show(this, message, Localization.Get("Error"), MessageBoxButton.OK, MessageBoxImage.Warning));

    private void HandleHotkey(System.Windows.Forms.Keys key)
    {
        bool blockedAtPress = _selecting || _modalDepth > 0;
        if (_closing || Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (_closing) return;
            if ((key == System.Windows.Forms.Keys.Escape && _running) || (key == _hotkeys?.Hotkey && _running)) StopSession();
            else if (key == _hotkeys?.Hotkey && CanStartFromHotkey(_selecting, _modalDepth, blockedAtPress)) StartSession();
        });
    }

    internal ConfigData CaptureConfig()
    {
        if (!decimal.TryParse(IntervalInput.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal interval) ||
            !decimal.TryParse(JitterInput.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal jitter))
            throw new InvalidDataException(Localization.Get("IntervalLabel") + " / " + Localization.Get("JitterLabel"));
        var config = new ConfigData { ClickAreas = _areas, Interval = interval, Jitter = jitter, SequenceIndex = SequenceInput.SelectedIndex,
            ShowOverlay = OverlayInput.IsChecked == true, ShowDetectionOverlay = DetectionInput.IsChecked == true,
            HighlightCurrentArea = HighlightInput.IsChecked == true, HardwareMode = HardwareInput.IsChecked == true };
        config.Validate(10, 600000, 0, 10000, 3);
        return config;
    }

    internal void ApplyConfig(ConfigData config)
    {
        config.Validate(10, 600000, 0, 10000, 3);
        _areas = config.ClickAreas;
        IntervalInput.Text = config.Interval.ToString(CultureInfo.CurrentCulture);
        JitterInput.Text = config.Jitter.ToString(CultureInfo.CurrentCulture);
        SequenceInput.SelectedIndex = config.SequenceIndex;
        OverlayInput.IsChecked = config.ShowOverlay;
        DetectionInput.IsChecked = config.ShowDetectionOverlay;
        HighlightInput.IsChecked = config.HighlightCurrentArea;
        HardwareInput.IsChecked = config.HardwareMode;
        RefreshAreas();
    }

    private void RestoreSession()
    {
        try { if (_store?.Load() is { } config) ApplyConfig(config); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {
            _restoreError = ex;
            try { _store?.BackupUnreadableConfig(); }
            catch (Exception backup) when (backup is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            { _saveBlocked = true; _restoreError = new IOException(ex.Message + Environment.NewLine + backup.Message, ex); }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = Localization.Get("JsonFilter"), Title = Localization.Get("SaveConfigTitle") };
        if (Modal(() => dialog.ShowDialog(this)) != true) return;
        try { File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(CaptureConfig(), new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { ShowError(string.Format(Localization.Get("SaveConfigError"), ex.Message)); }
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = Localization.Get("JsonFilter"), Title = Localization.Get("LoadConfigTitle") };
        if (Modal(() => dialog.ShowDialog(this)) != true) return;
        try { ApplyConfig(JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(dialog.FileName)) ?? throw new InvalidDataException("配置内容为空。")); }
        catch (Exception ex) { ShowError(string.Format(Localization.Get("LoadConfigError"), ex.Message)); }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (_session != null || _selecting) return;
        _selecting = true;
        bool restoreHelp = _help?.IsVisible == true;
        Hide(); _overlay.Hide(); _help?.Hide();
        try
        {
            await Task.Delay(200);
            if (_closing) return;
            var selection = new SelectionWindow();
            bool selected = Modal(() => selection.ShowDialog()) == true;
            Show();
            if (selected)
            {
                var prompt = new LabelPromptWindow(Localization.Get("AreaLabelDefault") + (_areas.Count + 1)) { Owner = this };
                if (Modal(() => prompt.ShowDialog()) == true && !string.IsNullOrWhiteSpace(prompt.Label))
                { _areas.Add(new ClickArea { Area = selection.SelectedArea, Label = prompt.Label }); RefreshAreas(); }
            }
        }
        finally { _selecting = false; if (!_closing) { Show(); if (restoreHelp) _help?.Show(); UpdateOverlay(); } }
    }

    private void Remove_Click(object sender, RoutedEventArgs e) { if (_session == null && AreasList.SelectedItem is ClickArea area) { _areas.Remove(area); RefreshAreas(); } }
    private void Up_Click(object sender, RoutedEventArgs e) => MoveArea(-1);
    private void Down_Click(object sender, RoutedEventArgs e) => MoveArea(1);
    internal void MoveArea(int delta)
    {
        int index = AreasList.SelectedIndex, target = index + delta;
        if (_session != null || index < 0 || target < 0 || target >= _areas.Count) return;
        var area = _areas[index]; _areas.RemoveAt(index); _areas.Insert(target, area); RefreshAreas(area);
    }
    private void Left_Click(object sender, RoutedEventArgs e) => SetButton(false);
    private void Right_Click(object sender, RoutedEventArgs e) => SetButton(true);
    internal void SetButton(bool right) { if (_session == null && AreasList.SelectedItem is ClickArea area) { area.RightButton = right; RefreshAreas(area); } }
    private void Condition_Click(object sender, RoutedEventArgs e)
    {
        if (_session != null || AreasList.SelectedItem is not ClickArea area) return;
        _selecting = true; UpdateOverlay();
        try
        {
            var dialog = new ConditionSettingsWindow(area.Condition, ShowHelp) { Owner = this };
            if (Modal(() => dialog.ShowDialog()) == true) { area.Condition = dialog.ResultCondition; RefreshAreas(area); }
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { _selecting = false; UpdateOverlay(); }
    }
    private void Hotkey_Click(object sender, RoutedEventArgs e)
    {
        if (_hotkeys == null) return;
        Modal(() => new HotkeySettingsWindow(_hotkeys) { Owner = this }.ShowDialog());
        Translate();
    }
    private void Help_Click(object sender, RoutedEventArgs e) => ShowHelp(HelpTopic.QuickStart);
    private void ShowHelp(HelpTopic topic)
    {
        if (_help == null) { _help = new HelpWindow(topic, () => _running); _help.Closed += (_, _) => _help = null; }
        _help.ShowTopic(topic); _help.Show(); _help.Activate();
    }
    private void About_Click(object sender, RoutedEventArgs e) => Modal(() => new AboutWindow(this) { Owner = this }.ShowDialog());
    private void Start_Click(object sender, RoutedEventArgs e) { if (_running) StopSession(); else StartSession(); }

    internal void StartSession()
    {
        if (_session != null || _closing || _selecting || _modalDepth > 0) return;
        if (_areas.Count == 0) { ShowError(Localization.Get("AddAreaFirst")); return; }
        ConfigData config;
        ClickArea[] snapshot;
        try { config = CaptureConfig(); snapshot = JsonSerializer.Deserialize<ClickArea[]>(JsonSerializer.Serialize(config.ClickAreas))!; }
        catch (Exception ex) { ShowError(ex.Message); return; }
        var cancellation = new CancellationTokenSource();
        _sessionCancellation = cancellation;
        _running = true; _hover = null; _conditionSession = snapshot.Any(area => area.Condition != null);
        _session = Task.CompletedTask;
        RefreshActions(); UpdateOverlay();
        _session = RunSessionAsync(snapshot, config, cancellation);
    }

    private async Task RunSessionAsync(ClickArea[] snapshot, ConfigData config, CancellationTokenSource cancellation)
    {
        await Task.Yield();
        Exception? failure = null;
        try
        {
            await Task.Run(async () =>
            {
                using var detector = new OpenCvConditionDetector();
                var runner = new ClickSessionRunner(detector);
                await runner.RunAsync(snapshot, new ClickSessionOptions { IntervalMs = (int)config.Interval, JitterMs = (int)config.Jitter, SequenceIndex = config.SequenceIndex },
                    async (area, token) => await Dispatcher.InvokeAsync(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        if (_closing || !_running) throw new OperationCanceledException(token);
                        _mouse.PerformRandomClickInArea(area.Area, config.HardwareMode, area.RightButton);
                        _active = _areas[Array.IndexOf(snapshot, area)]; UpdateOverlay();
                    }, DispatcherPriority.Normal, token),
                    (area, detected) =>
                    {
                        if (_closing || cancellation.IsCancellationRequested) return;
                        Dispatcher.BeginInvoke(() =>
                        {
                            if (_closing || !_running) return;
                            Title = Localization.Get("WindowTitle") + " — " + area.Label + " — " + Localization.Get(detected.HasValue && detected.Value.Score >= (area.Condition?.Threshold ?? 1) ? "ConditionMatched" : "ConditionWaiting") + (detected.HasValue ? " " + detected.Value.Score.ToString("F3") : "");
                        });
                    }, cancellation.Token);
            }, cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested || _closing) { }
        catch (Exception ex) { failure = ex; }
        finally
        {
            cancellation.Dispose(); _sessionCancellation = null; _session = null; _running = false; _conditionSession = false; _active = null;
            if (!_closing) { Title = Localization.Get("WindowTitle"); RefreshActions(); UpdateOverlay(); }
        }
        if (failure != null && !_closing) ShowError(failure.Message);
    }

    internal void StopSession()
    {
        _running = false; _active = null; _sessionCancellation?.Cancel(); RefreshActions(); UpdateOverlay();
    }

    private void RefreshAreas(ClickArea? selected = null)
    {
        selected ??= AreasList.SelectedItem as ClickArea;
        _hover = null; AreasList.ItemsSource = null; AreasList.ItemsSource = _areas; AreasList.SelectedItem = selected;
        RefreshActions(); UpdateOverlay();
    }
    private void RefreshActions()
    {
        AreaCount.Text = _areas.Count.ToString(CultureInfo.CurrentCulture);
        bool idle = _session == null;
        MainMenu.IsEnabled = TimingPanel.IsEnabled = AreasList.IsEnabled = AddButton.IsEnabled = UpdateButton.IsEnabled = idle;
        bool selected = AreasList.SelectedItem != null;
        RemoveButton.IsEnabled = ConditionButton.IsEnabled = LeftMenu.IsEnabled = RightMenu.IsEnabled = ConditionMenu.IsEnabled = idle && selected;
        UpButton.IsEnabled = idle && AreasList.SelectedIndex > 0;
        DownButton.IsEnabled = idle && selected && AreasList.SelectedIndex < _areas.Count - 1;
        StartButton.IsEnabled = idle || _running;
        StartButton.Content = Localization.Get(_running ? "StopClicking" : idle ? "StartClicking" : "ConditionStopped");
        StartButton.Background = new System.Windows.Media.SolidColorBrush(_running
            ? System.Windows.Media.Color.FromRgb(180, 40, 40)
            : System.Windows.Media.Color.FromRgb(37, 99, 185));
    }

    private void Selection_Changed(object sender, SelectionChangedEventArgs e) { if (!_loaded) return; RefreshActions(); UpdateOverlay(); }
    private void Areas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _hover = _running ? null : (ItemsControl.ContainerFromElement(AreasList, e.OriginalSource as DependencyObject) as ListBoxItem)?.Content as ClickArea;
        UpdateOverlay();
    }
    private void Areas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) { _hover = null; UpdateOverlay(); }
    private void Areas_LeftClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1 && ItemsControl.ContainerFromElement(AreasList, e.OriginalSource as DependencyObject) is ListBoxItem { IsSelected: true })
        {
            AreasList.SelectedIndex = -1;
            _hover = null;
            e.Handled = true;
            UpdateOverlay();
        }
    }
    private void Areas_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!_loaded || e.VerticalChange == 0 && e.HorizontalChange == 0) return;
        _hover = null;
        // 等待滚动布局完成后重新命中测试，避免高亮滚动前位于指针下方的项目。
        Dispatcher.BeginInvoke(() =>
        {
            if (_closing || _running || !AreasList.IsMouseOver) return;
            var hit = AreasList.InputHitTest(System.Windows.Input.Mouse.GetPosition(AreasList)) as DependencyObject;
            _hover = (ItemsControl.ContainerFromElement(AreasList, hit) as ListBoxItem)?.Content as ClickArea;
            UpdateOverlay();
        }, DispatcherPriority.Background);
        UpdateOverlay();
    }
    private void Areas_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(AreasList, e.OriginalSource as DependencyObject) is ListBoxItem item) item.IsSelected = true;
        LeftMenu.IsChecked = AreasList.SelectedItem is ClickArea { RightButton: false };
        RightMenu.IsChecked = AreasList.SelectedItem is ClickArea { RightButton: true };
    }
    private void Overlay_Click(object sender, RoutedEventArgs e) => UpdateOverlay();
    private void UpdateOverlay()
    {
        if (!_loaded || _closing) return;
        HighlightInput.IsEnabled = OverlayInput.IsChecked == true;
        _overlay.AreasToDraw = new List<ClickArea>(_areas);
        _overlay.RequireCaptureExclusion = _conditionSession;
        _overlay.ShowClickAreas = OverlayInput.IsChecked == true;
        _overlay.ShowDetectionAreas = DetectionInput.IsChecked == true;
        _overlay.PreviewArea = !_running ? _hover ?? AreasList.SelectedItem as ClickArea : null;
        _overlay.ActiveArea = _running && HighlightInput.IsChecked == true ? _active : null;
        if ((_overlay.ShowClickAreas || _overlay.ShowDetectionAreas) && !_selecting && (!_conditionSession || _overlay.CanShowDuringCondition())) _overlay.Show();
        else _overlay.Hide();
        _overlay.InvalidateVisual();
    }
    private void Timing_Changed(object sender, TextChangedEventArgs e) { if (TimingHint != null) RefreshTimingHint(); }
    private void RefreshTimingHint()
    {
        bool valid = decimal.TryParse(IntervalInput.Text, out decimal interval) && interval is >= 10 and <= 600000;
        valid &= decimal.TryParse(JitterInput.Text, out decimal jitter) && jitter is >= 0 and <= 10000;
        TimingHint.Text = !valid ? Localization.Get("IntervalLabel") + " 10–600000; " + Localization.Get("JitterLabel") + " 0–10000" : jitter > interval ? Localization.Get("JitterExceedsIntervalHint") : "";
        TimingHint.Visibility = string.IsNullOrEmpty(TimingHint.Text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (e.Cancel) return;
        if (!TrySaveBeforeClose(error => Modal(() => WpfMessageBox.Show(this,
            string.Format(Localization.Get("SaveConfigError"), error.Message) + Environment.NewLine + Localization.Get("CloseWithoutSaving"),
            Localization.Get("Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)) == MessageBoxResult.Yes))
        { e.Cancel = true; return; }
        _closing = true; _updates.Cancel(); StopSession();
    }

    // 保存失败时由调用方明确决定是否放弃，既保护已有文件，也允许在磁盘不可写时退出。
    internal bool TrySaveBeforeClose(Func<Exception, bool> discardChanges)
    {
        if (_saveBlocked || _store == null) return true;
        try { _store.Save(CaptureConfig()); return true; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        { return discardChanges(ex); }
    }
    private void OnClosed(object? sender, EventArgs e)
    {
        _hotkeys?.Dispose(); _overlay.Close(); _help?.Close();
        if (_ownsHttp) _http.Dispose();
        // 更新任务观察到取消后再释放令牌，避免关闭与网络回调竞争。
        if (UpdateCheckTask is { } task) _ = task.ContinueWith(_ => _updates.Dispose(), TaskScheduler.Default);
        else _updates.Dispose();
    }
}
