using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace MouseClicker.Tests;

public class TemplateCaptureFormTests
{
    [Theory]
    [InlineData(-1000, -100, -1200, -300, 500, 400)]
    [InlineData(-1000, 70, -1200, -300, 500, 400)]
    [InlineData(-1600, -300, -1600, -300, 1200, 900)]
    public void ActionToolbarPlacementStaysInCurrentViewportIncludingNegativeScreens(int x, int y, int vx, int vy, int vw, int vh)
    {
        var viewport = new Rectangle(vx, vy, vw, vh);
        var selection = new Rectangle(x, y, 80, 20);
        var placed = SelectionActionLayout.Place(selection, viewport, new Size(86, 40), 6);
        Assert.True(viewport.Contains(placed));
        Assert.False(placed.IntersectsWith(selection));
    }

    [Fact]
    public void RealActionButtonsFollowKeyboardAndResizeAndConfirmValidatedFrame() => Sta(() =>
    {
        using var form = new TemplateCaptureForm(new Size(100, 80)) { Opacity = 0, ShowInTaskbar = false };
        form.Show();
        var confirm = Assert.IsType<Button>(form.Controls.Find("confirmSelectionButton", true).Single());
        var cancel = Assert.IsType<Button>(form.Controls.Find("cancelSelectionButton", true).Single());
        Assert.False(string.IsNullOrEmpty(confirm.AccessibleName));
        Assert.False(string.IsNullOrEmpty(cancel.AccessibleName));
        var toolbar = confirm.Parent!;
        var before = toolbar.Bounds;
        var initialSize = form.Frame.Size;
        Key(form, Keys.Shift | Keys.Right);
        Assert.Equal(before.X + 10, toolbar.Left);
        var corner = new Point(form.Frame.Right, form.Frame.Bottom);
        Mouse(form, "OnMouseDown", corner);
        Mouse(form, "OnMouseUp", new Point(corner.X - 20, corner.Y - 20));
        Assert.NotEqual(initialSize, form.Frame.Size);
        Assert.NotEqual(before.Location, toolbar.Location);
        var toolbarScreen = toolbar.Bounds;
        toolbarScreen.Offset(form.Left, form.Top);
        Assert.False(toolbarScreen.IntersectsWith(form.MoveHandle));
        Assert.False(toolbarScreen.IntersectsWith(Rectangle.Inflate(form.Frame, 9, 9)));
        var expected = form.Frame;
        confirm.PerformClick();
        Assert.Equal(DialogResult.OK, form.DialogResult);
        Assert.Equal(expected, form.SelectedArea);
    });

