using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using MouseClicker.Tests.TestDoubles;
using MouseClicker.Wpf;
using Xunit;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;

namespace MouseClicker.Tests;

public class WpfMainWindowTests
{
    [Fact]
    public void ConfigRoundTripPreservesOldDefaultsAndInvalidLoadIsAtomic() => WpfTestHost.Run(() =>
    {
        var window = new MainWindow(null, enableHotkeys: false);
        try
        {
            var old = JsonSerializer.Deserialize<ConfigData>("""
                {"ClickAreas":[{"Area":{"X":-300,"Y":20,"Width":50,"Height":30},"Label":"旧区域","RightButton":true}],"Interval":1000,"Jitter":100,"SequenceIndex":2,"ShowOverlay":false}
                """)!;
            window.ApplyConfig(old);
            var actual = window.CaptureConfig();
            Assert.True(actual.HighlightCurrentArea);
            Assert.False(actual.ShowDetectionOverlay);
            Assert.True(actual.ClickAreas[0].RightButton);
            Assert.Equal(-300, actual.ClickAreas[0].Area.X);
            Assert.Throws<InvalidDataException>(() => window.ApplyConfig(new ConfigData { Interval = 0, Jitter = 0, SequenceIndex = 0 }));
            Assert.Equal("旧区域", window.CaptureConfig().ClickAreas[0].Label);
            Assert.Equal(1000, window.CaptureConfig().Interval);
            ((TextBox)window.FindName("IntervalInput")).Text = "无效输入";
            Assert.Throws<InvalidDataException>(() => window.CaptureConfig());
        }
        finally { window.Close(); }
    });

