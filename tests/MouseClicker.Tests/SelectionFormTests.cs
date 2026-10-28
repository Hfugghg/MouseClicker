using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace MouseClicker.Tests;

public class SelectionFormTests
{
    private sealed class SelectionProbe : SelectionForm
    {
        public void Down(Point point) => OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
        public void MoveTo(Point point) => OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, point.X, point.Y, 0));
        public void Up(Point point) => OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
    }

    private static void InSta(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Selection test did not finish.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void Window_CoversTheVirtualDesktopWithoutMaximizing() => InSta(() =>
    {
        using var form = new SelectionProbe();
        Assert.Equal(FormStartPosition.Manual, form.StartPosition);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(SystemInformation.VirtualScreen, form.Bounds);
    });

    [Theory]
    [InlineData(20, 30)]
    [InlineData(80, 30)]
    [InlineData(20, 90)]
    public void DegenerateSelection_RemainsOpenAndAllowsRetry(int x, int y) => InSta(() =>
    {
        using var form = new SelectionProbe();
        form.Down(new Point(20, 30));
        form.Up(new Point(x, y));
        Assert.Equal(DialogResult.None, form.DialogResult);
        Assert.Equal(Rectangle.Empty, form.SelectedArea);
        Assert.False(form.IsDisposed);
        form.Down(new Point(20, 30));
        form.Up(new Point(80, 90));
        Assert.Equal(DialogResult.OK, form.DialogResult);
        Assert.Equal(new Size(60, 60), form.SelectedArea.Size);
    });

    [Fact]
    public void MouseUpWithoutMouseDown_DoesNotCompleteSelection() => InSta(() =>
    {
        using var form = new SelectionProbe();
        form.Up(new Point(100, 100));
        Assert.Equal(DialogResult.None, form.DialogResult);
        Assert.Equal(Rectangle.Empty, form.SelectedArea);
    });

    [Theory]
    [InlineData(0, 0)]
    [InlineData(120, 80)]
    [InlineData(-400, -300)]
    public void ReverseDrag_UsesReleasePointAndScreenCoordinates_AndRoundTripsConfiguration(int left, int top) => InSta(() =>
    {
        using var form = new SelectionProbe();
        form.Bounds = new Rectangle(left, top, 300, 200);
        // 创建隐藏窗口句柄，让真实客户区到屏幕坐标转换参与验证。
        _ = form.Handle;
        var origin = form.PointToScreen(Point.Empty);
        Assert.Equal(new Point(left, top), origin);
        form.Down(new Point(100, 90));
        form.MoveTo(new Point(50, 40));
        form.Up(new Point(20, 10));
        Assert.Equal(DialogResult.OK, form.DialogResult);
        var expected = new Rectangle(left + 20, top + 10, 80, 80);
        Assert.Equal(expected, form.SelectedArea);

        var config = new ConfigData
        {
            Interval = 1000, Jitter = 0, SequenceIndex = 0,
            ClickAreas = new() { new ClickArea { Area = form.SelectedArea, Label = "screen area" } }
        };
        config.Validate(10, 600000, 0, 10000, 3);
        var restored = JsonSerializer.Deserialize<ConfigData>(JsonSerializer.Serialize(config))!;
        restored.Validate(10, 600000, 0, 10000, 3);
        Assert.Equal(expected, Assert.Single(restored.ClickAreas).Area);
    });

    [Fact]
    public void RetryDoesNotReusePreviousDragRectangle() => InSta(() =>
    {
        using var form = new SelectionProbe();
        form.Down(new Point(10, 10));
        form.MoveTo(new Point(70, 80));
        form.Up(new Point(10, 10));
        form.Down(new Point(30, 30));
        form.Up(new Point(30, 30));
        Assert.Equal(DialogResult.None, form.DialogResult);
        Assert.Equal(Rectangle.Empty, form.SelectedArea);
    });
}
