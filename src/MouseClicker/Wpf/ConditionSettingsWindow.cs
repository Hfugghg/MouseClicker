using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using Image = System.Windows.Controls.Image;

namespace MouseClicker.Wpf;

internal sealed class ConditionSettingsWindow : Window
{
    internal const long MaximumImportBytes = 32L * 1024 * 1024;
    internal const long MaximumImportPixels = 32L * 1024 * 1024;

    internal static void ValidateImportSource(long fileBytes, Size? dimensions = null)
    {
        if (fileBytes < 0 || fileBytes > MaximumImportBytes ||
            dimensions is Size size && (size.Width < 2 || size.Height < 2 || (long)size.Width * size.Height > MaximumImportPixels))
            throw new InvalidDataException(Localization.Get("ConditionImportTooLarge"));
    }
    private readonly CheckBox enabled = new() { Content = new TextBlock { Text = Localization.Get("ConditionEnabled"), TextWrapping = TextWrapping.Wrap } };
    private readonly CheckBox multiScale = new() { Content = new TextBlock { Text = Localization.Get("ConditionUseMultiScale"), TextWrapping = TextWrapping.Wrap } };
    private readonly TextBlock region = DialogLayout.Text(string.Empty);
    private readonly TextBlock dimensions = DialogLayout.Text(string.Empty);
    private readonly TextBlock hint = DialogLayout.Text(string.Empty);
    private readonly TextBlock result = DialogLayout.Text(string.Empty);
    private readonly Image preview = new() { Height = 150, Stretch = System.Windows.Media.Stretch.Uniform };
    private readonly TextBox threshold = new() { Text = .9.ToString(CultureInfo.CurrentCulture) };
    private readonly TextBox poll = new() { Text = "200" };
    private readonly TextBox timeout = new() { Text = "60" };
    private readonly ComboBox mode = new();
    private readonly StackPanel operations = new();
    private readonly Button capture;
    private readonly Button import;
    private readonly Button crop;
    private readonly Button test;
    private readonly Button save;
    private readonly Func<Rectangle, Bitmap> captureScreen;
    private Rectangle detectionArea;
    private string template = string.Empty;
    private Bitmap? templateBitmap;
    private bool busy;
    private bool closed;
    internal ClickCondition? ResultCondition { get; private set; }

