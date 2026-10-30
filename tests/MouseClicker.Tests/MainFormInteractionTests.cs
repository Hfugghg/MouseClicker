using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace MouseClicker.Tests;

public class MainFormInteractionTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(Form1 form, string name) => (T)typeof(Form1).GetField(name, Private)!.GetValue(form)!;
    private static void Call(Form1 form, string name) => typeof(Form1).GetMethod(name, Private)!.Invoke(form, null);

    public static IEnumerable<object[]> StableInteractions()
    {
        foreach (string action in new[] { "timing", "overlay", "hardware", "sequence", "selection", "reorder", "remove", "list-refresh", "explicit-layout", "hotkey-return", "update-status" })
            foreach (bool scroll in new[] { false, true })
                // Refreshing all translated copy legitimately replaces the synthetic
                // overflow text, so that setup cannot represent unchanged content.
                if (action != "hotkey-return" || !scroll) yield return new object[] { action, scroll };
    }

    [Theory]
    [MemberData(nameof(StableInteractions))]
    public void InteractionKeepsUnchangedGeometryAndScrollWithoutWholeWindowRepaint(string action, bool scroll) => Sta(() =>
    {
        using var form = CreateShownForm(scroll);
        var viewport = Field<Panel>(form, "contentViewport");
        Point originalScroll = viewport.AutoScrollPosition;
        var bounds = Descendants(form).ToDictionary(c => c, c => c.Bounds);
        var geometryChanges = new List<string>();
        var broadInvalidations = new List<string>();
        foreach (Control control in Descendants(form))
        {
            control.LocationChanged += (_, _) => geometryChanges.Add($"{control.GetType().Name}:{control.Text}:Location");
            control.SizeChanged += (_, _) => geometryChanges.Add($"{control.GetType().Name}:{control.Text}:Size");
        }
        foreach (Control control in new Control[] { form, viewport, Field<Panel>(form, "areasCard"), Field<Panel>(form, "timingCard"), Field<Panel>(form, "displayCard") })
            control.Invalidated += (_, _) => broadInvalidations.Add(control == form ? "form" : control.Name + control.GetType().Name);

        var list = Field<ListBox>(form, "areasListBox");
        for (int i = 0; i < 5; i++)
        {
            switch (action)
            {
                case "timing":
                    foreach (string name in new[] { "intervalNumericUpDown", "jitterNumericUpDown" })
                    {
                        var input = Field<NumericUpDown>(form, name);
                        decimal before = input.Value;
                        input.UpButton();
                        Assert.Equal(before + input.Increment, input.Value);
                        input.DownButton();
                        Assert.Equal(before, input.Value);
                    }
                    break;
                case "overlay":
                    foreach (string name in new[] { "showOverlayCheckBox", "showDetectionOverlayCheckBox", "highlightCurrentAreaCheckBox" })
                    {
                        var check = Field<CheckBox>(form, name);
                        check.Checked = !check.Checked;
                        check.Checked = !check.Checked;
                    }
                    break;
                case "hardware":
                    Field<CheckBox>(form, "hardwareModeCheckBox").Checked = i % 2 == 0;
                    break;
                case "sequence": Field<ComboBox>(form, "sequenceComboBox").SelectedIndex = i % 3; break;
                case "selection": list.SelectedIndex = i % 2; break;
                case "reorder":
                    list.SelectedIndex = 0;
                    Field<Button>(form, "moveDownButton").PerformClick();
                    Assert.Equal(1, list.SelectedIndex);
                    Field<Button>(form, "moveUpButton").PerformClick();
                    Assert.Equal(0, list.SelectedIndex);
                    break;
                case "remove":
                    if (i == 0)
                    {
                        list.SelectedIndex = 0;
                        Field<Button>(form, "removeAreaButton").PerformClick();
                        Assert.Single(list.Items.Cast<object>());
                    }
                    break;
                case "list-refresh": Call(form, "RefreshAreaList"); break;
                case "explicit-layout": Call(form, "PerformLayoutLayout"); break;
                case "hotkey-return": Call(form, "UpdateAppearanceStrings"); Call(form, "PerformLayoutLayout"); break;
                case "update-status": Call(form, "RefreshUpdateState"); break;
                default: throw new InvalidOperationException(action);
            }
            Application.DoEvents();
            Assert.Equal(originalScroll, viewport.AutoScrollPosition);
        }
        Assert.True(geometryChanges.Count == 0, string.Join("\n", geometryChanges));
        Assert.True(broadInvalidations.Count == 0, string.Join("\n", broadInvalidations));
        foreach (var (control, expected) in bounds) Assert.Equal(expected, control.Bounds);
    });

    [Fact]
    public void UpdateBadgeAppearsAndDisappearsWithoutIntermediateGeometryRoundTrips() => Sta(() =>
    {
        using var form = CreateShownForm(false);
        var badge = Field<LinkLabel>(form, "updateAvailableLinkLabel");
        var viewport = Field<Panel>(form, "contentViewport");
        Rectangle before = viewport.Bounds;
        Size originalSize = form.ClientSize;
        FieldInfo update = typeof(Form1).GetField("_availableUpdate", Private)!;
        foreach (bool available in new[] { true, false })
        {
            var moves = Descendants(form).ToDictionary(c => c, _ => 0);
            var handlers = new Dictionary<Control, EventHandler>();
            foreach (Control control in moves.Keys)
            {
                EventHandler handler = (_, _) => moves[control]++;
                handlers.Add(control, handler);
                control.LocationChanged += handler;
            }
            update.SetValue(form, available ? new UpdateInfo(new Version(99, 0, 0), new Uri("https://example.com/release")) : null);
            Call(form, "RefreshUpdateState");
            Application.DoEvents();
            Assert.Equal(available, badge.Visible);
            Assert.All(moves, item => Assert.True(item.Value <= 1, $"{item.Key.GetType().Name} moved {item.Value} times"));
            foreach (var (control, handler) in handlers) control.LocationChanged -= handler;
        }
        Assert.Equal(before, viewport.Bounds);
        Assert.Equal(originalSize, form.ClientSize);
    });

    [Fact]
    public void ChangingSessionEnabledStateDoesNotRepositionControls() => Sta(() =>
    {
        using var form = CreateShownForm(false);
        int moves = 0;
        foreach (Control control in Descendants(form))
        {
            control.LocationChanged += (_, _) => moves++;
            control.SizeChanged += (_, _) => moves++;
        }
        MethodInfo setEnabled = typeof(Form1).GetMethod("SetUIEnabled", Private)!;
        setEnabled.Invoke(form, [false]);
        Assert.False(Field<NumericUpDown>(form, "intervalNumericUpDown").Enabled);
        setEnabled.Invoke(form, [true]);
        Assert.True(Field<NumericUpDown>(form, "intervalNumericUpDown").Enabled);
        Assert.Equal(0, moves);
    });

    [Fact]
    public void OverlayRefreshRepaintsOnlyWhenVisibleStateChanges() => Sta(() =>
    {
        using var form = CreateShownForm(false);
        var overlay = Field<OverlayForm>(form, "_overlayForm");
        var show = Field<CheckBox>(form, "showOverlayCheckBox");
        show.Checked = true;
        Application.DoEvents();
        int repaints = 0;
        overlay.Invalidated += (_, _) => repaints++;
        for (int i = 0; i < 5; i++) Call(form, "UpdateOverlayState");
        Assert.Equal(0, repaints);
        Field<CheckBox>(form, "showDetectionOverlayCheckBox").Checked = true;
        Assert.True(repaints > 0);
        repaints = 0;
        Field<ListBox>(form, "areasListBox").SelectedIndex = 0;
        Assert.True(repaints > 0);
        repaints = 0;
        Call(form, "UpdateOverlayState");
        Assert.Equal(0, repaints);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void JitterWarningTransitionMovesOnlyRequiredGeometryWithoutRoundTrips(bool scroll) => Sta(() =>
    {
        using var form = CreateShownForm(scroll);
        var viewport = Field<Panel>(form, "contentViewport");
        var areas = Field<Panel>(form, "areasCard");
        Point originalScroll = viewport.AutoScrollPosition;
        var unrelatedChanges = new List<Control>();
        foreach (Control control in Descendants(areas).Prepend(areas))
        {
            control.LocationChanged += (_, _) => unrelatedChanges.Add(control);
            control.SizeChanged += (_, _) => unrelatedChanges.Add(control);
        }
        var trajectories = Descendants(form).ToDictionary(c => c, c => new List<Point> { c.Location });
        foreach (Control control in trajectories.Keys)
            control.LocationChanged += (_, _) => trajectories[control].Add(control.Location);
        var interval = Field<NumericUpDown>(form, "intervalNumericUpDown");
        var jitter = Field<NumericUpDown>(form, "jitterNumericUpDown");
        jitter.Value = interval.Value;
        jitter.UpButton();
        Application.DoEvents();
        Assert.True(Field<Label>(form, "jitterExceedsIntervalHintLabel").Visible);
        Assert.Equal(originalScroll, viewport.AutoScrollPosition);
        Assert.Empty(unrelatedChanges);
        foreach (var (control, points) in trajectories)
            Assert.True(points.Count <= 2, $"{control.GetType().Name}:{control.Text} moved {points.Count - 1} times");
        jitter.DownButton();
        Assert.False(Field<Label>(form, "jitterExceedsIntervalHintLabel").Visible);
        Assert.Equal(originalScroll, viewport.AutoScrollPosition);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShrinkingContentAtBottomClampsScrollAndKeepsCardLogicalOrigin(bool keepOverflow) => Sta(() =>
    {
        using var form = CreateShownForm(true);
        var viewport = Field<Panel>(form, "contentViewport");
        var jitter = Field<NumericUpDown>(form, "jitterNumericUpDown");
        var interval = Field<NumericUpDown>(form, "intervalNumericUpDown");
        if (keepOverflow) jitter.Value = interval.Value + 1;
        Application.DoEvents();
        viewport.AutoScrollPosition = new Point(0, int.MaxValue / 2);
        Application.DoEvents();
        int originalOffset = -viewport.AutoScrollPosition.Y;
        Assert.True(originalOffset > 0);
        if (keepOverflow)
            jitter.Value = interval.Value;
        else
        {
            Field<Label>(form, "areasHint").Text = Localization.Get("MainAreasHint");
            Call(form, "PerformLayoutLayout");
        }
        Application.DoEvents();
        int maximumOffset = Math.Max(0, viewport.AutoScrollMinSize.Height - viewport.ClientSize.Height);
        Assert.Equal(Math.Min(originalOffset, maximumOffset), -viewport.AutoScrollPosition.Y);
        Assert.Equal(0, Field<Panel>(form, "areasCard").Top - viewport.AutoScrollPosition.Y);
        Assert.Equal(keepOverflow, viewport.VerticalScroll.Visible);
        var start = Field<Button>(form, "startButton");
        Assert.True(start.Bottom <= form.ClientSize.Height);
        Assert.True(start.Top >= viewport.Bottom);
        // A second layout must retain the same logical coordinates and scroll offset.
        var bounds = Descendants(form).ToDictionary(c => c, c => c.Bounds);
        Point scroll = viewport.AutoScrollPosition;
        Call(form, "PerformLayoutLayout");
        Assert.Equal(scroll, viewport.AutoScrollPosition);
        foreach (var (control, expected) in bounds) Assert.Equal(expected, control.Bounds);
    });

    private static Form1 CreateShownForm(bool scroll)
    {
        var form = new Form1(null, null) { Opacity = 0, ShowInTaskbar = false };
        typeof(Form1).GetField("_updateCheckStarted", Private)!.SetValue(form, true);
        typeof(Form1).GetField("_hotkeyFailureShown", Private)!.SetValue(form, true);
        Field<CheckBox>(form, "showOverlayCheckBox").Checked = false;
        var areas = Field<List<ClickArea>>(form, "_clickAreas");
        areas.Add(new() { Label = "A", Area = new Rectangle(10, 10, 8, 8) });
        areas.Add(new() { Label = "B", Area = new Rectangle(30, 30, 8, 8) });
        Call(form, "RefreshAreaList");
        form.Show();
        if (scroll)
        {
            Field<Label>(form, "areasHint").Text = string.Join(" ", Enumerable.Repeat("Context requiring scrolling.", 300));
            Call(form, "PerformLayoutLayout");
        }
        Application.DoEvents();
        if (scroll)
        {
            var viewport = Field<Panel>(form, "contentViewport");
            Assert.True(viewport.VerticalScroll.Visible);
            viewport.AutoScrollPosition = new Point(0, 80);
            Assert.True(viewport.AutoScrollPosition.Y < 0);
        }
        return form;
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Sta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI interaction test timed out.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
