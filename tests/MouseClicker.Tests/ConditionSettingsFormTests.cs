using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using Xunit;

namespace MouseClicker.Tests;

public class ConditionSettingsFormTests
{
    [Theory]
    [InlineData("match")]
    [InlineData("miss")]
    [InlineData("error")]
    public void RecognitionTestCompletesWithVisibleExplicitOutcome(string scenario) => OnSta(() =>
    {
        var condition = MakeCondition();
        using var form = new ConditionSettingsForm(condition, null, _ =>
        {
            if (scenario == "error") throw new IOException("capture failed");
            if (scenario == "miss") return new Bitmap(100, 80);
            using var stream = new MemoryStream(Convert.FromBase64String(condition.TemplateImageBase64));
            return new Bitmap(stream);
        });
        Exception? error = null;
        bool completed = false;
        using var watchdog = new System.Windows.Forms.Timer { Interval = 5000 };
        watchdog.Tick += (_, _) => form.Close();
        form.Shown += async (_, _) =>
        {
            try
            {
                await (Task)typeof(ConditionSettingsForm).GetMethod("TestAsync", Private)!.Invoke(form, null)!;
                var result = Field<Label>(form, "result");
                // 这里的断言不依赖界面语言：其他测试可能在窗体运行期间
                // 修改全局界面语言。
                if (scenario == "error")
                {
                    Assert.EndsWith("capture failed", result.Text);
                    Assert.Equal(Color.Firebrick, result.ForeColor);
                }
                else
                {
                    Assert.Contains(condition.Threshold.ToString("F3"), result.Text);
                    Assert.Equal(scenario == "match" ? Color.DarkGreen : Color.Firebrick, result.ForeColor);
                    Assert.DoesNotContain(Localization.Get("ConditionWaiting"), result.Text);
                }
                Assert.True(result.Visible);
                Assert.True(form.RectangleToScreen(form.ClientRectangle).Contains(result.PointToScreen(Point.Empty)));
                Assert.NotSame(Field<Button>(form, "test").Parent, result.Parent);
                Assert.True(Field<Button>(form, "test").Enabled);
                completed = true;
            }
            catch (Exception ex) { error = ex; }
            finally { form.Close(); }
        };
        watchdog.Start();
        form.ShowDialog();
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        Assert.True(completed);
    });

    [Fact]
    public void ImportSourceAndTemplateBudgetsAllowFullResolutionImages()
    {
        ConditionSettingsForm.ValidateImportSource(20L * 1024 * 1024, new Size(3840, 2160));
        Assert.Throws<InvalidDataException>(() => ConditionSettingsForm.ValidateImportSource(ConditionSettingsForm.MaximumImportBytes + 1));
        Assert.Throws<InvalidDataException>(() => ConditionSettingsForm.ValidateImportSource(0, new Size(8192, 8192)));
    }

    [Fact]
    public void FullFourKTemplateCanBeKeptWithoutCroppingOrResizing() => OnSta(() =>
    {
        using var form = new ConditionSettingsForm(MakeCondition());
        using var source = new Bitmap(3840, 2160);
        source.SetPixel(3800, 2100, Color.Blue);
        form.SetCroppedTemplate(source, new Rectangle(3800, 2100, 20, 20));
        Field<CheckBox>(form, "useMultiScale").Checked = true;
        typeof(ConditionSettingsForm).GetMethod("SetTemplate", Private)!.Invoke(form, [source]);
        Assert.Equal(source.Size, Field<Bitmap>(form, "templateBitmap").Size);
        Assert.Equal(Color.Blue.ToArgb(), Field<Bitmap>(form, "templateBitmap").GetPixel(3800, 2100).ToArgb());
        Assert.True(form.BuildCondition().UseMultiScale);
        Assert.True(Field<Button>(form, "test").Enabled);
    });

    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(ConditionSettingsForm form, string name) => (T)typeof(ConditionSettingsForm).GetField(name, Private)!.GetValue(form)!;
    private static void SetRegion(ConditionSettingsForm form, Rectangle area)
    {
        typeof(ConditionSettingsForm).GetField("detectionArea", Private)!.SetValue(form, area);
        typeof(ConditionSettingsForm).GetMethod("UpdateRegion", Private)!.Invoke(form, null);
    }

