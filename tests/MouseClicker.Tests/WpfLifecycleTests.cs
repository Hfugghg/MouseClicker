using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MouseClicker.Wpf;
using Xunit;

namespace MouseClicker.Tests;

public class WpfLifecycleTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target)!;

    [Fact]
    public void AboutReceivesUpdateThenUnsubscribesWhenClosed() => WpfTestHost.RunAsync(async () =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "MouseClicker-Wpf-Lifecycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var handler = new PendingReleaseHandler();
        using var http = new HttpClient(handler);
        var main = new MainWindow(null, http, new UpdatePreferencesStore(Path.Combine(directory, "preferences.json")), enableHotkeys: false);
        var about = new AboutWindow(main);
        try
        {
            main.ApplyConfig(new ConfigData { Interval = 1000, SequenceIndex = 0, ShowOverlay = false });
            main.Show(); about.Show();
            await main.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.Equal(Localization.Get("CheckingForUpdates"), Field<TextBlock>(about, "status").Text);
            handler.Complete();
            await main.UpdateCheckTask!.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Contains("99.0.0", Field<TextBlock>(about, "status").Text);
            Assert.Equal(Visibility.Visible, Field<System.Windows.Controls.Button>(about, "skip").Visibility);
            string beforeClose = Field<TextBlock>(about, "status").Text;
            about.Close();
            Assert.True(main.SkipAvailableUpdate());
            Assert.Equal(beforeClose, Field<TextBlock>(about, "status").Text);
        }
        finally { about.Close(); main.Close(); Directory.Delete(directory, true); }
    });

    [Fact]
    public void ClosingConditionDuringRecognitionDoesNotPublishLateResult() => WpfTestHost.RunAsync(async () =>
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var source = new Bitmap(8, 8);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) source.SetPixel(x, y, Color.FromArgb(x * 30, y * 30, (x + y) * 15));
        using var bytes = new MemoryStream(); source.Save(bytes, ImageFormat.Png);
        var condition = new ClickCondition { DetectionArea = new Rectangle(0, 0, 20, 20), TemplateImageBase64 = Convert.ToBase64String(bytes.ToArray()), Threshold = .9, PollIntervalMs = 200, TimeoutMs = 60000 };
        var window = new ConditionSettingsWindow(condition, captureScreen: _ =>
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("测试未释放截图操作。");
            return new Bitmap(source);
        });
        try
        {
            window.Show();
            var pending = (Task)typeof(ConditionSettingsWindow).GetMethod("TestAsync", Private)!.Invoke(window, null)!;
            Assert.True(await Task.Run(() => entered.Wait(TimeSpan.FromSeconds(5))));
            string beforeClose = Field<TextBlock>(window, "result").Text;
            window.Close(); release.Set();
            await pending.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(beforeClose, Field<TextBlock>(window, "result").Text);
            Assert.Null(Field<Bitmap?>(window, "templateBitmap"));
        }
        finally { release.Set(); window.Close(); }
    });

    private sealed class PendingReleaseHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource<HttpResponseMessage> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Complete() => response.TrySetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"tag_name":"v99.0.0","html_url":"https://github.com/Hfugghg/MouseClicker/releases/tag/v99.0.0","draft":false,"prerelease":false}""")
        });
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => response.Task.WaitAsync(token);
    }
}