    [Fact]
    public void ButtonConfirmCannotBypassValidationAndCancelReturnsNoSelection() => Sta(() =>
    {
        using var form = new TemplateCaptureForm(new Size(2, 2)) { Opacity = 0, ShowInTaskbar = false };
        form.Show();
        var confirm = Assert.IsType<Button>(form.Controls.Find("confirmSelectionButton", true).Single());
        var cancel = Assert.IsType<Button>(form.Controls.Find("cancelSelectionButton", true).Single());
        typeof(TemplateCaptureForm).GetField("frame", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(form, new Rectangle(SystemInformation.VirtualScreen.Right + 10, 0, 2, 2));
        confirm.PerformClick();
        Assert.Equal(DialogResult.None, form.DialogResult);
        Assert.Equal(Rectangle.Empty, form.SelectedArea);
        cancel.PerformClick();
        Assert.Equal(DialogResult.Cancel, form.DialogResult);
        Assert.Equal(Rectangle.Empty, form.SelectedArea);
    });

    [Theory]
    [InlineData(16, 16)]
    [InlineData(2, 2)]
    public void TinyFramesRemainMovableByDedicatedHandleAndArrowKeys(int width, int height) => Sta(() =>
    {
        using var form = new TemplateCaptureForm(new Size(width, height));
        _ = form.Handle;
        var initial = form.Frame;
        var handle = form.MoveHandle;
        var point = new Point(handle.Left + handle.Width / 2, handle.Top + handle.Height / 2);
        Mouse(form, "OnMouseDown", point);
        Mouse(form, "OnMouseUp", new Point(point.X + 20, point.Y + 10));
        Assert.Equal(new Point(initial.X + 20, initial.Y + 10), form.Frame.Location);
        Assert.Equal(initial.Size, form.Frame.Size);
        Key(form, Keys.Left);
        Key(form, Keys.Shift | Keys.Up);
        Assert.Equal(new Point(initial.X + 19, initial.Y), form.Frame.Location);
        Assert.Equal(initial.Size, form.Frame.Size);
        Assert.Equal(DialogResult.None, form.DialogResult);
    });

    [Fact]
    public void GeometryMovesAcrossNegativeCoordinatesWithoutChangingSize()
    {
        var moved = TemplateCaptureForm.Transform(new Rectangle(100, 100, 200, 80), new Point(-1200, -300),
            TemplateCaptureForm.Edges.None, new Size(200, 100), new Rectangle(-1600, -600, 3520, 1680));
        Assert.Equal(new Rectangle(-1100, -200, 200, 80), moved);
    }

    [Fact]
    public void CornerResizesClampMaximumAndKeepOppositeCornerFixed()
    {
        var initial = new Rectangle(-500, -300, 100, 80);
        var enlarged = TemplateCaptureForm.Transform(initial, new Point(-1000, -1000),
            TemplateCaptureForm.Edges.Left | TemplateCaptureForm.Edges.Top, new Size(300, 200), new Rectangle(-2000, -1000, 4000, 2000));
        Assert.Equal(new Size(300, 200), enlarged.Size);
        Assert.Equal(initial.Right, enlarged.Right);
        Assert.Equal(initial.Bottom, enlarged.Bottom);
        var reduced = TemplateCaptureForm.Transform(initial, new Point(1000, 1000),
            TemplateCaptureForm.Edges.Left | TemplateCaptureForm.Edges.Top, new Size(300, 200), new Rectangle(-2000, -1000, 4000, 2000));
        Assert.Equal(new Size(2, 2), reduced.Size);
        Assert.Equal(initial.Right, reduced.Right);
        Assert.Equal(initial.Bottom, reduced.Bottom);
    }

    [Fact]
    public void FitSizeHonorsDecodedPixelBudget()
    {
        var size = TemplateCaptureForm.FitSize(new Size(10000, 10000), new Size(10000, 10000));
        Assert.True((long)size.Width * size.Height <= ClickCondition.MaximumTemplatePixels);
        Assert.InRange(size.Width, 2, 10000);
        Assert.InRange(size.Height, 2, 10000);
    }

    private static void Sta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void Mouse(TemplateCaptureForm form, string method, Point screen)
    {
        var local = new Point(screen.X - form.Left, screen.Y - form.Top);
        typeof(TemplateCaptureForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(form, [new MouseEventArgs(MouseButtons.Left, 1, local.X, local.Y, 0)]);
    }

    private static void Key(TemplateCaptureForm form, Keys key)
    {
        object[] args = [new Message(), key];
        typeof(TemplateCaptureForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, args);
    }

    [Fact]
    public void ActualMouseHandlersMoveResizeAndRequireExplicitConfirmation() => Sta(() =>
    {
        using var form = new TemplateCaptureForm(new Size(240, 160));
        _ = form.Handle;
        var initial = form.Frame;
        var middle = new Point(initial.Left + initial.Width / 2, initial.Top + initial.Height / 2);
        Mouse(form, "OnMouseDown", middle);
        Mouse(form, "OnMouseMove", new Point(middle.X + 20, middle.Y + 10));
        Mouse(form, "OnMouseUp", new Point(middle.X + 20, middle.Y + 10));
        Assert.Equal(new Point(initial.X + 20, initial.Y + 10), form.Frame.Location);
        Assert.Equal(DialogResult.None, form.DialogResult);
        var corner = new Point(form.Frame.Right, form.Frame.Bottom);
        Mouse(form, "OnMouseDown", corner);
        Mouse(form, "OnMouseUp", new Point(corner.X - 50, corner.Y - 30));
        Assert.Equal(initial.Width - 50, form.Frame.Width);
        Assert.Equal(initial.Height - 30, form.Frame.Height);
        var smallerCorner = new Point(form.Frame.Right, form.Frame.Bottom);
        Mouse(form, "OnMouseDown", smallerCorner);
        Mouse(form, "OnMouseUp", new Point(smallerCorner.X + 1000, smallerCorner.Y + 1000));
        Assert.Equal(new Size(240, 160), form.Frame.Size);
        var expected = form.Frame;
        Key(form, Keys.Enter);
        Assert.Equal(DialogResult.OK, form.DialogResult);
        Assert.Equal(expected, form.SelectedArea);
    });

    [Fact]
    public void EscapeCancelsWithoutReturningSelectedArea() => Sta(() =>
    {
        using var form = new TemplateCaptureForm(new Size(100, 80));
        Key(form, Keys.Escape);
        Assert.Equal(DialogResult.Cancel, form.DialogResult);
        Assert.Equal(Rectangle.Empty, form.SelectedArea);
    });

    [Fact]
    public void InvalidScreenPositionCannotBeConfirmed() => Sta(() =>
    {
        using var form = new TemplateCaptureForm(new Size(100, 80));
        typeof(TemplateCaptureForm).GetField("frame", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(form, new Rectangle(SystemInformation.VirtualScreen.Right + 50, 0, 100, 80));
        Assert.False(form.TryConfirm());
        Assert.Equal(DialogResult.None, form.DialogResult);
        Assert.Equal(Rectangle.Empty, form.SelectedArea);
    });
}