    [Fact]
    public void CaptureAndImportRequireDetectionAreaButTemplatePositionRemainsIndependent() => OnSta(() =>
    {
        using var form = new ConditionSettingsForm(null);
        Assert.False(Field<Button>(form, "capture").Enabled);
        Assert.False(Field<Button>(form, "import").Enabled);
        Assert.True(Field<Button>(form, "save").Enabled);
        SetRegion(form, new Rectangle(300, 50, 1, 20));
        Assert.False(Field<Button>(form, "capture").Enabled);
        SetRegion(form, new Rectangle(-900, -300, 30, 20));
        Assert.True(Field<Button>(form, "capture").Enabled);
        Assert.True(Field<Button>(form, "import").Enabled);
        using var image = new Bitmap(30, 20);
        typeof(ConditionSettingsForm).GetMethod("SetTemplate", Private)!.Invoke(form, [image]);
        Assert.True(Field<Button>(form, "test").Enabled);
        Assert.True(Field<Button>(form, "save").Enabled);
        Assert.Equal(new Rectangle(-900, -300, 30, 20), form.BuildCondition().DetectionArea);
    });

    [Fact]
    public void ImportCropUsesOriginalPixelsAndRejectsInvalidCropWithoutChangingTemplate() => OnSta(() =>
    {
        using var form = new ConditionSettingsForm(MakeCondition());
        SetRegion(form, new Rectangle(-500, 20, 3, 3));
        using var source = new Bitmap(8, 8);
        source.SetPixel(5, 4, Color.CornflowerBlue);
        source.SetPixel(6, 5, Color.Red);
        form.SetCroppedTemplate(source, new Rectangle(5, 4, 2, 2));
        var condition = form.BuildCondition();
        using var stream = new MemoryStream(Convert.FromBase64String(condition.TemplateImageBase64));
        using var image = new Bitmap(stream);
        Assert.Equal(new Size(2, 2), image.Size);
        Assert.Equal(Color.CornflowerBlue.ToArgb(), image.GetPixel(0, 0).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), image.GetPixel(1, 1).ToArgb());
        Assert.Throws<InvalidDataException>(() => form.SetCroppedTemplate(source, new Rectangle(7, 7, 2, 2)));
        Assert.Equal(condition.TemplateImageBase64, form.BuildCondition().TemplateImageBase64);
    });

    [Fact]
    public void NewEditorEnablesScaleWhileExistingConditionsKeepOriginalMode() => OnSta(() =>
    {
        using var fresh = new ConditionSettingsForm(null);
        Assert.True(Field<CheckBox>(fresh, "useMultiScale").Checked);
        using var old = new ConditionSettingsForm(MakeCondition());
        Assert.False(Field<CheckBox>(old, "useMultiScale").Checked);
        SetRegion(old, new Rectangle(-400, 50, 2, 2));
        Assert.False(Field<Button>(old, "save").Enabled);
        Field<CheckBox>(old, "useMultiScale").Checked = true;
        Assert.True(Field<Button>(old, "save").Enabled);
        Assert.True(old.BuildCondition().UseMultiScale);
        Field<CheckBox>(old, "useMultiScale").Checked = false;
        Assert.False(Field<Button>(old, "test").Enabled);
        Assert.Throws<InvalidDataException>(() => old.BuildCondition());
    });

    [Fact]
    public void OptionalCropUsesRealTemplateAndCancelLeavesOriginalUnchanged() => OnSta(() =>
    {
        using var form = new ConditionSettingsForm(MakeCondition());
        var original = form.BuildCondition().TemplateImageBase64;
        typeof(ConditionSettingsForm).GetMethod("ReplacePreview", Private)!.Invoke(form, [new Bitmap(100, 80)]);
        bool inspected = false;
        using var cancel = new System.Windows.Forms.Timer { Interval = 50 };
        cancel.Tick += (_, _) =>
        {
            var crop = Application.OpenForms.Cast<Form>().OfType<TemplateCropForm>().FirstOrDefault();
            if (crop == null) return;
            inspected = crop.CropArea.Size == new Size(4, 4);
            crop.DialogResult = DialogResult.Cancel;
            crop.Close();
        };
        cancel.Start();
        typeof(ConditionSettingsForm).GetMethod("CropTemplate", Private)!.Invoke(form, null);
        Assert.True(inspected);
        Assert.Equal(original, form.BuildCondition().TemplateImageBase64);
        Assert.Equal(new Size(4, 4), Field<Bitmap>(form, "templateBitmap").Size);
    });

    [Fact]
    public void BusyEditorDisablesTemplateAndScaleOperations() => OnSta(() =>
    {
        using var form = new ConditionSettingsForm(MakeCondition());
        typeof(ConditionSettingsForm).GetField("busy", Private)!.SetValue(form, true);
        typeof(ConditionSettingsForm).GetMethod("UpdateSetupState", Private)!.Invoke(form, null);
        foreach (var name in new[] { "capture", "import", "cropTemplate", "test", "save", "useMultiScale" })
            Assert.False(Field<Control>(form, name).Enabled);
    });

    [Fact]
    public void OversizeTemplateAndSmallerRegionAreImmediatelyRecoverable() => OnSta(() =>
    {
        using var form = new ConditionSettingsForm(MakeCondition());
        SetRegion(form, new Rectangle(1200, 400, 3, 4));
        Assert.False(Field<Button>(form, "test").Enabled);
        Assert.False(Field<Button>(form, "save").Enabled);
        Assert.Contains("4", Field<Label>(form, "setupHint").Text);
        Assert.Throws<InvalidDataException>(() => form.BuildCondition());
        Field<CheckBox>(form, "enabled").Checked = false;
        Assert.True(Field<Button>(form, "save").Enabled);
        using var smaller = new Bitmap(2, 2);
        typeof(ConditionSettingsForm).GetMethod("SetTemplate", Private)!.Invoke(form, [smaller]);
        Assert.True(Field<Button>(form, "test").Enabled);
        Assert.True(Field<Button>(form, "save").Enabled);
        using var larger = new Bitmap(8, 6);
        typeof(ConditionSettingsForm).GetMethod("SetTemplate", Private)!.Invoke(form, [larger]);
        Assert.False(Field<Button>(form, "test").Enabled);
        SetRegion(form, new Rectangle(-800, 70, 10, 10));
        Assert.True(Field<Button>(form, "save").Enabled);
        Assert.Equal(new Size(8, 6), Field<Size>(form, "templateSize"));
        // 识别预览可能使用检测区域的尺寸，但不能因此改写模板尺寸。
        typeof(ConditionSettingsForm).GetMethod("ReplacePreview", Private)!.Invoke(form, [new Bitmap(100, 80)]);
        SetRegion(form, new Rectangle(-800, 70, 10, 10));
        Assert.True(Field<Button>(form, "test").Enabled);
        Assert.Equal(new Size(8, 6), Field<Size>(form, "templateSize"));
    });

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
        using var watchdog = new System.Windows.Forms.Timer { Interval = 10000 };
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
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        Assert.True(completed, "The modal loop exited before the async handler completed.");
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

    [Fact]
    public void SaveCommitsValidConditionAndClosesWithOk() => OnSta(() =>
    {
        var original = MakeCondition();
        using var form = new ConditionSettingsForm(original);
        typeof(ConditionSettingsForm).GetMethod("Save", Private)!.Invoke(form, null);
        Assert.NotNull(form.ResultCondition);
        Assert.Equal(original.DetectionArea, form.ResultCondition!.DetectionArea);
        Assert.Equal(original.TemplateImageBase64, form.ResultCondition.TemplateImageBase64);
        Assert.Equal(DialogResult.OK, form.DialogResult);
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
