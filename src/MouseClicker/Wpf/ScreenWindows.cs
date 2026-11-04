using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DPoint = System.Drawing.Point;
using DRect = System.Drawing.Rectangle;
using DSize = System.Drawing.Size;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace MouseClicker.Wpf;

/// <summary>统一使用物理屏幕像素保存几何，仅在绘制时转换为 WPF 单位。</summary>
internal static class ScreenCoordinates
{
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out DPoint point);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    internal static DPoint CursorPosition => GetCursorPos(out var point) ? point : throw new System.ComponentModel.Win32Exception();
    internal static DRect VirtualScreen => System.Windows.Forms.SystemInformation.VirtualScreen;
    internal static DRect Select(DPoint start, DPoint end) => DRect.FromLTRB(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
    internal static bool IsValid(DRect area, DSize maximum) => area.Width >= 2 && area.Height >= 2 && area.Width <= maximum.Width && area.Height <= maximum.Height && (long)area.Width * area.Height <= ClickCondition.MaximumTemplatePixels;
    internal static Rect Local(Visual visual, DRect physical)
    {
        var a = visual.PointFromScreen(new Point(physical.Left, physical.Top));
        var b = visual.PointFromScreen(new Point(physical.Right, physical.Bottom));
        return new Rect(a, b);
    }
    internal static void Cover(Window window)
    {
        var bounds = VirtualScreen;
        if (!SetWindowPos(new WindowInteropHelper(window).Handle, new IntPtr(-1), bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x10))
            throw new System.ComponentModel.Win32Exception();
    }
}

internal abstract class ScreenWindow : Window
{
    protected ScreenWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        SourceInitialized += (_, _) => ScreenCoordinates.Cover(this);
        Loaded += (_, _) => ScreenCoordinates.Cover(this);
    }
    protected static void Label(DrawingContext dc, string text, Point position, double dpi)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei UI"), 14, Brushes.White, dpi);
        dc.DrawRectangle(Brushes.Black, null, new Rect(position, new System.Windows.Size(formatted.Width + 8, formatted.Height + 4)));
        dc.DrawText(formatted, new Point(position.X + 4, position.Y + 2));
    }
}

internal class SelectionWindow : ScreenWindow
{
    private DPoint anchor;
    private bool drawing;
    protected DRect Frame;
    internal DRect SelectedArea { get; private set; }
    internal SelectionWindow()
    {
        Background = new SolidColorBrush(Color.FromArgb(85, 0, 0, 0));
        Cursor = System.Windows.Input.Cursors.Cross;
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { DialogResult = false; e.Handled = true; } };
        MouseRightButtonDown += (_, _) => DialogResult = false;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        anchor = ScreenCoordinates.CursorPosition;
        drawing = CaptureMouse();
    }
    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!drawing) return;
        Frame = ScreenCoordinates.Select(anchor, ScreenCoordinates.CursorPosition);
        InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!drawing) return;
        Frame = ScreenCoordinates.Select(anchor, ScreenCoordinates.CursorPosition);
        drawing = false;
        ReleaseMouseCapture();
        if (Frame.Width <= 0 || Frame.Height <= 0) return;
        SelectedArea = Frame;
        DialogResult = true;
    }
    protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e)
    {
        drawing = false;
        base.OnLostMouseCapture(e);
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (PresentationSource.FromVisual(this) == null || Frame.Width <= 0 || Frame.Height <= 0) return;
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(65, 60, 130, 220)), new Pen(Brushes.White, 2), ScreenCoordinates.Local(this, Frame));
    }
}

