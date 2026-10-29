using System.Drawing.Imaging;

namespace MouseClicker;

internal sealed class ConditionSettingsForm : Form
{
    internal const long MaximumImportBytes = 32L * 1024 * 1024;
    internal const long MaximumImportPixels = 32L * 1024 * 1024;

    internal static void ValidateImportSource(long fileBytes, Size? dimensions = null)
    {
        if (fileBytes < 0 || fileBytes > MaximumImportBytes ||
            dimensions is Size size && (size.Width < 2 || size.Height < 2 || (long)size.Width * size.Height > MaximumImportPixels))
            throw new InvalidDataException(Localization.Get("ConditionImportTooLarge"));
    }
    private readonly CheckBox enabled = new() { AutoSize = true };
    private readonly CheckBox useMultiScale = new() { AutoSize = true, Name = "useMultiScaleCheckBox", MaximumSize = new Size(540, 0) };
    private readonly Label region = new() { AutoSize = true, MaximumSize = new Size(540, 0) };
    private readonly Label dimensions = new() { AutoSize = true, MaximumSize = new Size(540, 0) };
    private readonly Label setupHint = new() { AutoSize = true, MaximumSize = new Size(540, 0) };
    private readonly Button resizeDetection = new() { AutoSize = true, Name = "resizeDetectionButton" };
    private readonly Button save = new() { AutoSize = true, Name = "saveConditionButton" };
    private readonly Button capture = new() { AutoSize = true, Name = "captureTemplateButton" };
    private readonly Button import = new() { AutoSize = true, Name = "importTemplateButton" };
    private readonly Button cropTemplate = new() { AutoSize = true, Name = "cropTemplateButton" };
    private readonly PictureBox preview = new() { SizeMode = PictureBoxSizeMode.Zoom, Height = 150, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly NumericUpDown threshold = new() { Minimum = .1m, Maximum = 1, DecimalPlaces = 2, Increment = .01m, Value = .9m };
    private readonly NumericUpDown poll = new() { Minimum = 50, Maximum = 60000, Value = 200 };
    private readonly NumericUpDown timeout = new() { Minimum = 0, Maximum = 86400, Value = 60 };
    private readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
    private readonly Label result = new() { AutoSize = true, MaximumSize = new Size(540, 0) };
    private readonly Button test = new();
    private Rectangle detectionArea;
    private string template = string.Empty;
    private Size templateSize;
    private Bitmap? templateBitmap;
    private readonly List<Control> operationControls = [];
    private bool busy;
    private bool closed;
    internal ClickCondition? ResultCondition { get; private set; }

    internal ConditionSettingsForm(ClickCondition? original) : this(original, null) { }

    internal ConditionSettingsForm(ClickCondition? original, Action<HelpTopic>? onHelpRequested)
    {
        Text = Localization.Get("ConditionEdit");
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(590, 680);
        MinimumSize = new Size(610, 720);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoScroll = true, Padding = new Padding(16) };
        layout.Controls.Add(table, 0, 0);
        enabled.Text = Localization.Get("ConditionEnabled");
        table.Controls.Add(enabled);
        useMultiScale.Text = Localization.Get("ConditionUseMultiScale");
        useMultiScale.Checked = original?.UseMultiScale ?? true;
        table.Controls.Add(useMultiScale);
        var guide = new Button { Text = HelpContent.GetCatalog(Localization.Language).ConditionGuideText, AutoSize = true, Name = "conditionGuideButton" };
        guide.Click += (_, _) => { if (!busy) onHelpRequested?.Invoke(HelpTopic.ImageConditionSetup); };
        table.Controls.Add(guide);
        var select = new Button { Text = Localization.Get("ConditionRegion"), AutoSize = true };
        select.Click += async (_, _) => await SelectAsync(false);
        table.Controls.Add(select);
        operationControls.AddRange([enabled, useMultiScale, guide, select, resizeDetection, threshold, poll, timeout, mode]);
        table.Controls.Add(region);
        table.Controls.Add(dimensions);
        table.Controls.Add(setupHint);
        resizeDetection.Text = Localization.Get("ConditionResizeDetection");
        resizeDetection.Click += async (_, _) => await SelectAsync(false);
        table.Controls.Add(resizeDetection);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        capture.Text = Localization.Get("ConditionCapture");
        import.Text = Localization.Get("ConditionImport");
        cropTemplate.Text = Localization.Get("ConditionCropTemplate");
        capture.Click += async (_, _) => await SelectAsync(true);
        import.Click += (_, _) => ImportTemplate();
        cropTemplate.Click += (_, _) => CropTemplate();
        buttons.Controls.AddRange([capture, import, cropTemplate]);
        table.Controls.Add(buttons);
        table.Controls.Add(preview);
        AddField(table, "ConditionThreshold", threshold);
        AddField(table, "ConditionPoll", poll);
        mode.Items.AddRange([Localization.Get("ConditionOnce"), Localization.Get("ConditionWhile")]);
        mode.SelectedIndex = 0;
        AddField(table, "ConditionMode", mode);
        AddField(table, "ConditionTimeout", timeout);
        table.Controls.Add(new Label { Text = Localization.Get("ConditionHint"), AutoSize = true, MaximumSize = new Size(540, 0) });
        test.Text = Localization.Get("ConditionTest");
        test.AutoSize = true;
        test.Click += async (_, _) => await TestAsync();
        table.Controls.Add(test);
        table.Controls.Add(result);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 12), WrapContents = false };
        save.Text = Localization.Get("ConditionSave");
        var cancel = new Button { Text = Localization.Get("ConditionCancel"), DialogResult = DialogResult.Cancel, AutoSize = true };
        save.Click += (_, _) => Save();
        actions.Controls.AddRange([save, cancel]);
        layout.Controls.Add(actions, 0, 1);
        CancelButton = cancel;
        enabled.Checked = original != null;
        if (original != null)
        {
            detectionArea = original.DetectionArea;
            template = original.TemplateImageBase64;
            threshold.Value = Math.Clamp((decimal)original.Threshold, threshold.Minimum, threshold.Maximum);
            poll.Value = Math.Clamp(original.PollIntervalMs, (int)poll.Minimum, (int)poll.Maximum);
            timeout.Value = Math.Clamp(original.TimeoutMs / 1000m, timeout.Minimum, timeout.Maximum);
            mode.SelectedIndex = (int)original.TriggerMode;
            using var stream = new MemoryStream(Convert.FromBase64String(template));
            using var image = Image.FromStream(stream);
            templateSize = image.Size;
            templateBitmap = new Bitmap(image);
            ReplacePreview(new Bitmap(image));
        }
        UpdateRegion();
        enabled.CheckedChanged += (_, _) => UpdateSetupState();
        useMultiScale.CheckedChanged += (_, _) => { result.Text = string.Empty; UpdateSetupState(); };
    }

    private static void AddField(TableLayoutPanel table, string key, Control control)
    {
        var row = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        row.Controls.Add(new Label { Text = Localization.Get(key), AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
        row.Controls.Add(control);
        table.Controls.Add(row);
    }

    private void UpdateRegion()
    {
        result.Text = string.Empty;
        region.Text = string.Format(Localization.Get("ConditionDetectionDimensions"), detectionArea.Width, detectionArea.Height, detectionArea.X, detectionArea.Y);
        UpdateSetupState();
    }

    private void UpdateSetupState()
    {
        dimensions.Text = templateSize.IsEmpty ? Localization.Get("ConditionSetupTemplate") :
            string.Format(Localization.Get("ConditionTemplateDimensions"), templateSize.Width, templateSize.Height);
        bool regionValid = detectionArea.Width >= 2 && detectionArea.Height >= 2;
        bool fits = !templateSize.IsEmpty && regionValid && (useMultiScale.Checked || templateSize.Width <= detectionArea.Width && templateSize.Height <= detectionArea.Height);
        setupHint.Text = !regionValid ? Localization.Get("ConditionSetupRegionFirst") :
            templateSize.IsEmpty ? Localization.Get("ConditionSetupTemplate") : !fits ?
            string.Format(Localization.Get("ConditionTemplateFit"), templateSize.Width, templateSize.Height, detectionArea.Width, detectionArea.Height) :
            useMultiScale.Checked ? Localization.Get("ConditionScaleHint") : string.Empty;
        setupHint.ForeColor = !fits && !templateSize.IsEmpty ? Color.Firebrick : SystemColors.ControlText;
        resizeDetection.Visible = !templateSize.IsEmpty && !fits;
        capture.Enabled = regionValid && !busy;
        import.Enabled = regionValid && !busy;
        cropTemplate.Enabled = templateBitmap != null && !busy;
        foreach (var control in operationControls) control.Enabled = !busy;
        test.Enabled = fits && !busy;
        save.Enabled = (!enabled.Checked || fits) && !busy;
    }
    private void ReplacePreview(Bitmap image) { var old = preview.Image; preview.Image = image; old?.Dispose(); }

    // A modal form must stay Visible: Hide() terminates the ShowDialog message loop.
    internal async Task WithTransparentWindowsAsync(Func<Task> operation)
    {
        Form root = this;
        while (root.Owner is { IsDisposed: false } owner) root = owner;
        var windows = new Dictionary<Form, double>();
        void Snapshot(Form window)
        {
            if (window.IsDisposed || window.Disposing || !windows.TryAdd(window, window.Opacity)) return;
            foreach (var owned in window.OwnedForms) Snapshot(owned);
        }
        Snapshot(root);
        Snapshot(this);
        try
        {
            foreach (var window in windows.Keys) window.Opacity = 0;
            await Task.Delay(150);
            if (closed || IsDisposed || Disposing) return;
            await operation();
        }
        finally
        {
            foreach (var pair in windows)
                if (!pair.Key.IsDisposed && !pair.Key.Disposing && (!ReferenceEquals(pair.Key, this) || !closed))
                    pair.Key.Opacity = pair.Value;
            if (!closed && !IsDisposed && !Disposing) Activate();
        }
    }

    private async Task SelectAsync(bool capture)
    {
        if (busy) return;
        if (capture && (detectionArea.Width < 2 || detectionArea.Height < 2)) { UpdateSetupState(); return; }
        busy = true;
        UpdateSetupState();
        try
        {
            await WithTransparentWindowsAsync(async () =>
            {
                Rectangle selected;
                if (capture)
                {
                    using var selector = new TemplateCaptureForm(detectionArea.Size);
                    if (selector.ShowDialog() != DialogResult.OK) return;
                    selected = selector.SelectedArea;
                }
                else
                {
                    using var selector = new SelectionForm();
                    if (selector.ShowDialog() != DialogResult.OK) return;
                    selected = selector.SelectedArea;
                }
                await Task.Delay(150);
                if (closed || IsDisposed || Disposing) return;
                if (capture)
                {
                    using var image = ScreenCapture.Capture(selected);
                    SetTemplate(image);
                }
                else { detectionArea = selected; UpdateRegion(); }
            });
        }
        catch (Exception ex) { if (!closed && !IsDisposed) result.Text = ex.Message; }
        finally { busy = false; if (!closed && !IsDisposed) UpdateSetupState(); }
    }

    private void ImportTemplate()
    {
        if (busy) return;
        if (detectionArea.Width < 2 || detectionArea.Height < 2) { UpdateSetupState(); return; }
        busy = true;
        UpdateSetupState();
        try
        {
            using var dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp", CheckFileExists = true };
            if (dialog.ShowDialog(this) != DialogResult.OK || closed || IsDisposed) return;
            ValidateImportSource(new FileInfo(dialog.FileName).Length);
            using var image = Image.FromFile(dialog.FileName);
            ValidateImportSource(0, image.Size);
            using var bitmap = new Bitmap(image);
            SetTemplate(bitmap);
        }
        catch (Exception ex) { if (!closed && !IsDisposed) result.Text = ex.Message; }
        finally { busy = false; if (!closed && !IsDisposed) UpdateSetupState(); }
    }

    private void CropTemplate()
    {
        if (busy || templateBitmap == null) return;
        busy = true;
        UpdateSetupState();
        try
        {
            using var crop = new TemplateCropForm(templateBitmap, templateBitmap.Size);
            if (crop.ShowDialog(this) != DialogResult.OK || closed || IsDisposed) return;
            SetCroppedTemplate(templateBitmap, crop.CropArea);
        }
        catch (Exception ex) { if (!closed && !IsDisposed) result.Text = ex.Message; }
        finally { busy = false; if (!closed && !IsDisposed) UpdateSetupState(); }
    }

    internal void SetCroppedTemplate(Bitmap source, Rectangle crop)
    {
        ValidateImportSource(0, source.Size);
        if (crop.Width < 2 || crop.Height < 2 || (long)crop.Width * crop.Height > ClickCondition.MaximumTemplatePixels ||
            !new Rectangle(Point.Empty, source.Size).Contains(crop))
            throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
        using var cropped = source.Clone(crop, PixelFormat.Format24bppRgb);
        SetTemplate(cropped);
    }

    private void SetTemplate(Bitmap image)
    {
        if (image.Width < 2 || image.Height < 2 || (long)image.Width * image.Height > ClickCondition.MaximumTemplatePixels)
            throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);
        if (stream.Length > ClickCondition.MaximumTemplateBytes)
            throw new InvalidDataException(Localization.Get("ConditionErrorTemplateSize"));
        var newTemplate = Convert.ToBase64String(stream.ToArray());
        var newBitmap = new Bitmap(image);
        var newPreview = new Bitmap(image);
        template = newTemplate;
        templateSize = image.Size;
        var oldBitmap = templateBitmap;
        templateBitmap = newBitmap;
        oldBitmap?.Dispose();
        ReplacePreview(newPreview);
        enabled.Checked = true;
        result.Text = string.Empty;
        UpdateSetupState();
    }

    internal ClickCondition BuildCondition()
    {
        var condition = new ClickCondition { DetectionArea = detectionArea, TemplateImageBase64 = template,
            UseMultiScale = useMultiScale.Checked,
            Threshold = (double)threshold.Value, PollIntervalMs = (int)poll.Value,
            TimeoutMs = checked((int)(timeout.Value * 1000)), TriggerMode = (ConditionTriggerMode)mode.SelectedIndex };
        condition.Validate();
        return condition;
    }

    private void Save()
    {
        if (busy) return;
        try
        {
            ResultCondition = enabled.Checked ? BuildCondition() : null;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { result.Text = ex.Message; }
    }

    private async Task TestAsync()
    {
        if (busy) return;
        try
        {
            var condition = BuildCondition();
            busy = true;
            UpdateSetupState();
            await WithTransparentWindowsAsync(async () =>
            {
                var sample = await Task.Run(() =>
                {
                    var bitmap = ScreenCapture.Capture(condition.DetectionArea);
                    try
                    {
                        using var stream = new MemoryStream(Convert.FromBase64String(condition.TemplateImageBase64));
                        using var image = new Bitmap(stream);
                        var detected = OpenCvConditionDetector.Match(bitmap, image, condition.UseMultiScale);
                        if (detected.Score >= condition.Threshold)
                        {
                            using var graphics = Graphics.FromImage(bitmap);
                            using var pen = new Pen(Color.Lime, 2);
                            graphics.DrawRectangle(pen, detected.MatchBounds);
                        }
                        var bounds = detected.MatchBounds;
                        bounds.Offset(condition.DetectionArea.Location);
                        return (detected: detected with { MatchBounds = bounds }, bitmap);
                    }
                    catch { bitmap.Dispose(); throw; }
                });
                if (closed || IsDisposed || Disposing) { sample.bitmap.Dispose(); return; }
                ReplacePreview(sample.bitmap);
                result.Text = Localization.Get(sample.detected.Score >= condition.Threshold ? "ConditionMatched" : "ConditionWaiting") +
                    "  " + sample.detected.Score.ToString("F3") + "  " + sample.detected.MatchBounds + "\n" +
                    string.Format(Localization.Get("ConditionMatchScale"), sample.detected.Scale, sample.detected.MatchBounds.Width, sample.detected.MatchBounds.Height);
            });
        }
        catch (Exception ex) { if (!closed && !IsDisposed) result.Text = ex.Message; }
        finally { busy = false; if (!closed && !IsDisposed) UpdateSetupState(); }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        closed = true;
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { preview.Image?.Dispose(); templateBitmap?.Dispose(); }
        base.Dispose(disposing);
    }
}
