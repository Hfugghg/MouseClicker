using System.Drawing.Drawing2D;

namespace MouseClicker;

// All crop geometry uses original image pixels, independent of display scale and DPI.
internal static class TemplateCropGeometry
{
    internal static Size Maximum(Size source, Size requested)
    {
        int width = Math.Min(source.Width, requested.Width), height = Math.Min(source.Height, requested.Height);
        if (width < 2 || height < 2) throw new ArgumentOutOfRangeException(nameof(requested));
        return new Size(width, height);
    }

    internal static RectangleF Fit(Size source, Size viewport)
    {
        if (source.Width <= 0 || source.Height <= 0 || viewport.Width <= 0 || viewport.Height <= 0) return RectangleF.Empty;
        float scale = Math.Min((float)viewport.Width / source.Width, (float)viewport.Height / source.Height);
        var size = new SizeF(source.Width * scale, source.Height * scale);
        return new RectangleF((viewport.Width - size.Width) / 2, (viewport.Height - size.Height) / 2, size.Width, size.Height);
    }

    internal static Point ToImage(Point point, RectangleF displayed, Size source) => new(
        Math.Clamp((int)Math.Round((point.X - displayed.Left) * source.Width / displayed.Width), 0, source.Width),
        Math.Clamp((int)Math.Round((point.Y - displayed.Top) * source.Height / displayed.Height), 0, source.Height));

    internal static Rectangle Select(Point anchor, Point end, Size source, Size maximum)
    {
        var limit = Maximum(source, maximum);
        int maxWidth = limit.Width, maxHeight = limit.Height;
        int width = Math.Clamp(Math.Abs(end.X - anchor.X), 2, Math.Min(maxWidth, ClickCondition.MaximumTemplatePixels / 2));
        int height = Math.Clamp(Math.Abs(end.Y - anchor.Y), 2, Math.Min(maxHeight, ClickCondition.MaximumTemplatePixels / width));
        int x = end.X < anchor.X ? anchor.X - width : anchor.X;
        int y = end.Y < anchor.Y ? anchor.Y - height : anchor.Y;
        return new Rectangle(Math.Clamp(x, 0, source.Width - width), Math.Clamp(y, 0, source.Height - height), width, height);
    }

    internal static Rectangle Move(Rectangle initial, Point delta, Size source) => new(
        Math.Clamp(initial.X + delta.X, 0, source.Width - initial.Width),
        Math.Clamp(initial.Y + delta.Y, 0, source.Height - initial.Height), initial.Width, initial.Height);
}

internal sealed class TemplateCropForm : Form
{
    private readonly CropCanvas canvas;
    private readonly Label dimensions = new() { AutoSize = true, Dock = DockStyle.Fill };
    internal Rectangle CropArea => canvas.CropArea;

