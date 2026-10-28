using System.Drawing.Imaging;

namespace MouseClicker;

internal sealed class ConditionSettingsForm : Form
{
    private readonly CheckBox enabled = new() { AutoSize = true };
    private readonly Label region = new() { AutoSize = true };
    private readonly PictureBox preview = new() { SizeMode = PictureBoxSizeMode.Zoom, Height = 150, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly NumericUpDown threshold = new() { Minimum = .1m, Maximum = 1, DecimalPlaces = 2, Increment = .01m, Value = .9m };
    private readonly NumericUpDown poll = new() { Minimum = 50, Maximum = 60000, Value = 200 };
    private readonly NumericUpDown timeout = new() { Minimum = 0, Maximum = 86400, Value = 60 };
    private readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
    private readonly Label result = new() { AutoSize = true, MaximumSize = new Size(540, 0) };
    private readonly Button test = new();
    private Rectangle detectionArea;
    private string template = string.Empty;
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
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoScroll = true, Padding = new Padding(16) };
        Controls.Add(table);
        enabled.Text = Localization.Get("ConditionEnabled");
        table.Controls.Add(enabled);
        var guide = new Button { Text = HelpContent.GetCatalog(Localization.Language).ConditionGuideText, AutoSize = true, Name = "conditionGuideButton" };
        guide.Click += (_, _) => { if (!busy) onHelpRequested?.Invoke(HelpTopic.ImageConditionSetup); };
        table.Controls.Add(guide);
        var select = new Button { Text = Localization.Get("ConditionRegion"), AutoSize = true };
        select.Click += async (_, _) => await SelectAsync(false);
        table.Controls.Add(select);
        table.Controls.Add(region);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        var capture = new Button { Text = Localization.Get("ConditionCapture"), AutoSize = true };
        var import = new Button { Text = Localization.Get("ConditionImport"), AutoSize = true };
        capture.Click += async (_, _) => await SelectAsync(true);
        import.Click += (_, _) => ImportTemplate();
        buttons.Controls.AddRange([capture, import]);
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
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        var save = new Button { Text = Localization.Get("ConditionSave"), AutoSize = true };
        var cancel = new Button { Text = Localization.Get("ConditionCancel"), DialogResult = DialogResult.Cancel, AutoSize = true };
        save.Click += (_, _) => Save();
        actions.Controls.AddRange([save, cancel]);
        table.Controls.Add(actions);
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
            ReplacePreview(new Bitmap(image));
        }
        UpdateRegion();
    }

    private static void AddField(TableLayoutPanel table, string key, Control control)
    {
        var row = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        row.Controls.Add(new Label { Text = Localization.Get(key), AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
        row.Controls.Add(control);
        table.Controls.Add(row);
    }

    private void UpdateRegion() => region.Text = detectionArea.ToString();
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
        busy = true;
        try
        {
            await WithTransparentWindowsAsync(async () =>
            {
                Rectangle selected;
                using (var selector = new SelectionForm())
                {
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
        finally { busy = false; }
    }

    private void ImportTemplate()
    {
        if (busy) return;
        using var dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp", CheckFileExists = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > ClickCondition.MaximumTemplateBytes) throw new InvalidDataException(Localization.Get("ConditionErrorTemplateSize"));
            using var image = Image.FromFile(dialog.FileName);
            if ((long)image.Width * image.Height > ClickCondition.MaximumTemplatePixels) throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
            using var bitmap = new Bitmap(image);
            SetTemplate(bitmap);
        }
        catch (Exception ex) { result.Text = ex.Message; }
    }

    private void SetTemplate(Bitmap image)
    {
        if (image.Width < 2 || image.Height < 2 || (long)image.Width * image.Height > ClickCondition.MaximumTemplatePixels)
            throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);
        if (stream.Length > ClickCondition.MaximumTemplateBytes)
            throw new InvalidDataException(Localization.Get("ConditionErrorTemplateSize"));
        template = Convert.ToBase64String(stream.ToArray());
        ReplacePreview(new Bitmap(image));
        enabled.Checked = true;
    }

    internal ClickCondition BuildCondition()
    {
        var condition = new ClickCondition { DetectionArea = detectionArea, TemplateImageBase64 = template,
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
            await WithTransparentWindowsAsync(async () =>
            {
                var sample = await Task.Run(() =>
                {
                    var bitmap = ScreenCapture.Capture(condition.DetectionArea);
                    try
                    {
                        using var stream = new MemoryStream(Convert.FromBase64String(condition.TemplateImageBase64));
                        using var image = new Bitmap(stream);
                        var detected = OpenCvConditionDetector.Match(bitmap, image);
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
                    "  " + sample.detected.Score.ToString("F3") + "  " + sample.detected.MatchBounds;
            });
        }
        catch (Exception ex) { if (!closed && !IsDisposed) result.Text = ex.Message; }
        finally { busy = false; }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        closed = true;
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) preview.Image?.Dispose();
        base.Dispose(disposing);
    }
}