internal sealed class TemplateCaptureWindow : ScreenWindow
{
    private readonly System.Windows.Controls.StackPanel actions;
    private readonly DSize maximum;
    private DRect frame;
    private DRect initial;
    private DPoint anchor;
    private bool dragging;
    private TemplateCaptureForm.Edges edges;
    internal DRect SelectedArea { get; private set; }
    internal TemplateCaptureWindow(DSize maximumSize)
    {
        if (maximumSize.Width < 2 || maximumSize.Height < 2) throw new ArgumentOutOfRangeException(nameof(maximumSize));
        maximum = maximumSize;
        Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        var bounds = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea ?? ScreenCoordinates.VirtualScreen;
        var size = new DSize(Math.Max(2, Math.Min(maximum.Width, bounds.Width / 2)), Math.Max(2, Math.Min(maximum.Height, bounds.Height / 2)));
        frame = new DRect(bounds.X + (bounds.Width - size.Width) / 2, bounds.Y + (bounds.Height - size.Height) / 2, size.Width, size.Height);
        actions = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        var ok = new System.Windows.Controls.Button { Content = Localization.Get("TemplateCropConfirm"), Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(4), IsDefault = true };
        var cancel = new System.Windows.Controls.Button { Content = Localization.Get("ConditionCancel"), Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(4), IsCancel = true };
        ok.Click += (_, _) => Confirm();
        actions.Children.Add(ok); actions.Children.Add(cancel); var canvas = new System.Windows.Controls.Canvas(); canvas.Children.Add(actions); Content = canvas;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { DialogResult = false; e.Handled = true; }
            else if (e.Key == Key.Enter) { Confirm(); e.Handled = true; }
            else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
            {
                int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
                var delta = e.Key switch { Key.Left => new DPoint(-step, 0), Key.Right => new DPoint(step, 0), Key.Up => new DPoint(0, -step), _ => new DPoint(0, step) };
                frame = TemplateCaptureForm.Transform(frame, delta, TemplateCaptureForm.Edges.None, maximum, ScreenCoordinates.VirtualScreen);
                InvalidateVisual(); e.Handled = true;
            }
        };
        MouseRightButtonDown += (_, _) => DialogResult = false;
    }
    private void Confirm()
    {
        if (!ScreenCoordinates.IsValid(frame, maximum)) return;
        SelectedArea = frame; DialogResult = true;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        anchor = ScreenCoordinates.CursorPosition;
        if (!DRect.Inflate(frame, 10, 10).Contains(anchor)) return;
        edges = TemplateCaptureForm.Edges.None;
        if (Math.Abs(anchor.X - frame.Left) <= 10) edges |= TemplateCaptureForm.Edges.Left;
        else if (Math.Abs(anchor.X - frame.Right) <= 10) edges |= TemplateCaptureForm.Edges.Right;
        if (Math.Abs(anchor.Y - frame.Top) <= 10) edges |= TemplateCaptureForm.Edges.Top;
        else if (Math.Abs(anchor.Y - frame.Bottom) <= 10) edges |= TemplateCaptureForm.Edges.Bottom;
        initial = frame; dragging = CaptureMouse();
    }
    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var cursor = ScreenCoordinates.CursorPosition;
        bool horizontal = Math.Abs(cursor.X - frame.Left) <= 10 || Math.Abs(cursor.X - frame.Right) <= 10;
        bool vertical = Math.Abs(cursor.Y - frame.Top) <= 10 || Math.Abs(cursor.Y - frame.Bottom) <= 10;
        bool diagonal = (Math.Abs(cursor.X - frame.Left) <= 10) == (Math.Abs(cursor.Y - frame.Top) <= 10);
        Cursor = horizontal && vertical ? (diagonal ? System.Windows.Input.Cursors.SizeNWSE : System.Windows.Input.Cursors.SizeNESW)
            : horizontal ? System.Windows.Input.Cursors.SizeWE : vertical ? System.Windows.Input.Cursors.SizeNS
            : frame.Contains(cursor) ? System.Windows.Input.Cursors.SizeAll : System.Windows.Input.Cursors.Arrow;
        if (!dragging) return;
        var p = ScreenCoordinates.CursorPosition;
        frame = TemplateCaptureForm.Transform(initial, new DPoint(p.X - anchor.X, p.Y - anchor.Y), edges, maximum, ScreenCoordinates.VirtualScreen);
        InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { base.OnMouseLeftButtonUp(e); dragging = false; ReleaseMouseCapture(); }
    protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { dragging = false; base.OnLostMouseCapture(e); }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (PresentationSource.FromVisual(this) == null) return;
        var dpiScale = VisualTreeHelper.GetDpi(this);
        var viewport = System.Windows.Forms.Screen.FromRectangle(frame).WorkingArea;
        var placement = SelectionActionLayout.Place(frame, viewport, new DSize(Math.Max(160, (int)Math.Ceiling(actions.ActualWidth * dpiScale.DpiScaleX)), Math.Max(48, (int)Math.Ceiling(actions.ActualHeight * dpiScale.DpiScaleY))), 12);
        var local = PointFromScreen(new Point(placement.X, placement.Y));
        System.Windows.Controls.Canvas.SetLeft(actions, local.X);
        System.Windows.Controls.Canvas.SetTop(actions, local.Y);
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)), new Pen(Brushes.DarkOrange, 3), ScreenCoordinates.Local(this, frame));
        foreach (var corner in WpfCropGeometry.Corners(ScreenCoordinates.Local(this, frame)))
            dc.DrawRectangle(Brushes.White, null, new Rect(corner.X - 4, corner.Y - 4, 8, 8));
        Label(dc, Localization.Get("TemplateCaptureInstructions") + "\n" + string.Format(Localization.Get("TemplateCaptureSize"), frame.Width, frame.Height, maximum.Width, maximum.Height), new Point(20, 20), VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }
}