    internal TemplateCropForm(Bitmap source, Size maximumSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (Math.Min(source.Width, maximumSize.Width) < 2 || Math.Min(source.Height, maximumSize.Height) < 2)
            throw new ArgumentOutOfRangeException(nameof(maximumSize));
        maximumSize = TemplateCropGeometry.Maximum(source.Size, maximumSize);
        Text = Localization.Get("TemplateCropTitle");
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 620);
        MinimumSize = new Size(520, 420);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = Localization.Get("TemplateCropHint") + " " + Localization.Get("TemplateCropKeyboardHint"), AutoSize = true, Dock = DockStyle.Fill, MaximumSize = new Size(760, 0) }, 0, 0);
        canvas = new CropCanvas(source, maximumSize) { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 10) };
        layout.Controls.Add(canvas, 0, 1);
        layout.Controls.Add(dimensions, 0, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
        var confirm = new Button { Text = Localization.Get("TemplateCropConfirm"), AutoSize = true, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = Localization.Get("ConditionCancel"), AutoSize = true, DialogResult = DialogResult.Cancel };
        actions.Controls.AddRange([confirm, cancel]);
        layout.Controls.Add(actions, 0, 3);
        AcceptButton = confirm;
        CancelButton = cancel;
        canvas.ConfirmRequested += () => { DialogResult = DialogResult.OK; Close(); };
        canvas.CancelRequested += () => { DialogResult = DialogResult.Cancel; Close(); };
        void UpdateDimensions() => dimensions.Text = string.Format(Localization.Get("TemplateCropDimensions"),
            CropArea.Width, CropArea.Height, Math.Min(source.Width, maximumSize.Width), Math.Min(source.Height, maximumSize.Height));
        canvas.CropChanged += UpdateDimensions;
        UpdateDimensions();
    }

    private sealed class CropCanvas : Control
    {
        private readonly Bitmap source; // Borrowed: the caller owns this image.
        private readonly Size maximum;
        private Point anchor;
        private Rectangle initial;
        private bool moving;
        private bool dragging;
        private readonly Panel selectionActions = new() { Name = "selectionActions", BackColor = Color.White, Cursor = Cursors.Default };
        private readonly Button confirmSelection = new() { Name = "confirmSelectionButton", Text = "√", ForeColor = Color.ForestGreen, BackColor = Color.White, UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        private readonly Button cancelSelection = new() { Name = "cancelSelectionButton", Text = "×", ForeColor = Color.Firebrick, BackColor = Color.White, UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        private readonly ToolTip actionTips = new();
        internal Rectangle CropArea { get; private set; }
        internal event Action? CropChanged;
        internal event Action? ConfirmRequested;
        internal event Action? CancelRequested;
        private RectangleF ImageBounds => TemplateCropGeometry.Fit(source.Size, ClientSize);

        internal CropCanvas(Bitmap source, Size maximum)
        {
            this.source = source;
            this.maximum = TemplateCropGeometry.Maximum(source.Size, maximum);
            CropArea = TemplateCropGeometry.Select(Point.Empty, new Point(this.maximum.Width, this.maximum.Height), source.Size, this.maximum);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(35, 35, 35);
            Cursor = Cursors.Cross;
            Name = "templateCropCanvas";
            TabStop = true;
            SetStyle(ControlStyles.Selectable, true);
            AccessibleName = Localization.Get("TemplateCropTitle");
            confirmSelection.AccessibleName = Localization.Get("TemplateCropConfirm");
            cancelSelection.AccessibleName = Localization.Get("ConditionCancel");
            actionTips.SetToolTip(confirmSelection, confirmSelection.AccessibleName);
            actionTips.SetToolTip(cancelSelection, cancelSelection.AccessibleName);
            confirmSelection.Click += (_, _) => ConfirmRequested?.Invoke();
            cancelSelection.Click += (_, _) => CancelRequested?.Invoke();
            selectionActions.Controls.AddRange([confirmSelection, cancelSelection]);
            Controls.Add(selectionActions);
            UpdateActions();
        }

        private void UpdateActions()
        {
            if (source is null || selectionActions is null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            int target = (int)Math.Ceiling(32d * DeviceDpi / 96);
            int spacing = Math.Max(4, (int)Math.Ceiling(4d * DeviceDpi / 96));
            confirmSelection.Bounds = new Rectangle(0, 0, target, target);
            cancelSelection.Bounds = new Rectangle(target + spacing, 0, target, target);
            selectionActions.Bounds = SelectionActionLayout.Place(Rectangle.Ceiling(DisplayCrop()), ClientRectangle,
                new Size(target * 2 + spacing, target), (int)Math.Ceiling(14d * DeviceDpi / 96));
            selectionActions.BringToFront();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateActions();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateActions();
        }

        private RectangleF DisplayCrop()
        {
            var bounds = ImageBounds;
            return new RectangleF(bounds.Left + CropArea.X * bounds.Width / source.Width, bounds.Top + CropArea.Y * bounds.Height / source.Height,
                CropArea.Width * bounds.Width / source.Width, CropArea.Height * bounds.Height / source.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var bounds = ImageBounds;
            if (bounds.IsEmpty) return;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(source, bounds);
            var crop = DisplayCrop();
            using var shade = new SolidBrush(Color.FromArgb(100, Color.Black));
            using var outside = new Region(bounds);
            outside.Exclude(crop);
            e.Graphics.FillRegion(shade, outside);
            using var pen = new Pen(Color.DeepSkyBlue, 2);
            e.Graphics.DrawRectangle(pen, crop.X, crop.Y, crop.Width, crop.Height);
            float handle = 8f * DeviceDpi / 96;
            foreach (var corner in Corners(crop))
                e.Graphics.FillRectangle(Brushes.DeepSkyBlue, corner.X - handle / 2, corner.Y - handle / 2, handle, handle);
        }

        private static PointF[] Corners(RectangleF area) => [new(area.Left, area.Top), new(area.Right, area.Top), new(area.Left, area.Bottom), new(area.Right, area.Bottom)];

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || ImageBounds.IsEmpty) return;
            Focus();
            var crop = DisplayCrop();
            var corners = Corners(crop);
            float handle = 10f * DeviceDpi / 96;
            int corner = Array.FindIndex(corners, p => Math.Abs(e.X - p.X) <= handle && Math.Abs(e.Y - p.Y) <= handle);
            if (corner < 0 && !ImageBounds.Contains(e.Location)) return;
            initial = CropArea;
            moving = corner < 0 && crop.Contains(e.Location);
            anchor = corner >= 0 ? new Point(corner % 2 == 0 ? CropArea.Right : CropArea.Left, corner < 2 ? CropArea.Bottom : CropArea.Top)
                : TemplateCropGeometry.ToImage(e.Location, ImageBounds, source.Size);
            dragging = true;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging || ImageBounds.IsEmpty) return;
            var current = TemplateCropGeometry.ToImage(e.Location, ImageBounds, source.Size);
            CropArea = moving ? TemplateCropGeometry.Move(initial, new Point(current.X - anchor.X, current.Y - anchor.Y), source.Size)
                : TemplateCropGeometry.Select(anchor, current, source.Size, maximum);
            Invalidate();
            CropChanged?.Invoke();
            UpdateActions();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            OnMouseMove(e);
            dragging = false;
            Capture = false;
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture) dragging = false;
        }

        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int step = e.Shift ? 10 : 1;
            var delta = e.KeyCode switch
            {
                Keys.Left => new Point(-step, 0), Keys.Right => new Point(step, 0),
                Keys.Up => new Point(0, -step), Keys.Down => new Point(0, step), _ => Point.Empty
            };
            if (delta == Point.Empty) return;
            CropArea = TemplateCropGeometry.Move(CropArea, delta, source.Size);
            Invalidate();
            CropChanged?.Invoke();
            UpdateActions();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) actionTips.Dispose();
            base.Dispose(disposing);
        }
    }
}
