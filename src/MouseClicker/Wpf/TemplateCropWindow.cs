using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DPoint = System.Drawing.Point;
using DRect = System.Drawing.Rectangle;
using DSize = System.Drawing.Size;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;

namespace MouseClicker.Wpf;

/// <summary>使用原图像素裁剪，预览图像独立于调用方位图的生命周期。</summary>
internal sealed class TemplateCropWindow : Window
{
    private readonly CropSurface surface;
    internal DRect CropArea => surface.CropArea;
    internal TemplateCropWindow(System.Drawing.Bitmap source, DSize maximumSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        var maximum = TemplateCropGeometry.Maximum(source.Size, maximumSize);
        Title = Localization.Get("TemplateCropTitle");
        Width = 820; Height = 620; MinWidth = 520; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var layout = new System.Windows.Controls.DockPanel { Margin = new Thickness(12) };
        var hint = new System.Windows.Controls.TextBlock { Text = Localization.Get("TemplateCropHint") + " " + Localization.Get("TemplateCropKeyboardHint"), TextWrapping = TextWrapping.Wrap };
        System.Windows.Controls.DockPanel.SetDock(hint, System.Windows.Controls.Dock.Top); layout.Children.Add(hint);
        var actions = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        var ok = new System.Windows.Controls.Button { Content = Localization.Get("TemplateCropConfirm"), Padding = new Thickness(16, 8, 16, 8), IsDefault = true };
        var cancel = new System.Windows.Controls.Button { Content = Localization.Get("ConditionCancel"), Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        ok.Click += (_, _) => DialogResult = true;
        actions.Children.Add(ok); actions.Children.Add(cancel);
        System.Windows.Controls.DockPanel.SetDock(actions, System.Windows.Controls.Dock.Bottom); layout.Children.Add(actions);
        var dimensions = new System.Windows.Controls.TextBlock();
        System.Windows.Controls.DockPanel.SetDock(dimensions, System.Windows.Controls.Dock.Bottom); layout.Children.Add(dimensions);
        surface = new CropSurface(source, maximum);
        surface.ConfirmRequested += () => DialogResult = true;
        surface.CancelRequested += () => DialogResult = false;
        void UpdateDimensions() => dimensions.Text = string.Format(Localization.Get("TemplateCropDimensions"), CropArea.Width, CropArea.Height, maximum.Width, maximum.Height);
        surface.Changed += UpdateDimensions;
        UpdateDimensions(); layout.Children.Add(surface); Content = layout;
        Loaded += (_, _) => surface.Focus();
    }
    private sealed class CropSurface : System.Windows.Controls.Canvas
    {
        private readonly BitmapImage image;
        private readonly System.Windows.Controls.StackPanel actions = new() { Orientation = System.Windows.Controls.Orientation.Horizontal };
        internal event Action? ConfirmRequested;
        internal event Action? CancelRequested;
        private readonly DSize source, maximum;
        private DPoint anchor;
        private DRect initial;
        private bool moving, dragging;
        internal event Action? Changed;
        internal DRect CropArea { get; private set; }
        private System.Drawing.RectangleF ImageBounds => TemplateCropGeometry.Fit(source, new DSize(Math.Max(1, (int)ActualWidth), Math.Max(1, (int)ActualHeight)));
        internal CropSurface(System.Drawing.Bitmap bitmap, DSize maximum)
        {
            source = bitmap.Size; this.maximum = maximum;
            using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); stream.Position = 0;
            image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze();
            CropArea = TemplateCropGeometry.Select(DPoint.Empty, new DPoint(maximum.Width, maximum.Height), source, maximum);
            Focusable = true; Cursor = System.Windows.Input.Cursors.Cross;
            var confirm = new System.Windows.Controls.Button { Content = "√", ToolTip = Localization.Get("TemplateCropConfirm"), Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(2) };
            var cancel = new System.Windows.Controls.Button { Content = "×", ToolTip = Localization.Get("ConditionCancel"), Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(2) };
            System.Windows.Automation.AutomationProperties.SetName(confirm, Localization.Get("TemplateCropConfirm"));
            System.Windows.Automation.AutomationProperties.SetName(cancel, Localization.Get("ConditionCancel"));
            confirm.Click += (_, _) => ConfirmRequested?.Invoke();
            cancel.Click += (_, _) => CancelRequested?.Invoke();
            actions.Children.Add(confirm); actions.Children.Add(cancel); Children.Add(actions);
            SizeChanged += (_, _) => { UpdateActions(); InvalidateVisual(); };
        }
        private DPoint ImagePoint(System.Windows.Input.MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            return TemplateCropGeometry.ToImage(new DPoint((int)Math.Round(point.X), (int)Math.Round(point.Y)), ImageBounds, source);
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (actions.IsMouseOver) return;
            Focus();
            var crop = DisplayCrop();
            var point = e.GetPosition(this);
            int corner = WpfCropGeometry.HitCorner(crop, point);
            var bounds = ImageBounds;
            if (corner < 0 && !new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height).Contains(point)) return;
            initial = CropArea;
            // 命中角点时固定对角，允许直接缩小初始全图选区。
            anchor = corner >= 0 ? WpfCropGeometry.OppositeCorner(CropArea, corner) : ImagePoint(e);
            moving = corner < 0 && crop.Contains(point) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            dragging = CaptureMouse(); e.Handled = true;
        }
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int corner = WpfCropGeometry.HitCorner(DisplayCrop(), e.GetPosition(this));
            Cursor = corner >= 0 ? (corner is 0 or 3 ? System.Windows.Input.Cursors.SizeNWSE : System.Windows.Input.Cursors.SizeNESW)
                : DisplayCrop().Contains(e.GetPosition(this)) ? System.Windows.Input.Cursors.SizeAll : System.Windows.Input.Cursors.Cross;
            if (!dragging) return;
            var point = ImagePoint(e);
            CropArea = moving ? TemplateCropGeometry.Move(initial, new DPoint(point.X - anchor.X, point.Y - anchor.Y), source) : TemplateCropGeometry.Select(anchor, point, source, maximum);
            InvalidateVisual(); UpdateActions(); Changed?.Invoke();
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { OnMouseMove(e); dragging = false; ReleaseMouseCapture(); base.OnMouseLeftButtonUp(e); }
        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e) { dragging = false; base.OnLostMouseCapture(e); }
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down)) return;
            int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
            var delta = e.Key switch { Key.Left => new DPoint(-step, 0), Key.Right => new DPoint(step, 0), Key.Up => new DPoint(0, -step), _ => new DPoint(0, step) };
            CropArea = TemplateCropGeometry.Move(CropArea, delta, source); InvalidateVisual(); UpdateActions(); Changed?.Invoke(); e.Handled = true;
        }
        private Rect DisplayCrop()
        {
            var bounds = ImageBounds;
            double scale = bounds.Width / source.Width;
            return new Rect(bounds.X + CropArea.X * scale, bounds.Y + CropArea.Y * scale, CropArea.Width * scale, CropArea.Height * scale);
        }
        private void UpdateActions()
        {
            var placement = WpfCropGeometry.PlaceActions(DisplayCrop(), new System.Windows.Size(ActualWidth, ActualHeight), new System.Windows.Size(Math.Max(100, actions.ActualWidth), Math.Max(36, actions.ActualHeight)));
            SetLeft(actions, placement.X); SetTop(actions, placement.Y);
        }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc); dc.DrawRectangle(Brushes.DimGray, null, new Rect(RenderSize));
            var bounds = ImageBounds;
            dc.DrawImage(image, new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));
            double scale = bounds.Width / source.Width;
            var crop = DisplayCrop();
            dc.DrawRectangle(null, new Pen(Brushes.DarkOrange, 2), crop);
            foreach (var corner in WpfCropGeometry.Corners(crop)) dc.DrawRectangle(Brushes.DarkOrange, null, new Rect(corner.X - 4, corner.Y - 4, 8, 8));
            UpdateActions();
        }
    }
}

