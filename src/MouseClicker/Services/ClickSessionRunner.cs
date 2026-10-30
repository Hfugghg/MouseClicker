namespace MouseClicker;

internal sealed record ClickSessionOptions
{
    public int SequenceIndex { get; init; }
    public int IntervalMs { get; init; } = 100;
    public int JitterMs { get; init; }
}

/// <summary>串行执行一次点击会话。调用方在 UI 线程之外运行它，并负责释放检测器。</summary>
internal sealed class ClickSessionRunner
{
    private readonly IConditionDetector _detector;
    private readonly TimeProvider _clock;
    private readonly Random _random;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public ClickSessionRunner(IConditionDetector detector, TimeProvider? timeProvider = null, Random? random = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _detector = detector;
        _clock = timeProvider ?? TimeProvider.System;
        _random = random ?? new Random();
        _delay = delay ?? ((duration, token) => Task.Delay(duration, _clock, token));
    }

    public async Task RunAsync(IReadOnlyList<ClickArea> areas, ClickSessionOptions options,
        Func<ClickArea, CancellationToken, Task> clickAsync,
        Action<ClickArea, DetectionResult?>? status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(areas);
        ArgumentNullException.ThrowIfNull(clickAsync);
        if (options.SequenceIndex is < 0 or > 2 || options.IntervalMs < 0 || options.JitterMs < 0)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (areas.Count == 0) return;
        var states = new Dictionary<ClickArea, Observation>();
        foreach (var area in areas)
        {
            if (area.Condition is { } condition)
            {
                condition.Validate();
                states.TryAdd(area, new Observation());
            }
        }

        long start = _clock.GetTimestamp();
        TimeSpan Now() => _clock.GetElapsedTime(start);
        int index = options.SequenceIndex == 1 ? _random.Next(areas.Count) : 0;
        TimeSpan selectedAt = Now(), nextClickAt = TimeSpan.Zero;
        bool announce = true;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = areas[index];
            if (announce)
            {
                status?.Invoke(current, null);
                announce = false;
            }

            // 也要观察非当前项：出现锁存会跨序列周期保留，
            // 但在执行其他项期间，画面消失可以让它重新就绪。
            foreach (var pair in states.OrderBy(pair => ReferenceEquals(pair.Key, current) ? 1 : 0))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var condition = pair.Key.Condition!;
                var state = pair.Value;
                if (Now() < state.NextPoll) continue;
                DetectionResult result;
                try { result = _detector.Detect(condition); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { throw new InvalidOperationException($"[{pair.Key.Label}] " + Localization.Get("ConditionErrorRecognition") + " " + ex.Message, ex); }
                cancellationToken.ThrowIfCancellationRequested();
                state.LastObserved = Now();
                state.NextPoll = state.LastObserved + TimeSpan.FromMilliseconds(condition.PollIntervalMs);
                state.Result = result;
                bool matches = double.IsFinite(result.Score) && result.Score >= condition.Threshold;
                state.Hits = matches ? Math.Min(2, state.Hits + 1) : 0;
                state.Misses = matches ? 0 : Math.Min(2, state.Misses + 1);
                if (state.Misses >= 2) state.Consumed = false;
                if (ReferenceEquals(pair.Key, current) &&
                    (state.LastReportedMatch != matches || Now() - state.LastReported >= TimeSpan.FromMilliseconds(250)))
                {
                    status?.Invoke(current, result);
                    state.LastReported = Now();
                    state.LastReportedMatch = matches;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            TimeSpan now = Now();
            bool ready = now >= nextClickAt;
            if (current.Condition is { } currentCondition)
            {
                // 点击后的强制冷却不应算作条件等待时间。
                TimeSpan conditionWaitStarted = selectedAt > nextClickAt ? selectedAt : nextClickAt;
                if (currentCondition.TimeoutMs > 0 && now - conditionWaitStarted >= TimeSpan.FromMilliseconds(currentCondition.TimeoutMs))
                    throw new TimeoutException($"[{current.Label}] " + Localization.Get("ConditionErrorTimeout"));
                var state = states[current];
                // 必须使用点击冷却和步骤切换之后采集的样本；
                // 绝不能拿上一项遗留的画面结果来点击。
                ready &= state.Hits >= 2 && now < state.NextPoll &&
                    state.LastObserved >= nextClickAt && state.LastObserved >= selectedAt &&
                    (currentCondition.TriggerMode == ConditionTriggerMode.WhileVisible || !state.Consumed);
            }

            if (ready)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await clickAsync(current, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (states.TryGetValue(current, out var clickedState)) clickedState.Consumed = true;
                if (options.SequenceIndex == 2 && index == areas.Count - 1) return;
                index = options.SequenceIndex == 1 ? _random.Next(areas.Count) : (index + 1) % areas.Count;
                selectedAt = Now();
                long jitter = _random.NextInt64(-(long)options.JitterMs, (long)options.JitterMs + 1);
                nextClickAt = selectedAt + TimeSpan.FromMilliseconds(Math.Max(10L, Math.Abs(options.IntervalMs + jitter)));
                announce = true;
                continue;
            }

            // 在保留各项独立轮询周期的同时，限制取消响应的延迟。
            TimeSpan wake = now + TimeSpan.FromMilliseconds(25);
            foreach (var state in states.Values) if (state.NextPoll < wake) wake = state.NextPoll;
            if (nextClickAt > now && nextClickAt < wake) wake = nextClickAt;
            await _delay(wake > now ? wake - now : TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class Observation
    {
        public TimeSpan NextPoll;
        public TimeSpan LastObserved;
        public TimeSpan LastReported;
        public bool? LastReportedMatch;
        public DetectionResult Result;
        public int Hits;
        public int Misses;
        public bool Consumed;
    }
}
