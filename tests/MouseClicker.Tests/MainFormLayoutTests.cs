using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;
using Xunit.Abstractions;

namespace MouseClicker.Tests;

public class MainFormLayoutTests(ITestOutputHelper output)
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(Form1 form, string name) => (T)typeof(Form1).GetField(name, Private)!.GetValue(form)!;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedWindowMovesKeepWindowAndControlsTheSameSize(bool overflow) => OnSta(() =>
    {
        using var form = new Form1(null, null);
        if (overflow)
            Field<Label>(form, "areasHint").Text = string.Join(" ", Enumerable.Repeat("Context requiring scrolling.", 300));
        if (overflow) typeof(Form1).GetMethod("PerformLayoutLayout", Private)!.Invoke(form, null);
        Size expectedWindow = form.ClientSize;
        Button edit = Field<Button>(form, "editConditionButton");
        Rectangle expectedButton = edit.Bounds;
        Rectangle expectedStart = Field<Button>(form, "startButton").Bounds;
        var expectedBounds = Descendants(form).ToDictionary(control => control, control => control.Bounds);
        for (int i = 0; i < 25; i++)
        {
            typeof(Form).GetMethod("OnResizeEnd", Private)!.Invoke(form, [EventArgs.Empty]);

            output.WriteLine($"{i}: client={form.ClientSize}; edit={edit.Size}; preferred={edit.PreferredSize}");
            Assert.Equal(expectedWindow, form.ClientSize);
            foreach (var (control, bounds) in expectedBounds) Assert.Equal(bounds, control.Bounds);
        }
        Assert.Equal(expectedButton, edit.Bounds);
        Assert.Equal(expectedStart, Field<Button>(form, "startButton").Bounds);
        Panel viewport = Field<Panel>(form, "contentViewport");
        if (overflow) Assert.True(viewport.AutoScrollMinSize.Height > viewport.Height);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedTimingSpinnerClicksKeepWindowAndControlsTheSameSize(bool overflow) => OnSta(() =>
    {
        using var form = new Form1(null, null);
        if (overflow)
        {
            Field<Label>(form, "areasHint").Text = string.Join(" ", Enumerable.Repeat("Context requiring scrolling.", 300));
            typeof(Form1).GetMethod("PerformLayoutLayout", Private)!.Invoke(form, null);
        }
        Size expectedWindow = form.ClientSize;
        var expectedBounds = Descendants(form).ToDictionary(control => control, control => control.Bounds);
        var interval = Field<NumericUpDown>(form, "intervalNumericUpDown");
        var jitter = Field<NumericUpDown>(form, "jitterNumericUpDown");
        int valueChanges = 0;
        interval.ValueChanged += (_, _) => valueChanges++;
        jitter.ValueChanged += (_, _) => valueChanges++;
        for (int i = 0; i < 25; i++)
        {
            foreach (var spinner in new[] { interval, jitter })
            {
                decimal before = spinner.Value;
                spinner.UpButton();
                Assert.Equal(before + spinner.Increment, spinner.Value);
                AssertStableBounds();
                spinner.DownButton();
                Assert.Equal(before, spinner.Value);
                AssertStableBounds();
            }
        }
        Assert.Equal(100, valueChanges);
        if (overflow)
        {
            Panel viewport = Field<Panel>(form, "contentViewport");
            Assert.True(viewport.AutoScrollMinSize.Height > viewport.Height);
        }

        void AssertStableBounds()
        {
            Assert.Equal(expectedWindow, form.ClientSize);
            foreach (var (control, bounds) in expectedBounds) Assert.Equal(bounds, control.Bounds);
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovingWithinTheSameDisplaySkipsLayoutAndPreservesScroll(bool overflow) => OnSta(() =>
    {
        using var form = new Form1(null, null);
        // AutoScroll 需要可见的原生窗口。让窗口保持透明，并抑制
        // 启动时的网络检查和弹窗，使测试只关注布局与滚动。
        typeof(Form1).GetField("_updateCheckStarted", Private)!.SetValue(form, true);
        typeof(Form1).GetField("_hotkeyFailureShown", Private)!.SetValue(form, true);
        form.Opacity = 0;
        form.ShowInTaskbar = false;
        form.Show();
        Application.DoEvents();
        Panel viewport = Field<Panel>(form, "contentViewport");
        if (overflow)
        {
            Field<Label>(form, "areasHint").Text = string.Join(" ", Enumerable.Repeat("Context requiring scrolling.", 300));
            typeof(Form1).GetMethod("PerformLayoutLayout", Private)!.Invoke(form, null);
            viewport.PerformLayout();
            Application.DoEvents();
            Assert.True(viewport.VerticalScroll.Visible);
            viewport.AutoScrollPosition = new Point(0, 80);
            Assert.True(viewport.AutoScrollPosition.Y < 0);
        }
        Point expectedScroll = viewport.AutoScrollPosition;
        int layouts = 0, locations = 0;
        foreach (Control control in Descendants(form).Prepend(form))
        {
            control.Layout += (_, _) => layouts++;
            if (control != form) control.LocationChanged += (_, _) => locations++;
        }
        for (int i = 0; i < 25; i++)
        {
            form.Left += i % 2 == 0 ? 1 : -1;
            typeof(Form).GetMethod("OnResizeEnd", Private)!.Invoke(form, [EventArgs.Empty]);
            Assert.Equal(expectedScroll, viewport.AutoScrollPosition);
        }
        Assert.Equal(0, layouts);
        Assert.Equal(0, locations);
    });

    [Theory]
    [InlineData("layoutWorkingAreaSize")]
    [InlineData("layoutDpi")]
    public void ChangedDisplayLayoutKeyTriggersOneRecalculation(string cachedField) => OnSta(() =>
    {
        using var form = new Form1(null, null);
        FieldInfo cache = typeof(Form1).GetField(cachedField, Private)!;
        object expected = cache.GetValue(form)!;
        cache.SetValue(form, cachedField == "layoutDpi" ? (object)0 : Size.Empty);
        int layouts = 0;
        Field<Panel>(form, "areasCard").Layout += (_, _) => layouts++;
        typeof(Form).GetMethod("OnResizeEnd", Private)!.Invoke(form, [EventArgs.Empty]);
        // 即使重新计算得到相同的最终几何，也必须消费环境键变化。
        // 它不必强制触发原生布局或重绘。
        Assert.Equal(expected, cache.GetValue(form));
        layouts = 0;
        typeof(Form).GetMethod("OnResizeEnd", Private)!.Invoke(form, [EventArgs.Empty]);
        Assert.Equal(0, layouts);
    });

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void OnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI layout test did not finish.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