/// <summary>裁剪交互几何使用画布逻辑单位，输出裁剪仍使用原图像素。</summary>
internal static class WpfCropGeometry
{
    internal static System.Windows.Point[] Corners(Rect crop) => [crop.TopLeft, crop.TopRight, crop.BottomLeft, crop.BottomRight];
    internal static int HitCorner(Rect crop, System.Windows.Point point) => Array.FindIndex(Corners(crop), p => Math.Abs(p.X - point.X) <= 10 && Math.Abs(p.Y - point.Y) <= 10);
    internal static DPoint OppositeCorner(DRect crop, int corner) => new(corner % 2 == 0 ? crop.Right : crop.Left, corner < 2 ? crop.Bottom : crop.Top);
    internal static DRect PlaceActions(Rect crop, System.Windows.Size viewport, System.Windows.Size toolbar) => SelectionActionLayout.Place(
        DRect.FromLTRB((int)Math.Floor(crop.Left), (int)Math.Floor(crop.Top), (int)Math.Ceiling(crop.Right), (int)Math.Ceiling(crop.Bottom)),
        new DRect(0, 0, Math.Max(1, (int)viewport.Width), Math.Max(1, (int)viewport.Height)),
        new DSize((int)Math.Ceiling(toolbar.Width), (int)Math.Ceiling(toolbar.Height)), 8);
}