    internal ConditionSettingsWindow(ClickCondition? original, Action<HelpTopic>? onHelpRequested = null, Func<Rectangle, Bitmap>? captureScreen = null)
    {
        this.captureScreen = captureScreen ?? ScreenCapture.Capture;
        DialogTheme.Apply(this);
        Title = Localization.Get("ConditionEdit"); Width = 630; Height = 750; MinWidth = 440; MinHeight = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var layout = new DockPanel { Margin = new Thickness(24) };
        var footer = new WrapPanel { Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        test = DialogLayout.Button(Localization.Get("ConditionTest"), async () => await TestAsync());
        save = DialogLayout.Button(Localization.Get("ConditionSave"), Save);
        save.IsDefault = true;
        footer.Children.Add(test); footer.Children.Add(save);
        footer.Children.Add(DialogLayout.Button(Localization.Get("ConditionCancel"), Close, true));
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        DockPanel.SetDock(result, Dock.Bottom); layout.Children.Add(result);
        var heading = new TextBlock { Text = Title, FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 18) };
        DockPanel.SetDock(heading, Dock.Top); layout.Children.Add(heading);
        var card = new Border { Background = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(222, 230, 240)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(18), Child = operations };
        layout.Children.Add(new ScrollViewer { Content = card, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Content = layout;
        operations.Children.Add(enabled); operations.Children.Add(multiScale);
        AutomationProperties.SetName(enabled, Localization.Get("ConditionEnabled"));
        AutomationProperties.SetName(multiScale, Localization.Get("ConditionUseMultiScale"));
        AutomationProperties.SetName(preview, Localization.Get("ConditionTemplateDimensions"));
        operations.Children.Add(DialogLayout.Button(HelpContent.GetCatalog(Localization.Language).ConditionGuideText, () => onHelpRequested?.Invoke(HelpTopic.ImageConditionSetup)));
        operations.Children.Add(DialogLayout.Button(Localization.Get("ConditionRegion"), async () => await SelectAsync(false)));
        operations.Children.Add(region); operations.Children.Add(dimensions); operations.Children.Add(hint);
        capture = DialogLayout.Button(Localization.Get("ConditionCapture"), async () => await SelectAsync(true));
        import = DialogLayout.Button(Localization.Get("ConditionImport"), ImportTemplate);
        crop = DialogLayout.Button(Localization.Get("ConditionCropTemplate"), CropTemplate);
        var tools = new WrapPanel(); tools.Children.Add(capture); tools.Children.Add(import); tools.Children.Add(crop); operations.Children.Add(tools);
        operations.Children.Add(preview);
        AddField("ConditionThreshold", threshold); AddField("ConditionPoll", poll);
        mode.Items.Add(Localization.Get("ConditionOnce")); mode.Items.Add(Localization.Get("ConditionWhile")); mode.SelectedIndex = 0;
        AddField("ConditionMode", mode); AddField("ConditionTimeout", timeout);
        operations.Children.Add(DialogLayout.Text(Localization.Get("ConditionHint")));
        enabled.IsChecked = original != null; multiScale.IsChecked = original?.UseMultiScale ?? true;
        if (original != null)
        {
            detectionArea = original.DetectionArea;
            threshold.Text = original.Threshold.ToString(CultureInfo.CurrentCulture);
            poll.Text = original.PollIntervalMs.ToString(CultureInfo.CurrentCulture);
            timeout.Text = (original.TimeoutMs / 1000m).ToString(CultureInfo.CurrentCulture);
            mode.SelectedIndex = (int)original.TriggerMode;
            using var stream = new MemoryStream(original.GetTemplateBytes());
            using var bitmap = new Bitmap(stream);
            SetTemplate(bitmap, false);
        }
        enabled.Checked += (_, _) => UpdateSetupState(); enabled.Unchecked += (_, _) => UpdateSetupState();
        multiScale.Checked += (_, _) => UpdateSetupState(); multiScale.Unchecked += (_, _) => UpdateSetupState();
        Closed += (_, _) => { closed = true; templateBitmap?.Dispose(); templateBitmap = null; };
        UpdateSetupState();
    }

    private void AddField(string key, UIElement field)
    {
        var label = DialogLayout.Text(Localization.Get(key));
        var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        label.VerticalAlignment = VerticalAlignment.Center; label.Margin = new Thickness(0, 0, 12, 0);
        Grid.SetColumn(field, 1); row.Children.Add(label); row.Children.Add(field); operations.Children.Add(row);
        AutomationProperties.SetName(field, label.Text);
        AutomationProperties.SetLabeledBy(field, label);
    }

    private void UpdateSetupState()
    {
        var size = templateBitmap?.Size ?? Size.Empty;
        bool validRegion = detectionArea.Width >= 2 && detectionArea.Height >= 2;
        bool fits = validRegion && !size.IsEmpty && (multiScale.IsChecked == true || size.Width <= detectionArea.Width && size.Height <= detectionArea.Height);
        region.Text = string.Format(Localization.Get("ConditionDetectionDimensions"), detectionArea.Width, detectionArea.Height, detectionArea.X, detectionArea.Y);
        dimensions.Text = size.IsEmpty ? Localization.Get("ConditionSetupTemplate") : string.Format(Localization.Get("ConditionTemplateDimensions"), size.Width, size.Height);
        hint.Text = !validRegion ? Localization.Get("ConditionSetupRegionFirst") : size.IsEmpty ? Localization.Get("ConditionSetupTemplate")
            : !fits ? string.Format(Localization.Get("ConditionTemplateFit"), size.Width, size.Height, detectionArea.Width, detectionArea.Height)
            : multiScale.IsChecked == true ? Localization.Get("ConditionScaleHint") : string.Empty;
        operations.IsEnabled = !busy;
        capture.IsEnabled = import.IsEnabled = validRegion;
        crop.IsEnabled = templateBitmap != null;
        test.IsEnabled = !busy && fits;
        save.IsEnabled = !busy && (enabled.IsChecked != true || fits);
    }

    // 保持模态窗口可见，仅临时透明，避免中断 ShowDialog 的消息循环。
    internal async Task WithTransparentWindowsAsync(Func<Task> operation)
    {
        var windows = System.Windows.Application.Current?.Windows.Cast<Window>().Where(w => w.IsVisible).ToDictionary(w => w, w => w.Opacity)
            ?? new Dictionary<Window, double> { [this] = Opacity };
        try
        {
            foreach (var window in windows.Keys) window.Opacity = 0;
            await Task.Delay(150);
            if (!closed) await operation();
        }
        finally
        {
            // 隐藏窗口也需要恢复透明度，否则随后重新显示时会变成不可见窗口。
            foreach (var pair in windows) if (!ReferenceEquals(pair.Key, this) || !closed) pair.Key.Opacity = pair.Value;
            if (!closed) Activate();
        }
    }

    private async Task SelectAsync(bool captureTemplate)
    {
        if (busy) return;
        busy = true; UpdateSetupState();
        try
        {
            await WithTransparentWindowsAsync(async () =>
            {
                Rectangle selected;
                if (captureTemplate)
                {
                    var selector = new TemplateCaptureWindow(detectionArea.Size);
                    if (selector.ShowDialog() != true) return;
                    selected = selector.SelectedArea;
                }
                else
                {
                    var selector = new SelectionWindow();
                    if (selector.ShowDialog() != true) return;
                    selected = selector.SelectedArea;
                }
                await Task.Delay(150);
                if (closed) return;
                if (captureTemplate) { using var bitmap = captureScreen(selected); SetTemplate(bitmap); }
                else detectionArea = selected;
            });
        }
        catch (Exception ex) { if (!closed) DisplayTestError(ex); }
        finally { busy = false; if (!closed) UpdateSetupState(); }
    }

    private void ImportTemplate()
    {
        if (busy) return;
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp", CheckFileExists = true };
            if (dialog.ShowDialog(this) != true) return;
            ValidateImportSource(new FileInfo(dialog.FileName).Length);
            using var image = System.Drawing.Image.FromFile(dialog.FileName);
            ValidateImportSource(0, image.Size);
            using var bitmap = new Bitmap(image); SetTemplate(bitmap);
        }
        catch (Exception ex) { DisplayTestError(ex); }
    }

    private void CropTemplate()
    {
        if (busy || templateBitmap == null) return;
        try
        {
            var dialog = new TemplateCropWindow(templateBitmap, templateBitmap.Size) { Owner = this };
            if (dialog.ShowDialog() == true) SetCroppedTemplate(templateBitmap, dialog.CropArea);
        }
        catch (Exception ex) { DisplayTestError(ex); }
    }

    internal void SetCroppedTemplate(Bitmap source, Rectangle cropArea)
    {
        ValidateImportSource(0, source.Size);
        if (cropArea.Width < 2 || cropArea.Height < 2 || !new Rectangle(System.Drawing.Point.Empty, source.Size).Contains(cropArea))
            throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
        using var bitmap = source.Clone(cropArea, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        SetTemplate(bitmap);
    }

    private void SetTemplate(Bitmap bitmap, bool activate = true)
    {
        if (bitmap.Width < 2 || bitmap.Height < 2 || (long)bitmap.Width * bitmap.Height > ClickCondition.MaximumTemplatePixels)
            throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
        using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        if (stream.Length > ClickCondition.MaximumTemplateBytes) throw new InvalidDataException(Localization.Get("ConditionErrorTemplateSize"));
        var replacement = new Bitmap(bitmap);
        template = Convert.ToBase64String(stream.ToArray());
        templateBitmap?.Dispose(); templateBitmap = replacement;
        SetPreview(stream.ToArray());
        if (activate) enabled.IsChecked = true;
        result.Text = string.Empty; UpdateSetupState();
    }

    private void SetPreview(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); preview.Source = image;
    }

    internal ClickCondition BuildCondition()
    {
        var condition = new ClickCondition { DetectionArea = detectionArea, TemplateImageBase64 = template, UseMultiScale = multiScale.IsChecked == true,
            Threshold = double.Parse(threshold.Text, CultureInfo.CurrentCulture), PollIntervalMs = int.Parse(poll.Text, CultureInfo.CurrentCulture),
            TimeoutMs = checked((int)(decimal.Parse(timeout.Text, CultureInfo.CurrentCulture) * 1000)), TriggerMode = (ConditionTriggerMode)mode.SelectedIndex };
        condition.Validate(); return condition;
    }

    private void Save()
    {
        if (busy) return;
        try { ResultCondition = enabled.IsChecked == true ? BuildCondition() : null; DialogResult = true; }
        catch (Exception ex) { DisplayTestError(ex); }
    }

    private async Task TestAsync()
    {
        if (busy) return;
        try
        {
            var condition = BuildCondition(); busy = true; UpdateSetupState(); result.Text = Localization.Get("ConditionTesting");
            await WithTransparentWindowsAsync(async () =>
            {
                var sample = await Task.Run(() =>
                {
                    using var bitmap = captureScreen(condition.DetectionArea);
                    using var stream = new MemoryStream(condition.GetTemplateBytes()); using var templateImage = new Bitmap(stream);
                    var detected = OpenCvConditionDetector.Match(bitmap, templateImage, condition.UseMultiScale);
                    if (detected.Score >= condition.Threshold)
                    { using var graphics = System.Drawing.Graphics.FromImage(bitmap); using var pen = new System.Drawing.Pen(System.Drawing.Color.Lime, 2); graphics.DrawRectangle(pen, detected.MatchBounds); }
                    using var output = new MemoryStream(); bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Png);
                    var bounds = detected.MatchBounds; bounds.Offset(condition.DetectionArea.Location);
                    return (Bytes: output.ToArray(), Detected: detected with { MatchBounds = bounds });
                });
                if (closed) return;
                SetPreview(sample.Bytes); DisplayTestResult(sample.Detected, condition.Threshold);
            });
        }
        catch (Exception ex) { if (!closed) DisplayTestError(ex); }
        finally { busy = false; if (!closed) UpdateSetupState(); }
    }

    internal void DisplayTestResult(DetectionResult detected, double requiredScore)
    {
        result.Foreground = detected.Score >= requiredScore ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Firebrick;
        result.Text = string.Format(Localization.Get(detected.Score >= requiredScore ? "ConditionTestMatched" : "ConditionTestNotMatched"), detected.Score, requiredScore)
            + Environment.NewLine + detected.MatchBounds + "  " + string.Format(Localization.Get("ConditionMatchScale"), detected.Scale, detected.MatchBounds.Width, detected.MatchBounds.Height);
    }
    internal void DisplayTestError(Exception error)
    {
        result.Foreground = System.Windows.Media.Brushes.Firebrick;
        result.Text = string.Format(Localization.Get("ConditionTestError"), error.Message);
    }
}



