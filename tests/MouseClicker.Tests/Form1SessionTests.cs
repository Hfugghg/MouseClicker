using System.Drawing;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MouseClicker.Tests.TestDoubles;
using Xunit;

namespace MouseClicker.Tests;

public class Form1SessionTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(Form1 form, string name) => (T)typeof(Form1).GetField(name, PrivateInstance)!.GetValue(form)!;
    private static void Call(Form1 form, string name) => typeof(Form1).GetMethod(name, PrivateInstance)!.Invoke(form, null);

    [DllImport("user32.dll")]
    private static extern uint GetQueueStatus(uint flags);

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }

    private sealed class CaptureContext : SynchronizationContext
    {
        public SendOrPostCallback? Callback;
        public object? State;
        public override void Post(SendOrPostCallback callback, object? state) { Callback = callback; State = state; }
    }

    private static Task StartWorkerWithoutPumping(Form1 form)
    {
        var originalContext = SynchronizationContext.Current;
        var capture = new CaptureContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(capture);
            Call(form, "StartClicking");
        }
        finally { SynchronizationContext.SetSynchronizationContext(originalContext); }
        var session = Field<Task>(form, "_clickSession");
        Assert.NotNull(capture.Callback);
        capture.Callback(capture.State);
        return session;
    }

    private static async Task OnUiThread(Func<Form1, FakeMouseInput, Task> body)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var client = new HttpClient(new OfflineHandler());
                using var form = new Form1(client, null) { ShowInTaskbar = false, Opacity = 0 };
                var input = new FakeMouseInput();
                typeof(Form1).GetField("_mouseController", PrivateInstance)!.SetValue(form, new MouseController(input));
                typeof(Form1).GetField("_clickAreas", PrivateInstance)!.SetValue(form, new List<ClickArea>
                {
                    new() { Label = "fake-only", Area = new Rectangle(10, 10, 20, 20) }
                });
                Field<CheckBox>(form, "showOverlayCheckBox").Checked = false;
                Field<NumericUpDown>(form, "intervalNumericUpDown").Value = 10;
                Field<NumericUpDown>(form, "jitterNumericUpDown").Value = 0;
                _ = form.Handle;
                form.BeginInvoke(async () =>
                {
                    try { await body(form, input); completion.TrySetResult(); }
                    catch (Exception ex) { completion.TrySetException(ex); }
                    finally { form.Dispose(); Application.ExitThread(); }
                });
                Application.Run();
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        }) { IsBackground = true, Name = "Form1 session integration test" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "STA message loop did not shut down.");
    }

    [Fact]
    public Task StopCancelsQueuedUiClick_AndPreventsReentrantStart() => OnUiThread(async (form, input) =>
    {
        var original = StartWorkerWithoutPumping(form);
        Call(form, "StartClicking");
        Assert.Same(original, Field<Task>(form, "_clickSession"));
        // STA 线程忙碌期间工作线程仍在运行：输入会保持排队。
        Assert.True(SpinWait.SpinUntil(() => (GetQueueStatus(0x0008) >> 16) != 0, TimeSpan.FromSeconds(5)),
            "Worker did not post its UI callback.");
        Thread.Sleep(100);
        Assert.Equal(0, input.TotalClickCount);
        Call(form, "StopClicking");
        Call(form, "StartClicking");
        Assert.Same(original, Field<Task>(form, "_clickSession"));
        await original.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, input.TotalClickCount);
        Assert.False(Field<bool>(form, "_isClicking"));
    });

    [Fact]
    public Task CloseCancelsQueuedClickWithoutInput() => OnUiThread(async (form, input) =>
    {
        var session = StartWorkerWithoutPumping(form);
        Assert.True(SpinWait.SpinUntil(() => (GetQueueStatus(0x0008) >> 16) != 0, TimeSpan.FromSeconds(5)));
        Thread.Sleep(100);
        Assert.Equal(0, input.TotalClickCount);
        form.Close();
        await session.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, input.TotalClickCount);
        Assert.True(Field<bool>(form, "_isClosing"));
    });
}
