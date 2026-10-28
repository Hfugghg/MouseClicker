using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using Xunit;

namespace MouseClicker.Tests;

public class ConditionSettingsFormTests
{
    [Fact]
    public void CaptureTemporarilyClearsOwnedGuideAndRestoresItsOpacity() => OnSta(() =>
    {
        using var owner = new Form { Opacity = .7 };
        using var guide = new Form { Opacity = .8, Owner = owner };
        using var form = new ConditionSettingsForm(null) { Opacity = .9 };
        using var watchdog = new System.Windows.Forms.Timer { Interval = 3000 };
        bool completed = false;
        Exception? error = null;
        watchdog.Tick += (_, _) => form.Close();
        form.Shown += async (_, _) =>
        {
            try
            {
                await form.WithTransparentWindowsAsync(async () =>
                {
                    await Task.Delay(30);
                    Assert.Equal(0, owner.Opacity);
                    Assert.Equal(0, guide.Opacity);
                    Assert.Equal(0, form.Opacity);
                    Assert.True(form.Visible);
                });
                Assert.Equal(.7, owner.Opacity);
                Assert.Equal(.8, guide.Opacity);
                Assert.Equal(.9, form.Opacity);
                Assert.False(guide.IsDisposed);
                completed = true;
            }
            catch (Exception ex) { error = ex; }
            finally { form.Close(); }
        };
        watchdog.Start();
        form.ShowDialog(owner);
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        Assert.True(completed);
    });

    [Fact]
    public void TransparentCaptureKeepsRealModalLoopAliveAcrossAwait() => OnSta(() =>
    {
        using var owner = new Form { Opacity = .8 };
        using var form = new ConditionSettingsForm(null);
        using var watchdog = new System.Windows.Forms.Timer { Interval = 3000 };
        bool completed = false;
        bool returned = false;
        Exception? error = null;
        watchdog.Tick += (_, _) => form.Close();
        form.Shown += async (_, _) =>
        {
            try
            {
                await form.WithTransparentWindowsAsync(async () =>
                {
                    await Task.Delay(80);
                    Assert.False(returned);
                    Assert.True(form.Visible);
                    Assert.True(form.Modal);
                    Assert.Equal(0, form.Opacity);
                    Assert.Equal(0, owner.Opacity);
                });
                Assert.Equal(1, form.Opacity);
                Assert.Equal(.8, owner.Opacity);
                completed = true;
            }
            catch (Exception ex) { error = ex; }
            finally { form.Close(); }
        };
        watchdog.Start();
        form.ShowDialog(owner);
        returned = true;
        Assert.True(completed);
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    });

    [Fact]
    public void ClosingDuringCaptureDoesNotReshowDialog() => OnSta(() =>
    {
        using var owner = new Form { Opacity = .8 };
        using var form = new ConditionSettingsForm(null);
        using var watchdog = new System.Windows.Forms.Timer { Interval = 3000 };
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? capture = null;
        bool entered = false;
        watchdog.Tick += (_, _) => form.Close();
        form.Shown += (_, _) =>
        {
            capture = form.WithTransparentWindowsAsync(async () =>
            {
                entered = true;
                form.Close();
                await release.Task;
            });
        };
        watchdog.Start();
        form.ShowDialog(owner);
        release.TrySetResult();
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (capture is { IsCompleted: false } && DateTime.UtcNow < deadline) Application.DoEvents();
        Assert.True(entered);
        Assert.NotNull(capture);
        Assert.True(capture.IsCompleted);
        capture.GetAwaiter().GetResult();
        Assert.False(form.Visible);
        Assert.Equal(.8, owner.Opacity);
    });

    [Fact]
    public void EditingUsesCopyAndCancelLeavesOriginalUnchanged() => OnSta(() =>
    {
        var original = MakeCondition();
        using var form = new ConditionSettingsForm(original);
        var threshold = (NumericUpDown)typeof(ConditionSettingsForm).GetField("threshold", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        threshold.Value = .75m;
        var changed = form.BuildCondition();
        Assert.NotSame(original, changed);
        Assert.Equal(.75, changed.Threshold);
        Assert.Equal(.9, original.Threshold);
        Assert.Equal(original.TemplateImageBase64, changed.TemplateImageBase64);
        Assert.Equal(original.DetectionArea, changed.DetectionArea);
        Assert.Null(form.ResultCondition);
    });

    [Fact]
    public void EmptyEnabledConditionCannotBeSaved() => OnSta(() =>
    {
        using var form = new ConditionSettingsForm(null);
        Assert.Throws<InvalidDataException>(() => form.BuildCondition());
    });

    [Fact]
    public void ExistingConditionPreservesIndependentDetectionRectangleAndModes() => OnSta(() =>
    {
        var original = MakeCondition();
        original.TriggerMode = ConditionTriggerMode.WhileVisible;
        original.TimeoutMs = 0;
        using var form = new ConditionSettingsForm(original);
        var loaded = form.BuildCondition();
        Assert.Equal(original.DetectionArea, loaded.DetectionArea);
        Assert.Equal(ConditionTriggerMode.WhileVisible, loaded.TriggerMode);
        Assert.Equal(0, loaded.TimeoutMs);
    });

    private static ClickCondition MakeCondition()
    {
        using var bitmap = new Bitmap(4, 4);
        bitmap.SetPixel(1, 1, Color.Red);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return new ClickCondition { DetectionArea = new Rectangle(-120, 30, 100, 80), TemplateImageBase64 = Convert.ToBase64String(stream.ToArray()) };
    }

    private static void OnSta(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { test(); } catch (Exception ex) { failure = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