    [Fact]
    public void ReorderAndMouseButtonChangesPreserveSelectionAndOtherAreas() => WpfTestHost.Run(() =>
    {
        var window = new MainWindow(null, enableHotkeys: false);
        try
        {
            window.ApplyConfig(Config());
            var list = (ListBox)window.FindName("AreasList");
            list.SelectedIndex = 1;
            window.SetButton(true);
            window.MoveArea(-1);
            Assert.Equal("第二项", window.CaptureConfig().ClickAreas[0].Label);
            Assert.True(window.CaptureConfig().ClickAreas[0].RightButton);
            Assert.False(window.CaptureConfig().ClickAreas[1].RightButton);
            Assert.Equal(0, list.SelectedIndex);
            window.MoveArea(-1);
            Assert.Equal(0, list.SelectedIndex);
            window.MoveArea(1);
            Assert.Equal(1, list.SelectedIndex);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void ClosingPersistsSessionAndNextWindowRestoresIt() => WpfTestHost.Run(() =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "MouseClicker-Wpf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new SessionConfigStore(Path.Combine(directory, "session.json"));
            var first = new MainWindow(store, enableHotkeys: false);
            first.ApplyConfig(Config()); first.Close();
            Assert.NotNull(store.Load());
            var second = new MainWindow(store, enableHotkeys: false);
            try { Assert.Equal("第二项", second.CaptureConfig().ClickAreas[1].Label); }
            finally { second.Close(); }
        }
        finally { Directory.Delete(directory, true); }
    });

    [Fact]
    public void CorruptSessionIsBackedUpBeforeCloseWritesReplacement() => WpfTestHost.Run(() =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "MouseClicker-Wpf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "session.json");
            File.WriteAllText(path, "{损坏数据");
            var window = new MainWindow(new SessionConfigStore(path), enableHotkeys: false);
            try
            {
                string backup = Assert.Single(Directory.GetFiles(directory, "*.bak"));
                Assert.Equal("{损坏数据", File.ReadAllText(backup));
                Assert.Empty(window.CaptureConfig().ClickAreas);
            }
            finally { window.Close(); }
            Assert.NotNull(JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(path)));
        }
        finally { Directory.Delete(directory, true); }
    });

    [Theory]
    [InlineData(false, 0, false, true)]
    [InlineData(true, 0, false, false)]
    [InlineData(false, 1, false, false)]
    [InlineData(false, 0, true, false)]
    public void QueuedHotkeyCannotStartAcrossModalBoundary(bool selecting, int depth, bool blockedAtPress, bool expected)
        => Assert.Equal(expected, MainWindow.CanStartFromHotkey(selecting, depth, blockedAtPress));

    [Fact]
    public void SemanticallyInvalidSessionIsBackedUpInsteadOfCrashingStartup() => WpfTestHost.Run(() =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "MouseClicker-Wpf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "session.json");
            string invalid = JsonSerializer.Serialize(new ConfigData { Interval = -1, SequenceIndex = 99 });
            File.WriteAllText(path, invalid);
            var window = new MainWindow(new SessionConfigStore(path), enableHotkeys: false);
            try
            {
                Assert.Equal(invalid, File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "*.bak"))));
                Assert.Equal(1000, window.CaptureConfig().Interval);
            }
            finally { window.Close(); }
        }
        finally { Directory.Delete(directory, true); }
    });

    [Fact]
    public void FailedSaveRequiresExplicitDiscardAndLeavesPreviousFileUntouched() => WpfTestHost.Run(() =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "MouseClicker-Wpf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "session.json");
            var store = new SessionConfigStore(path); store.Save(Config());
            string previous = File.ReadAllText(path);
            var window = new MainWindow(store, enableHotkeys: false);
            try
            {
                ((TextBox)window.FindName("IntervalInput")).Text = "无效";
                Assert.False(window.TrySaveBeforeClose(error => { Assert.IsType<InvalidDataException>(error); return false; }));
                Assert.Equal(previous, File.ReadAllText(path));
                Assert.True(window.TrySaveBeforeClose(_ => true));
                Assert.Equal(previous, File.ReadAllText(path));
                ((TextBox)window.FindName("IntervalInput")).Text = "1000";
            }
            finally { window.Close(); }
        }
        finally { Directory.Delete(directory, true); }
    });

    [Fact]
    public void LegacyFractionalTimingSurvivesRoundTrip() => WpfTestHost.Run(() =>
    {
        var window = new MainWindow(null, enableHotkeys: false);
        try
        {
            var config = Config(); config.Interval = 1000.5m; config.Jitter = 100.25m;
            window.ApplyConfig(config);
            Assert.Equal(config.Interval, window.CaptureConfig().Interval);
            Assert.Equal(config.Jitter, window.CaptureConfig().Jitter);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void SessionThatCannotBeReadOrBackedUpIsNeverOverwrittenOnClose() => WpfTestHost.Run(() =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "MouseClicker-Wpf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "session.json");
            File.WriteAllText(path, "保留原始内容");
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var window = new MainWindow(new SessionConfigStore(path), enableHotkeys: false);
                window.ApplyConfig(Config());
                Assert.True(window.TrySaveBeforeClose(_ => throw new InvalidOperationException("受保护的会话不应尝试保存。")));
                window.Close();
            }
            Assert.Equal("保留原始内容", File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(directory, "*.bak"));
        }
        finally { Directory.Delete(directory, true); }
    });

    [Fact]
    public void SinglePassUsesFakeInputAndCompletesWithCorrectButtons() => WpfTestHost.RunAsync(async () =>
    {
        var input = new FakeMouseInput();
        var window = new MainWindow(null, enableHotkeys: false, mouseInput: input);
        try
        {
            var config = Config(); config.SequenceIndex = 2; config.ClickAreas[1].RightButton = true;
            window.ApplyConfig(config);
            window.StartSession();
            Assert.True(window.IsRunning);
            var task = window.SessionTask;
            Assert.NotNull(task);
            await task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(window.IsRunning);
            Assert.Null(window.SessionTask);
            Assert.Equal(new[] { false, true }, input.SoftwareClickButtons);
            Assert.Equal(2, input.CursorPositions.Count);
            Assert.True(config.ClickAreas[0].Area.Contains(new Point(input.CursorPositions[0].X, input.CursorPositions[0].Y)));
            Assert.True(((Button)window.FindName("StartButton")).IsEnabled);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void ModalStartIsBlockedAndStopAwaitsCompletionBeforeRestart() => WpfTestHost.RunAsync(async () =>
    {
        var input = new FakeMouseInput();
        var window = new MainWindow(null, enableHotkeys: false, mouseInput: input);
        try
        {
            window.ApplyConfig(Config());
            window.Modal(() => { window.StartSession(); Assert.Null(window.SessionTask); return true; });
            window.StartSession();
            var task = window.SessionTask!;
            window.StopSession();
            window.StartSession();
            Assert.Same(task, window.SessionTask);
            await task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Null(window.SessionTask);
            Assert.Equal(0, input.TotalClickCount);
            window.StartSession();
            var next = window.SessionTask!;
            window.Close();
            await next.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(window.IsClosing);
            Assert.False(window.IsRunning);
            Assert.Equal(0, input.TotalClickCount);
        }
        finally { if (!window.IsClosing) window.Close(); }
    });

    [Fact]
    public void BackgroundUpdateAndSkipUseInjectedHttpAndPreferenceStore() => WpfTestHost.RunAsync(async () =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "MouseClicker-Wpf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        using var http = new HttpClient(new ReleaseHandler());
        var preferences = new UpdatePreferencesStore(Path.Combine(directory, "updates.json"));
        var window = new MainWindow(null, http, preferences, enableHotkeys: false);
        try
        {
            var config = Config(); config.ShowOverlay = false; window.ApplyConfig(config);
            window.Show();
            await window.UpdateCheckTask!.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(window.CheckingForUpdates);
            Assert.True(window.UpdateCheckSucceeded);
            Assert.Equal(new Version(99, 0, 0, 0), window.AvailableUpdate?.Version);
            Assert.True(window.SkipAvailableUpdate());
            Assert.Null(window.AvailableUpdate);
            Assert.True(preferences.IsSkipped(new Version(99, 0, 0)));
        }
        finally { window.Close(); Directory.Delete(directory, true); }
    });

    private static ConfigData Config() => new()
    {
        Interval = 10, Jitter = 0, SequenceIndex = 0, ShowOverlay = false,
        ClickAreas = [new() { Label = "第一项", Area = new Rectangle(-300, 20, 10, 10) }, new() { Label = "第二项", Area = new Rectangle(500, 200, 20, 15) }]
    };

    private sealed class ReleaseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"tag_name":"v99.0.0","html_url":"https://github.com/Hfugghg/MouseClicker/releases/tag/v99.0.0","draft":false,"prerelease":false} """)
            });
    }
}
