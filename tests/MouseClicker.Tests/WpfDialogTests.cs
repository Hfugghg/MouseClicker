using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using MouseClicker.Wpf;
using Xunit;
using CheckBox = System.Windows.Controls.CheckBox;
using TextBox = System.Windows.Controls.TextBox;

namespace MouseClicker.Tests;

public class WpfDialogTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target)!;

    [Theory]
    [InlineData(Key.F6, ModifierKeys.None, true)]
    [InlineData(Key.F12, ModifierKeys.None, true)]
    [InlineData(Key.Pause, ModifierKeys.None, true)]
    [InlineData(Key.Escape, ModifierKeys.None, false)]
    [InlineData(Key.A, ModifierKeys.None, false)]
    [InlineData(Key.F6, ModifierKeys.Control, false)]
    [InlineData(Key.F6, ModifierKeys.Alt, false)]
    [InlineData(Key.F6, ModifierKeys.Shift, false)]
    [InlineData(Key.F6, ModifierKeys.Windows, false)]
    public void HotkeyCaptureRejectsUnsupportedKeysAndModifiers(Key key, ModifierKeys modifiers, bool accepted)
    {
        Assert.Equal(accepted, HotkeySettingsWindow.TryGetCapturedKey(key, modifiers, out var captured));
        if (accepted) Assert.Equal(KeyInterop.VirtualKeyFromKey(key), (int)captured);
    }

    [Fact]
    public void HelpSearchIncludesArticleBodyAndPreservesTopicAcrossLanguages() => WpfTestHost.Run(() =>
    {
        var language = Localization.Language;
        try
        {
            Localization.Language = "en-US";
            var window = new HelpWindow(HelpTopic.ImageConditionSetup);
            try
            {
                Assert.Equal(HelpTopic.ImageConditionSetup, window.SelectedTopic);
                window.SetSearch("32 MB");
                Assert.True(window.VisibleTopicCount > 0);
                Assert.Contains("32 MB", window.ArticleText);
                window.SetSearch("does-not-exist-8ad6d2");
                Assert.Equal(0, window.VisibleTopicCount);
                Assert.Equal(HelpContent.GetCatalog("en-US").SearchNoResults, window.ArticleText);
                window.ShowTopic(HelpTopic.HotkeysAndConfiguration);
                window.ReloadCatalog(HelpContent.GetCatalog("zh-CN"));
                Assert.Equal(HelpTopic.HotkeysAndConfiguration, window.SelectedTopic);
                Assert.Contains("自动保存", window.ArticleText);
            }
            finally { window.Close(); }
        }
        finally { Localization.Language = language; }
    });

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void ConditionDefaultsParseInCurrentCulture(string culture) => WpfTestHost.Run(() =>
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        var window = new ConditionSettingsWindow(null);
        try
        {
            typeof(ConditionSettingsWindow).GetField("detectionArea", Private)!.SetValue(window, new Rectangle(-100, 20, 100, 80));
            using var source = MakeTemplate();
            window.SetCroppedTemplate(source, new Rectangle(0, 0, 12, 10));
            var condition = window.BuildCondition();
            Assert.Equal(0.9, condition.Threshold, 5);
            Assert.Equal(200, condition.PollIntervalMs);
            Assert.Equal(60000, condition.TimeoutMs);
            Assert.True(condition.UseMultiScale);
            Assert.Equal(-100, condition.DetectionArea.X);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void ConditionCloneAndCropDoNotMutateOriginalAndRejectInvalidInputs() => WpfTestHost.Run(() =>
    {
        var original = MakeCondition();
        string bytes = original.TemplateImageBase64;
        var window = new ConditionSettingsWindow(original);
        try
        {
            using var source = MakeTemplate();
            Assert.Throws<InvalidDataException>(() => window.SetCroppedTemplate(source, new Rectangle(-1, 0, 10, 10)));
            Assert.Throws<InvalidDataException>(() => window.SetCroppedTemplate(source, new Rectangle(0, 0, 1, 10)));
            window.SetCroppedTemplate(source, new Rectangle(2, 2, 8, 6));
            var edited = window.BuildCondition();
            Assert.Equal(bytes, original.TemplateImageBase64);
            using var stream = new MemoryStream(edited.GetTemplateBytes());
            using var bitmap = new Bitmap(stream);
            Assert.Equal(new Size(8, 6), bitmap.Size);
            Field<TextBox>(window, "threshold").Text = "2";
            Assert.Throws<InvalidDataException>(() => window.BuildCondition());
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData("match")]
    [InlineData("miss")]
    [InlineData("error")]
    public void RecognitionTestReportsOutcomeAndRestoresWindowAfterCapture(string scenario) => WpfTestHost.RunAsync(async () =>
    {
        var condition = MakeCondition();
        var window = new ConditionSettingsWindow(condition, captureScreen: _ =>
        {
            if (scenario == "error") throw new IOException("capture failed");
            if (scenario == "miss") return new Bitmap(100, 80);
            using var stream = new MemoryStream(condition.GetTemplateBytes());
            return new Bitmap(stream);
        });
        try
        {
            window.Show();
            window.Opacity = 0.8;
            await (Task)typeof(ConditionSettingsWindow).GetMethod("TestAsync", Private)!.Invoke(window, null)!;
            Assert.Equal(0.8, window.Opacity, 5);
            string result = Field<TextBlock>(window, "result").Text;
            if (scenario == "error") Assert.Contains("capture failed", result);
            else
            {
                Assert.Contains(condition.Threshold.ToString("F3"), result);
                string prefix = Localization.Get(scenario == "match" ? "ConditionTestMatched" : "ConditionTestNotMatched").Split("{0")[0];
                Assert.StartsWith(prefix, result);
            }
            Assert.True(Field<System.Windows.Controls.Button>(window, "test").IsEnabled);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void TransparentOperationRestoresOpacityWhenOperationThrows() => WpfTestHost.RunAsync(async () =>
    {
        var window = new ConditionSettingsWindow(null);
        try
        {
            window.Show(); window.Opacity = 0.7;
            await Assert.ThrowsAsync<IOException>(() => window.WithTransparentWindowsAsync(() => throw new IOException("expected")));
            Assert.Equal(0.7, window.Opacity, 5);
        }
        finally { window.Close(); }
    });

    private static Bitmap MakeTemplate()
    {
        var bitmap = new Bitmap(20, 16);
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                bitmap.SetPixel(x, y, Color.FromArgb((x * 37 + y * 11) % 256, (y * 51) % 256, (x * 23) % 256));
        return bitmap;
    }

    private static ClickCondition MakeCondition()
    {
        using var bitmap = MakeTemplate();
        using var stream = new MemoryStream(); bitmap.Save(stream, ImageFormat.Png);
        return new ClickCondition { DetectionArea = new Rectangle(-100, 0, 100, 80),
            TemplateImageBase64 = Convert.ToBase64String(stream.ToArray()), Threshold = 0.9, PollIntervalMs = 200, TimeoutMs = 60000 };
    }
}