internal sealed class OverlayWindow : ScreenWindow
{
    [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    internal bool RequireCaptureExclusion { get; set; }
    internal bool IsExcludedFromCapture { get; private set; }
    internal bool ShowClickAreas { get; set; } = true;
    internal bool ShowDetectionAreas { get; set; }
    internal List<ClickArea> AreasToDraw { get; set; } = [];
    private ClickArea? preview, active;
    internal ClickArea? PreviewArea { get => preview; set { preview = value; InvalidateVisual(); } }
    internal ClickArea? ActiveArea { get => active; set { active = value; InvalidateVisual(); } }
    internal OverlayWindow()
    {
        ShowActivated = false;
        IsHitTestVisible = false;
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, -20, GetWindowLong(hwnd, -20) | 0x20 | 0x08000000 | 0x80);
            IsExcludedFromCapture = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) && SetWindowDisplayAffinity(hwnd, 0x11);
        };
    }
    internal bool CanShowDuringCondition() { _ = new WindowInteropHelper(this).EnsureHandle(); return IsExcludedFromCapture; }
    public new void Show() { if (!RequireCaptureExclusion || CanShowDuringCondition()) base.Show(); }
    internal void Invalidate() => InvalidateVisual();
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (PresentationSource.FromVisual(this) == null) return;
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (ShowClickAreas)
            foreach (var area in OverlayForm.GetDrawOrder(AreasToDraw, preview, active))
            {
                var rect = ScreenCoordinates.Local(this, area.Area);
                var brush = ReferenceEquals(area, active) ? Brushes.LimeGreen : ReferenceEquals(area, preview) ? Brushes.DodgerBlue : Brushes.Red;
                dc.DrawRectangle(null, new Pen(brush, ReferenceEquals(area, active) || ReferenceEquals(area, preview) ? 3 : 2), rect);
                Label(dc, area.Label, new Point(rect.Left + 4, rect.Top + 4), dpi);
            }
        if (ShowDetectionAreas)
            foreach (var area in OverlayForm.GetDetectionAreas(AreasToDraw))
            {
                var rect = ScreenCoordinates.Local(this, area.Condition!.DetectionArea);
                dc.DrawRectangle(null, new Pen(Brushes.DarkOrange, 2) { DashStyle = DashStyles.Dash }, rect);
                Label(dc, string.Format(Localization.Get("DetectionOverlayLabel"), area.Label), new Point(rect.Left + 4, rect.Top + 4), dpi);
            }
    }
}



