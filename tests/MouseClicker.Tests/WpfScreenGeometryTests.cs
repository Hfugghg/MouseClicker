using System.Drawing;
using MouseClicker.Wpf;
using Xunit;

namespace MouseClicker.Tests;

public class WpfScreenGeometryTests
{
    [Theory]
    [InlineData(-1600, -900, -1200, -500, -1600, -900, 400, 400)]
    [InlineData(300, 400, -100, -200, -100, -200, 400, 600)]
    [InlineData(10, 20, 10, 20, 10, 20, 0, 0)]
    public void SelectionPreservesPhysicalCoordinates(int ax, int ay, int bx, int by, int x, int y, int width, int height)
    {
        Assert.Equal(new Rectangle(x, y, width, height), ScreenCoordinates.Select(new Point(ax, ay), new Point(bx, by)));
    }
    [Theory]
    [InlineData(1, 20, false)]
    [InlineData(2, 2, true)]
    [InlineData(100, 100, true)]
    [InlineData(101, 100, false)]
    public void TemplateSelectionEnforcesMinimumAndMaximum(int width, int height, bool valid)
    {
        Assert.Equal(valid, ScreenCoordinates.IsValid(new Rectangle(-100, -200, width, height), new Size(100, 100)));
    }
    [Fact]
    public void TemplateSelectionRejectsExcessivePixelCount()
    {
        Assert.False(ScreenCoordinates.IsValid(new Rectangle(0, 0, 10000, 10000), new Size(10000, 10000)));
    }
}

public class WpfScreenWindowTests
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [Fact]
    public void SelectionHandleCoversPhysicalVirtualDesktop()
    {
        RunSta(() =>
        {
            var window = new SelectionWindow();
            try
            {
                var handle = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
                Assert.True(GetWindowRect(handle, out var rect));
                Assert.Equal(ScreenCoordinates.VirtualScreen, Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom));
            }
            finally { window.Close(); }
        });
    }
    [Fact]
    public void CropOwnsImageAndClampsToMaximum()
    {
        RunSta(() =>
        {
            using var bitmap = new Bitmap(200, 100);
            var window = new TemplateCropWindow(bitmap, new Size(40, 30));
            try
            {
                Assert.Equal(new Size(40, 30), window.CropArea.Size);
                Assert.True(new Rectangle(0, 0, 200, 100).Contains(window.CropArea));
            }
            finally { window.Close(); }
        });
    }
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "WPF 窗口测试超时");
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}

public class WpfCropInteractionGeometryTests
{
    [Theory]
    [InlineData(0, 20, 20, 80, 80, 20, 20)]
    [InlineData(1, 80, 20, 80, 80, 0, 20)]
    [InlineData(2, 20, 80, 80, 80, 20, 0)]
    [InlineData(3, 80, 80, 80, 80, 0, 0)]
    public void DraggingEachCornerShrinksFullImageWithoutShift(int corner, int endX, int endY, int width, int height, int x, int y)
    {
        var full = new Rectangle(0, 0, 100, 100);
        var anchor = WpfCropGeometry.OppositeCorner(full, corner);
        Assert.Equal(new Rectangle(x, y, width, height), TemplateCropGeometry.Select(anchor, new Point(endX, endY), full.Size, full.Size));
        Assert.Equal(corner, WpfCropGeometry.HitCorner(new System.Windows.Rect(0, 0, 100, 100), WpfCropGeometry.Corners(new System.Windows.Rect(0, 0, 100, 100))[corner]));
    }
    [Fact]
    public void CornerToleranceUsesLogicalUnitsAtScaledPreview()
    {
        var preview = new System.Windows.Rect(50, 30, 500, 300);
        Assert.Equal(3, WpfCropGeometry.HitCorner(preview, new System.Windows.Point(557, 337)));
        Assert.Equal(-1, WpfCropGeometry.HitCorner(preview, new System.Windows.Point(565, 345)));
    }
    [Fact]
    public void ActionsFollowSelectionAndStayInsideViewport()
    {
        var viewport = new System.Windows.Size(500, 400);
        var toolbar = new System.Windows.Size(100, 40);
        var first = WpfCropGeometry.PlaceActions(new System.Windows.Rect(20, 20, 100, 100), viewport, toolbar);
        var moved = WpfCropGeometry.PlaceActions(new System.Windows.Rect(100, 80, 100, 100), viewport, toolbar);
        Assert.Equal(new Point(80, 60), new Point(moved.X - first.X, moved.Y - first.Y));
        var bottom = WpfCropGeometry.PlaceActions(new System.Windows.Rect(400, 300, 100, 100), viewport, toolbar);
        Assert.True(new Rectangle(0, 0, 500, 400).Contains(bottom));
        Assert.True(bottom.Bottom < 300);
    }
}
