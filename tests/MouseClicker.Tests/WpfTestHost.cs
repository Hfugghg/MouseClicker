using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace MouseClicker.Tests;

/// <summary>为 WPF 窗口提供独立 STA 线程和可推进异步操作的消息循环。</summary>
internal static class WpfTestHost
{
    internal static void Run(Action action) => RunAsync(() => { action(); return Task.CompletedTask; });

    internal static void RunAsync(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            var frame = new DispatcherFrame();
            var watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            watchdog.Tick += (_, _) =>
            {
                failure = new TimeoutException("WPF 测试未在限定时间内完成。");
                frame.Continue = false;
            };
            dispatcher.BeginInvoke(async () =>
            {
                try { await action(); }
                catch (Exception ex) { failure = ex; }
                finally { frame.Continue = false; }
            });
            watchdog.Start();
            Dispatcher.PushFrame(frame);
            watchdog.Stop();
            dispatcher.InvokeShutdown();
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(20))) throw new TimeoutException("WPF 测试线程未退出。");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
