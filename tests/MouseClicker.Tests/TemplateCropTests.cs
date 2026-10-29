using System.Drawing;
using Xunit;

namespace MouseClicker.Tests;

public class TemplateCropTests
{
    private static void Sta(Action body)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { body(); } catch (Exception ex) { error = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FollowingButtonsConfirmOrCancelWithoutChangingSelectedPixels(bool confirm) => Sta(() =>
    {
        using var source = new Bitmap(200, 100);
        using var form = new TemplateCropForm(source, new Size(40, 30)) { Opacity = 0, ShowInTaskbar = false };
        var chosen = form.CropArea;
        form.Show();
        var name = confirm ? "confirmSelectionButton" : "cancelSelectionButton";
        var button = Assert.IsType<System.Windows.Forms.Button>(form.Controls.Find(name, true).Single());
        Assert.True(button.Width >= 32 * button.DeviceDpi / 96);
        Assert.False(string.IsNullOrWhiteSpace(button.AccessibleName));
        button.PerformClick();
        Assert.Equal(confirm ? System.Windows.Forms.DialogResult.OK : System.Windows.Forms.DialogResult.Cancel, form.DialogResult);
        Assert.Equal(chosen, form.CropArea);
        Assert.Equal(Color.FromArgb(0), source.GetPixel(0, 0));
    });

    [Fact]
    public void ActionButtonsFollowKeyboardMovementResizeAndBottomRightEdgeWithoutCoveringTinyCrop() => Sta(() =>
    {
        using var source = new Bitmap(1000, 1000);
        using var form = new TemplateCropForm(source, new Size(2, 2));
        var canvas = form.Controls.Find("templateCropCanvas", true).Single();
        canvas.Dock = System.Windows.Forms.DockStyle.None;
        canvas.Size = new Size(200, 200);
        var toolbar = canvas.Controls.Find("selectionActions", false).Single();
        var type = canvas.GetType();
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        void RefreshActions() => type.GetMethod("UpdateActions", flags)!.Invoke(canvas, null);
        void CheckPosition()
        {
            var image = TemplateCropGeometry.Fit(source.Size, canvas.ClientSize);
            var crop = form.CropArea;
            var displayed = Rectangle.Ceiling(new RectangleF(image.Left + crop.X * image.Width / source.Width, image.Top + crop.Y * image.Height / source.Height,
                crop.Width * image.Width / source.Width, crop.Height * image.Height / source.Height));
            Assert.True(canvas.ClientRectangle.Contains(toolbar.Bounds));
            Assert.False(Rectangle.Inflate(displayed, 8, 8).IntersectsWith(toolbar.Bounds));
        }
        RefreshActions();
        CheckPosition();
        type.GetProperty("CropArea", flags)!.SetValue(canvas, new Rectangle(998, 998, 2, 2));
        RefreshActions();
        CheckPosition();
        var old = toolbar.Bounds;
        canvas.Size = new Size(400, 400);
        Assert.NotEqual(old, toolbar.Bounds);
        CheckPosition();
        type.GetProperty("CropArea", flags)!.SetValue(canvas, new Rectangle(500, 500, 2, 2));
        RefreshActions();
        old = toolbar.Bounds;
        type.GetMethod("OnKeyDown", flags)!.Invoke(canvas, [new System.Windows.Forms.KeyEventArgs(System.Windows.Forms.Keys.Left | System.Windows.Forms.Keys.Shift)]);
        Assert.NotEqual(old, toolbar.Bounds);
        Assert.Equal(490, form.CropArea.X);
        CheckPosition();
    });

    [Theory]
    [InlineData(4000, 4000)]
    [InlineData(16000000, 2)]
    public void InitialAndDraggedSelectionStayWithinPixelBudget(int width, int height)
    {
        var source = new Size(width, height);
        var limit = TemplateCropGeometry.Maximum(source, source);
        Assert.InRange(limit.Width, 2, width);
        Assert.InRange(limit.Height, 2, height);
        Assert.Equal(source, limit);
        var crop = TemplateCropGeometry.Select(Point.Empty, new Point(width, height), source, source);
        Assert.InRange(crop.Width, 2, limit.Width);
        Assert.InRange(crop.Height, 2, limit.Height);
        Assert.True((long)crop.Width * crop.Height <= ClickCondition.MaximumTemplatePixels);
    }

    [Fact]
    public void LegalWideStripIsNotRestrictedByInitialCropAspectRatio()
    {
        var source = new Size(3840, 2160);
        Assert.Equal(source, TemplateCropGeometry.Maximum(source, source));
        Assert.Equal(new Rectangle(0, 0, 3840, 100), TemplateCropGeometry.Select(Point.Empty, new Point(3840, 100), source, source));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(16)]
    public void TinyDisplayedCropCanMoveByKeyboardAndStopsAtImageEdges(int pixels)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var source = new Bitmap(1000, 1000);
                using var form = new TemplateCropForm(source, new Size(pixels, pixels));
                var canvas = form.Controls.Find("templateCropCanvas", true).Single();
                canvas.Size = new Size(200, 200);
                Assert.True(canvas.TabStop);
                Assert.True(pixels * .2 < 4);
                void Key(System.Windows.Forms.Keys key) => canvas.GetType().GetMethod("OnKeyDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .Invoke(canvas, [new System.Windows.Forms.KeyEventArgs(key)]);
                Key(System.Windows.Forms.Keys.Right);
                Key(System.Windows.Forms.Keys.Down);
                Assert.Equal(new Rectangle(1, 1, pixels, pixels), form.CropArea);
                Key(System.Windows.Forms.Keys.Left | System.Windows.Forms.Keys.Shift);
                Assert.Equal(new Rectangle(0, 1, pixels, pixels), form.CropArea);
            }
            catch (Exception ex) { error = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    [Theory]
    [InlineData(400, 200, 400, 400, 0, 100, 400, 200)]
    [InlineData(200, 400, 400, 400, 100, 0, 200, 400)]
    [InlineData(400, 200, 800, 800, 0, 200, 800, 400)]
    public void FitPreservesAspectAndCentersLetterbox(int sw, int sh, int vw, int vh, int x, int y, int w, int h) =>
        Assert.Equal(new RectangleF(x, y, w, h), TemplateCropGeometry.Fit(new Size(sw, sh), new Size(vw, vh)));

    [Fact]
    public void MouseConversionAccountsForLetterboxScaleAndOutsideDragging()
    {
        var source = new Size(800, 400);
        var display = TemplateCropGeometry.Fit(source, new Size(400, 400));
        Assert.Equal(new Point(400, 200), TemplateCropGeometry.ToImage(new Point(200, 200), display, source));
        Assert.Equal(Point.Empty, TemplateCropGeometry.ToImage(new Point(-100, 40), display, source));
        Assert.Equal(new Point(800, 400), TemplateCropGeometry.ToImage(new Point(700, 500), display, source));
    }

    [Fact]
    public void ReverseSelectionUsesOriginalPixelsAndMaximumSize()
    {
        var crop = TemplateCropGeometry.Select(new Point(700, 350), new Point(100, 50), new Size(800, 400), new Size(300, 200));
        Assert.Equal(new Rectangle(400, 150, 300, 200), crop);
    }

    [Fact]
    public void SelectionAndMovementStayInsideImageWithoutResizing()
    {
        var source = new Size(80, 40);
        Assert.Equal(new Rectangle(78, 38, 2, 2), TemplateCropGeometry.Select(new Point(80, 40), new Point(80, 40), source, new Size(200, 200)));
        var original = new Rectangle(10, 10, 30, 20);
        Assert.Equal(new Rectangle(50, 0, 30, 20), TemplateCropGeometry.Move(original, new Point(500, -400), source));
    }

    [Fact]
    public void FormStartsWithValidCropAndDoesNotOwnSource()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var source = new Bitmap(80, 60);
                using (var form = new TemplateCropForm(source, new Size(30, 100)))
                    Assert.Equal(new Rectangle(0, 0, 30, 60), form.CropArea);
                Assert.Equal(Color.FromArgb(0), source.GetPixel(0, 0));
            }
            catch (Exception ex) { error = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
