using System.Drawing.Drawing2D;

namespace MouseClicker;

internal sealed class TemplateCaptureForm : Form
{
    [Flags]
    internal enum Edges { None = 0, Left = 1, Right = 2, Top = 4, Bottom = 8 }
    private const int Grip = 9;
    private readonly Size maximum;
    private Rectangle frame;
    private Rectangle dragFrame;
    private Point dragStart;
    private Edges dragEdges;
    private bool dragging;
    private string error = string.Empty;
    private readonly Panel actions = new() { BackColor = Color.White };
    private readonly Button confirm = new() { Name = "confirmSelectionButton", Text = "√", BackColor = Color.White, ForeColor = Color.ForestGreen, UseVisualStyleBackColor = false };
    private readonly Button cancel = new() { Name = "cancelSelectionButton", Text = "×", BackColor = Color.White, ForeColor = Color.Firebrick, UseVisualStyleBackColor = false };
    private readonly ToolTip tips = new();
    private bool actionsReady;
    internal Rectangle SelectedArea { get; private set; }
    internal Rectangle Frame => frame;
    internal Rectangle MoveHandle
    {
        get
        {
            int x = Math.Clamp(frame.Left + frame.Width / 2 - 22, Left + 4, Math.Max(Left + 4, Right - 48));
            int y = frame.Top - 30 >= Top ? frame.Top - 30 : frame.Bottom + 6;
            return new Rectangle(x, Math.Clamp(y, Top + 4, Math.Max(Top + 4, Bottom - 28)), 44, 24);
        }
    }

    internal TemplateCaptureForm(Size maximumSize)
    {
        if (maximumSize.Width < 2 || maximumSize.Height < 2) throw new ArgumentOutOfRangeException(nameof(maximumSize));
        maximum = maximumSize;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        Opacity = .55;
        KeyPreview = true;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        var screen = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
        var size = FitSize(new Size(Math.Max(2, Math.Min(maximum.Width, screen.Width - 80)),
            Math.Max(2, Math.Min(maximum.Height, screen.Height - 180))), maximum);
        frame = new Rectangle(screen.Left + (screen.Width - size.Width) / 2,
            screen.Top + (screen.Height - size.Height) / 2, size.Width, size.Height);
        confirm.AccessibleName = Localization.Get("TemplateCropConfirm");
        cancel.AccessibleName = Localization.Get("ConditionCancel");
        tips.SetToolTip(confirm, confirm.AccessibleName);
        tips.SetToolTip(cancel, cancel.AccessibleName);
        confirm.Click += (_, _) => TryConfirm();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        actions.Controls.AddRange([confirm, cancel]);
        Controls.Add(actions);
        actionsReady = true;
        UpdateActionLayout();
    }

    private void UpdateActionLayout()
    {
        int unit = Math.Max(32, (int)Math.Round(40 * DeviceDpi / 96d));
        int gap = Math.Max(6, (int)Math.Round(6 * DeviceDpi / 96d));
        confirm.Bounds = new Rectangle(0, 0, unit, unit);
        cancel.Bounds = new Rectangle(unit + gap, 0, unit, unit);
        var viewport = Rectangle.Intersect(Screen.FromRectangle(frame).WorkingArea, Bounds);
        if (viewport.IsEmpty) viewport = Rectangle.Intersect(Screen.FromRectangle(frame).Bounds, Bounds);
        var reserved = Rectangle.Union(Rectangle.Inflate(frame, Grip, Grip), MoveHandle);
        var placement = SelectionActionLayout.Place(reserved, viewport, new Size(unit * 2 + gap, unit), gap);
        placement.Offset(-Left, -Top);
        actions.Bounds = placement;
        actions.BringToFront();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (actionsReady) UpdateActionLayout();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (actionsReady) UpdateActionLayout();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (actionsReady) UpdateActionLayout();
    }

    internal static Size FitSize(Size desired, Size maximum)
    {
        int width = Math.Clamp(desired.Width, 2, Math.Max(2, maximum.Width));
        int height = Math.Clamp(desired.Height, 2, Math.Max(2, maximum.Height));
        // Keep both independent dimensions and the decoded-template pixel budget valid.
        width = Math.Min(width, ClickCondition.MaximumTemplatePixels / 2);
        height = Math.Min(height, ClickCondition.MaximumTemplatePixels / width);
        return new Size(width, height);
    }

    internal static Rectangle Transform(Rectangle original, Point delta, Edges edges, Size maximum, Rectangle desktop)
    {
        if (edges == Edges.None)
            return new Rectangle(Math.Clamp(original.X + delta.X, desktop.Left, Math.Max(desktop.Left, desktop.Right - original.Width)),
                Math.Clamp(original.Y + delta.Y, desktop.Top, Math.Max(desktop.Top, desktop.Bottom - original.Height)), original.Width, original.Height);
        int width = original.Width + ((edges & Edges.Left) != 0 ? -delta.X : (edges & Edges.Right) != 0 ? delta.X : 0);
        int height = original.Height + ((edges & Edges.Top) != 0 ? -delta.Y : (edges & Edges.Bottom) != 0 ? delta.Y : 0);
        var size = FitSize(new Size(width, height), maximum);
        int left = (edges & Edges.Left) != 0 ? original.Right - size.Width : original.Left;
        int top = (edges & Edges.Top) != 0 ? original.Bottom - size.Height : original.Top;
        return new Rectangle(left, top, size.Width, size.Height);
    }

