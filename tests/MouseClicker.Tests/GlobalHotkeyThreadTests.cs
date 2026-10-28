using System.Reflection;
using Xunit;

namespace MouseClicker.Tests;

public class GlobalHotkeyThreadTests
{
    private static Thread HookThread(GlobalHotkeyManager manager) =>
        (Thread)typeof(GlobalHotkeyManager).GetField("_hookThread", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;

    [Fact]
    public async Task HookLoop_StopsWithoutPumpingOrUnblockingTheCreatingThread()
    {
        using var created = new ManualResetEventSlim();
        using var releaseCreator = new ManualResetEventSlim();
        GlobalHotkeyManager? manager = null;
        Exception? error = null;
        var creator = new Thread(() =>
        {
            try { manager = new GlobalHotkeyManager(); }
            catch (Exception ex) { error = ex; }
            finally { created.Set(); }
            releaseCreator.Wait();
        }) { IsBackground = true };
        creator.Start();
        try
        {
            Assert.True(created.Wait(TimeSpan.FromSeconds(10)));
            Assert.Null(error);
            Assert.NotNull(manager);
            Thread hookThread = HookThread(manager);
            Assert.NotSame(creator, hookThread);
            Assert.True(hookThread.IsAlive);
            // 创建线程保持阻塞超过 Windows 的最大钩子超时；退出消息仍必须被独立循环处理。
            await Task.Delay(1200);
            Task dispose = Task.Run(manager.Dispose);
            await dispose.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(hookThread.IsAlive);
            manager.Dispose();
        }
        finally
        {
            releaseCreator.Set();
            creator.Join(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public void RepeatedConstructionAndDisposal_LeavesNoHookThreadRunning()
    {
        for (int i = 0; i < 3; i++)
        {
            var manager = new GlobalHotkeyManager();
            Thread thread = HookThread(manager);
            Assert.True(thread.IsAlive);
            manager.Dispose();
            Assert.False(thread.IsAlive);
        }
    }
}
