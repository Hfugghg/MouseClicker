using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace MouseClicker.Tests;

public class ClickSessionRunnerTests
{
    private sealed class Clock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        public Task Delay(TimeSpan duration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _ticks += duration.Ticks;
            return Task.CompletedTask;
        }
        public void Advance(int milliseconds) => _ticks += TimeSpan.FromMilliseconds(milliseconds).Ticks;
    }

    private sealed class Detector(Func<ClickCondition, int, DetectionResult> detect) : IConditionDetector
    {
        public int Calls;
        public DetectionResult Detect(ClickCondition condition) => detect(condition, ++Calls);
        public void Dispose() { }
    }

    private static DetectionResult Result(bool matches) => new(matches ? .98 : .1, new Rectangle(110, 110, 8, 8));

    private static ClickArea Area(string label = "target", bool conditional = true)
    {
        using var bitmap = new Bitmap(8, 8);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                bitmap.SetPixel(x, y, Color.FromArgb(x * 31, y * 31, (x + y) * 15));
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return new ClickArea
        {
            Label = label, Area = new Rectangle(-200, 300, 30, 40), RightButton = true,
            Condition = conditional ? new ClickCondition
            {
                DetectionArea = new Rectangle(100, 100, 32, 32),
                TemplateImageBase64 = Convert.ToBase64String(stream.ToArray()),
                PollIntervalMs = 50, TimeoutMs = 1000
            } : null
        };
    }

    [Fact]
    public async Task OnceSequence_RequiresTwoFrames_AndKeepsIndependentClickTarget()
    {
        var area = Area();
        var clock = new Clock();
        using var detector = new Detector((condition, _) =>
        {
            Assert.Equal(new Rectangle(100, 100, 32, 32), condition.DetectionArea);
            return Result(true);
        });
        int clicks = 0;
        await new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([area],
            new ClickSessionOptions { SequenceIndex = 2 }, (clicked, _) =>
            {
                Assert.Same(area, clicked);
                Assert.Equal(new Rectangle(-200, 300, 30, 40), clicked.Area);
                Assert.True(clicked.RightButton);
                Assert.Equal(2, detector.Calls);
                clicks++;
                return Task.CompletedTask;
            }, null, CancellationToken.None);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public async Task ConstantAppearance_DoesNotClickAgainAcrossLoop()
    {
        var area = Area();
        var clock = new Clock();
        using var detector = new Detector((_, _) => Result(true));
        int clicks = 0;
        var error = await Assert.ThrowsAsync<TimeoutException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([area],
                new ClickSessionOptions(), (_, _) => { clicks++; return Task.CompletedTask; }, null, CancellationToken.None));
        Assert.Equal(1, clicks);
        Assert.Contains(area.Label, error.Message);
    }

    [Fact]
    public async Task TwoMisses_RearmButSingleMissDoesNot()
    {
        var area = Area();
        var clock = new Clock();
        // 首次出现、单次未匹配、再次出现、完全消失、重新出现。
        bool[] samples = [true, true, false, true, true, false, false, true, true];
        using var detector = new Detector((_, n) => Result(samples[Math.Min(n - 1, samples.Length - 1)]));
        using var cancel = new CancellationTokenSource();
        int clicks = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([area], new ClickSessionOptions { IntervalMs = 10 },
                (_, _) => { if (++clicks == 2) cancel.Cancel(); return Task.CompletedTask; }, null, cancel.Token));
        Assert.Equal(2, clicks);
        Assert.Equal(9, detector.Calls);
    }

    [Fact]
    public async Task WhileVisible_RespectsCooldownAndRequiresPostCooldownObservation()
    {
        var area = Area();
        area.Condition!.TriggerMode = ConditionTriggerMode.WhileVisible;
        var clock = new Clock();
        using var detector = new Detector((_, _) => Result(true));
        using var cancel = new CancellationTokenSource();
        List<long> times = [];
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([area],
                new ClickSessionOptions { IntervalMs = 120 }, (_, _) =>
                {
                    times.Add(clock.GetTimestamp());
                    if (times.Count == 3) cancel.Cancel();
                    return Task.CompletedTask;
                }, null, cancel.Token));
        Assert.All(times.Zip(times.Skip(1)), pair => Assert.True(pair.Second - pair.First >= TimeSpan.FromMilliseconds(120).Ticks));
        Assert.True(detector.Calls >= 8);
    }

    [Fact]
    public async Task InactiveConditions_AreObservedAndRearmedAcrossSteps()
    {
        var first = Area("first");
        var second = Area("second");
        var clock = new Clock();
        int firstSamples = 0;
        using var detector = new Detector((condition, _) =>
        {
            if (ReferenceEquals(condition, first.Condition))
            {
                firstSamples++;
                return Result(firstSamples is <= 2 or >= 5);
            }
            return Result(firstSamples >= 5);
        });
        using var cancel = new CancellationTokenSource();
        List<string> clicks = [];
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([first, second],
                new ClickSessionOptions { IntervalMs = 10 }, (area, _) =>
                {
                    clicks.Add(area.Label);
                    if (clicks.Count == 3) cancel.Cancel();
                    return Task.CompletedTask;
                }, null, cancel.Token));
        Assert.Equal(new[] { "first", "second", "first" }, clicks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task UnmatchedCurrentStep_IsNotSkipped(int sequence)
    {
        var first = Area("blocked");
        var second = Area("ordinary", false);
        var clock = new Clock();
        using var detector = new Detector((_, _) => Result(false));
        int clicks = 0;
        await Assert.ThrowsAsync<TimeoutException>(() =>
            new ClickSessionRunner(detector, clock, new ZeroRandom(), clock.Delay).RunAsync([first, second],
                new ClickSessionOptions { SequenceIndex = sequence }, (_, _) => { clicks++; return Task.CompletedTask; }, null, CancellationToken.None));
        Assert.Equal(0, clicks);
    }

    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }

    [Fact]
    public async Task CancellationDuringDetection_NeverSendsClick()
    {
        var clock = new Clock();
        using var cancel = new CancellationTokenSource();
        using var detector = new Detector((_, n) => { if (n == 2) cancel.Cancel(); return Result(true); });
        int clicks = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([Area()], new ClickSessionOptions(),
                (_, _) => { clicks++; return Task.CompletedTask; }, null, cancel.Token));
        Assert.Equal(0, clicks);
    }

    [Fact]
    public async Task TimeoutZero_WaitsUntilCancelled_AndNewSessionResetsLatch()
    {
        var area = Area();
        area.Condition!.TimeoutMs = 0;
        var clock = new Clock();
        using var cancel = new CancellationTokenSource();
        using var detector = new Detector((_, n) => { if (n == 50) cancel.Cancel(); return Result(true); });
        int clicks = 0;
        var runner = new ClickSessionRunner(detector, clock, delay: clock.Delay);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.RunAsync([area], new ClickSessionOptions(),
            (_, _) => { clicks++; return Task.CompletedTask; }, null, cancel.Token));
        await runner.RunAsync([area], new ClickSessionOptions { SequenceIndex = 2 },
            (_, _) => { clicks++; return Task.CompletedTask; }, null, CancellationToken.None);
        Assert.Equal(2, clicks);
    }

    [Fact]
    public async Task OrdinaryOnceSequence_PerformsEveryAreaInOrderWithItsMouseButton()
    {
        var first = Area("first", false);
        first.RightButton = false;
        var second = Area("second", false);
        second.Area = new Rectangle(600, -400, 10, 20);
        var input = new TestDoubles.FakeMouseInput();
        var mouse = new MouseController(input);
        var clock = new Clock();
        using var detector = new Detector((_, _) => throw new InvalidOperationException("Ordinary steps must not detect."));
        List<string> clicked = [];
        await new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([first, second],
            new ClickSessionOptions { SequenceIndex = 2 }, (area, _) =>
            {
                clicked.Add(area.Label);
                mouse.PerformRandomClickInArea(area.Area, rightButton: area.RightButton);
                return Task.CompletedTask;
            }, null, CancellationToken.None);
        Assert.Equal(new[] { "first", "second" }, clicked);
        Assert.Equal(new[] { false, true }, input.SoftwareClickButtons);
        Assert.True(first.Area.Contains(input.CursorPositions[0].X, input.CursorPositions[0].Y));
        Assert.True(second.Area.Contains(input.CursorPositions[1].X, input.CursorPositions[1].Y));
        Assert.Equal(0, detector.Calls);
    }

    [Fact]
    public async Task LongCooldown_DoesNotConsumeMatchingConditionTimeout()
    {
        var ordinary = Area("ordinary", false);
        var conditional = Area("conditional");
        conditional.Condition!.TimeoutMs = 60000;
        var clock = new Clock();
        using var detector = new Detector((_, _) => Result(true));
        List<string> clicks = [];
        await new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([ordinary, conditional],
            new ClickSessionOptions { SequenceIndex = 2, IntervalMs = 120000 }, (area, _) =>
            {
                clicks.Add(area.Label);
                if (ReferenceEquals(area, conditional))
                    Assert.True(clock.GetTimestamp() >= TimeSpan.FromMilliseconds(120000).Ticks);
                return Task.CompletedTask;
            }, null, CancellationToken.None);
        Assert.Equal(new[] { "ordinary", "conditional" }, clicks);
    }

    [Fact]
    public async Task UnmatchedCondition_TimeoutStartsAfterLongCooldown()
    {
        var conditional = Area("blocked");
        conditional.Condition!.TimeoutMs = 60000;
        var clock = new Clock();
        using var detector = new Detector((_, _) => Result(false));
        int clicks = 0;
        await Assert.ThrowsAsync<TimeoutException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([Area("ordinary", false), conditional],
                new ClickSessionOptions { SequenceIndex = 2, IntervalMs = 120000 },
                (_, _) => { clicks++; return Task.CompletedTask; }, null, CancellationToken.None));
        Assert.Equal(1, clicks);
        Assert.Equal(TimeSpan.FromMilliseconds(180000).Ticks, clock.GetTimestamp());
    }

    [Fact]
    public async Task InfiniteTimeout_AfterLongCooldown_WaitsUntilCancellation()
    {
        var conditional = Area("blocked");
        conditional.Condition!.TimeoutMs = 0;
        var clock = new Clock();
        using var cancel = new CancellationTokenSource();
        using var detector = new Detector((_, _) =>
        {
            if (clock.GetTimestamp() >= TimeSpan.FromMilliseconds(200000).Ticks) cancel.Cancel();
            return Result(false);
        });
        int clicks = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([Area("ordinary", false), conditional],
                new ClickSessionOptions { SequenceIndex = 2, IntervalMs = 120000 },
                (_, _) => { clicks++; return Task.CompletedTask; }, null, cancel.Token));
        Assert.Equal(1, clicks);
        Assert.Equal(TimeSpan.FromMilliseconds(200000).Ticks, clock.GetTimestamp());
    }

    [Fact]
    public async Task DetectorFailure_ReportsAreaAndNeverFallsBackToClick()
    {
        var clock = new Clock();
        using var detector = new Detector((_, _) => throw new IOException("capture failed"));
        int clicks = 0;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ClickSessionRunner(detector, clock, delay: clock.Delay).RunAsync([Area("bad")], new ClickSessionOptions(),
                (_, _) => { clicks++; return Task.CompletedTask; }, null, CancellationToken.None));
        Assert.Contains("bad", exception.Message);
        Assert.IsType<IOException>(exception.InnerException);
        Assert.Equal(0, clicks);
    }
}