    private Point ScreenPoint(MouseEventArgs e) => new(e.X + Left, e.Y + Top);

    private Edges HitEdges(Point p)
    {
        Edges edges = Edges.None;
        if (Math.Abs(p.X - frame.Left) <= Grip) edges |= Edges.Left;
        else if (Math.Abs(p.X - frame.Right) <= Grip) edges |= Edges.Right;
        if (Math.Abs(p.Y - frame.Top) <= Grip) edges |= Edges.Top;
        else if (Math.Abs(p.Y - frame.Bottom) <= Grip) edges |= Edges.Bottom;
        return edges;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        var p = ScreenPoint(e);
        var hit = Rectangle.Inflate(frame, Grip, Grip);
        bool moveHandle = MoveHandle.Contains(p);
        if (!moveHandle && !hit.Contains(p)) return;
        dragEdges = moveHandle ? Edges.None : HitEdges(p);
        dragFrame = frame;
        dragStart = p;
        dragging = true;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var p = ScreenPoint(e);
        if (dragging)
        {
            frame = Transform(dragFrame, new Point(p.X - dragStart.X, p.Y - dragStart.Y), dragEdges, maximum, Bounds);
            UpdateActionLayout();
            error = string.Empty;
            Invalidate();
        }
        var edges = HitEdges(p);
        Cursor = MoveHandle.Contains(p) ? Cursors.SizeAll : edges switch
        {
            Edges.Left or Edges.Right => Cursors.SizeWE,
            Edges.Top or Edges.Bottom => Cursors.SizeNS,
            Edges.Left | Edges.Top or Edges.Right | Edges.Bottom => Cursors.SizeNWSE,
            Edges.Right | Edges.Top or Edges.Left | Edges.Bottom => Cursors.SizeNESW,
            _ => frame.Contains(p) ? Cursors.SizeAll : Cursors.Default
        };
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        if (dragging) OnMouseMove(e);
        dragging = false;
        Capture = false;
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        if (!Capture) dragging = false;
        base.OnMouseCaptureChanged(e);
    }

    internal bool TryConfirm()
    {
        try
        {
            ScreenCapture.ValidateArea(frame, Screen.AllScreens.Select(s => s.Bounds));
            if (frame.Width < 2 || frame.Height < 2 || frame.Width > maximum.Width || frame.Height > maximum.Height ||
                (long)frame.Width * frame.Height > ClickCondition.MaximumTemplatePixels)
                throw new InvalidDataException();
            SelectedArea = frame;
            DialogResult = DialogResult.OK;
            Close();
            return true;
        }
        catch (InvalidDataException)
        {
            error = Localization.Get("TemplateCaptureInvalidArea");
            Invalidate();
            return false;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); return true; }
        if (keyData == Keys.Enter) { TryConfirm(); return true; }
        var key = keyData & Keys.KeyCode;
        if ((keyData & ~(Keys.KeyCode | Keys.Shift)) == Keys.None && key is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            int step = (keyData & Keys.Shift) != 0 ? 10 : 1;
            var delta = key switch { Keys.Left => new Point(-step, 0), Keys.Right => new Point(step, 0),
                Keys.Up => new Point(0, -step), _ => new Point(0, step) };
            frame = Transform(frame, delta, Edges.None, maximum, Bounds);
            UpdateActionLayout();
            error = string.Empty;
            Invalidate();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) tips.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var bounds = new Rectangle(frame.X - Left, frame.Y - Top, frame.Width, frame.Height);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(Color.FromArgb(70, 255, 255, 255));
        e.Graphics.FillRectangle(fill, bounds);
        using var border = new Pen(Color.DarkOrange, 3);
        e.Graphics.DrawRectangle(border, bounds);
        var move = MoveHandle;
        move.Offset(-Left, -Top);
        e.Graphics.FillRectangle(Brushes.White, move);
        using var moveFont = new Font(Font.FontFamily, 12, FontStyle.Bold);
        using var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString("↔", moveFont, Brushes.Black, move, centered);
        foreach (int x in new[] { bounds.Left, bounds.Left + bounds.Width / 2, bounds.Right })
            foreach (int y in new[] { bounds.Top, bounds.Top + bounds.Height / 2, bounds.Bottom })
                if (x != bounds.Left + bounds.Width / 2 || y != bounds.Top + bounds.Height / 2)
                    e.Graphics.FillRectangle(Brushes.White, x - 4, y - 4, 8, 8);
        var primary = Screen.PrimaryScreen?.Bounds ?? Screen.AllScreens[0].Bounds;
        var info = new Rectangle(primary.Left - Left + 20, primary.Top - Top + 16, Math.Max(100, primary.Width - 40), 135);
        using var font = new Font(Font.FontFamily, 12, FontStyle.Bold);
        e.Graphics.DrawString(Localization.Get("TemplateCaptureInstructions") + "\n" +
            string.Format(Localization.Get("TemplateCaptureSize"), frame.Width, frame.Height, maximum.Width, maximum.Height) +
            (error.Length == 0 ? string.Empty : "\n" + error), font, Brushes.White, info);
    }
}
