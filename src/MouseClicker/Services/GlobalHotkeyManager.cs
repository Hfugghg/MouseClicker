using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

namespace MouseClicker
{
    /// <summary>
    /// 负责全局键盘钩子，并保存用户选择的切换热键。
    /// </summary>
    internal sealed class GlobalHotkeyManager : IDisposable
    {
        private const int WhKeyboardLl = 13, WmKeyDown = 0x0100, WmKeyUp = 0x0101, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105;
        private const int VkShift = 0x10, VkControl = 0x11, VkMenu = 0x12, VkLWin = 0x5B, VkRWin = 0x5C;
        private readonly LowLevelKeyboardProc _hookProc;
        private readonly Thread _hookThread;
        private uint _threadId;
        private volatile Keys _hotkey = Keys.F6;
        private readonly HashSet<int> _pressedKeys = new();
        private readonly string _settingsPath;
        private IntPtr _hookId;
        private bool _disposed;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Window;
            public uint Message;
            public UIntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public System.Drawing.Point Point;
            public uint Private;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMessage message, IntPtr window, uint minimum, uint maximum, uint remove);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetMessage(out NativeMessage message, IntPtr window, uint minimum, uint maximum);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr module, uint threadId);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        public GlobalHotkeyManager()
        {
            _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MouseClicker", "hotkey.json");
            Hotkey = LoadHotkey();
            _hookProc = HookCallback;
            using var ready = new ManualResetEventSlim();
            Exception? startupError = null;
            _hookThread = new Thread(() =>
            {
                try
                {
                    _threadId = GetCurrentThreadId();
                    PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
                    InstallHook();
                }
                catch (Exception ex) { startupError = ex; }
                finally { ready.Set(); }

                if (startupError != null) return;
                try
                {
                    while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0) { }
                    // GetMessage 的 -1 返回值也必须退出并释放钩子。
                }
                finally
                {
                    if (_hookId != IntPtr.Zero) UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                    _pressedKeys.Clear();
                }
            }) { IsBackground = true, Name = "MouseClicker global keyboard hook" };
            _hookThread.Start();
            ready.Wait();
            if (startupError != null)
            {
                _hookThread.Join();
                ExceptionDispatchInfo.Capture(startupError).Throw();
            }
        }

        private void InstallHook()
        {
            IntPtr moduleHandle = IntPtr.Zero;
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule? currentModule = currentProcess.MainModule)
            {
                if (currentModule?.ModuleName is string moduleName)
                    moduleHandle = GetModuleHandle(moduleName);
            }
            _hookId = SetWindowsHookEx(WhKeyboardLl, _hookProc, moduleHandle, 0);
            if (_hookId == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install the global keyboard hook.");
            }
        }

        public Keys Hotkey { get => _hotkey; private set => _hotkey = value; }
        private volatile Action<Keys>? _keyPressed;
        private volatile Func<bool>? _shouldSuppressEscape;
        public event Action<Keys>? KeyPressed
        {
            add => _keyPressed += value;
            remove => _keyPressed -= value;
        }
        public Func<bool>? ShouldSuppressEscape { get => _shouldSuppressEscape; set => _shouldSuppressEscape = value; }
        public static IReadOnlyList<Keys> SupportedKeys { get; } = new[]
        {
            Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.Insert, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.Pause
        };

        public void SetHotkey(Keys key)
        {
            if (!SupportedKeys.Contains(key)) throw new ArgumentOutOfRangeException(nameof(key), "The selected key is not supported.");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new HotkeyPreferences { Hotkey = key.ToString() }));
            Hotkey = key;
        }

        private Keys LoadHotkey()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return Keys.F6;
                var preferences = JsonSerializer.Deserialize<HotkeyPreferences>(File.ReadAllText(_settingsPath));
                if (preferences != null && Enum.TryParse(preferences.Hotkey, true, out Keys key) && SupportedKeys.Contains(key)) return key;
            }
            catch { /* 忽略缺失或无效的偏好设置，改用默认值。 */ }
            return Keys.F6;
        }

        /// <summary>
        /// 钩子对一次按键的处置方式。
        /// </summary>
        [Flags]
        internal enum HookKeyDecision
        {
            PassThrough = 0,
            Invoke = 1,
            Suppress = 2,
            InvokeAndSuppress = Invoke | Suppress
        }

        /// <summary>
        /// 判断一次按键应触发热键、拦截，还是原样交给前台程序。
        /// 只有未按任何修饰键时才响应；ESC 仅在点击过程中拦截。
        /// </summary>
        internal static HookKeyDecision DecideKeyDown(
            Keys key, Keys hotkey,
            bool controlDown, bool altDown, bool shiftDown, bool windowsDown,
            bool isClicking)
        {
            // 带修饰键的按键属于前台程序的组合键，不触发也不拦截。
            if (controlDown || altDown || shiftDown || windowsDown) return HookKeyDecision.PassThrough;
            if (key == Keys.Escape)
                return isClicking ? HookKeyDecision.InvokeAndSuppress : HookKeyDecision.Invoke;
            if (key == hotkey) return HookKeyDecision.Invoke;
            return HookKeyDecision.PassThrough;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var instance = this;
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                int keyCode = Marshal.ReadInt32(lParam);
                if (message == WmKeyDown || message == WmSysKeyDown)
                {
                    // 记录按下状态，按住按键时的重复事件不再触发。
                    bool firstPress = instance._pressedKeys.Add(keyCode);
                    var key = (Keys)keyCode;
                    if (key == instance.Hotkey || key == Keys.Escape)
                    {
                        // 使用异步按键状态判断修饰键，避免钩子线程的消息队列状态滞后。
                        bool controlDown = IsKeyDown(VkControl);
                        bool altDown = IsKeyDown(VkMenu);
                        bool shiftDown = IsKeyDown(VkShift);
                        bool windowsDown = IsKeyDown(VkLWin) || IsKeyDown(VkRWin);
                        bool isClicking = key == Keys.Escape && instance.ShouldSuppressEscape?.Invoke() == true;
                        var decision = DecideKeyDown(key, instance.Hotkey, controlDown, altDown, shiftDown, windowsDown, isClicking);
                        if (firstPress && (decision & HookKeyDecision.Invoke) == HookKeyDecision.Invoke)
                            instance._keyPressed?.Invoke(key);
                        if ((decision & HookKeyDecision.Suppress) == HookKeyDecision.Suppress)
                            return (IntPtr)1;
                    }
                }
                else if (message == WmKeyUp || message == WmSysKeyUp)
                {
                    instance._pressedKeys.Remove(keyCode);
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static bool IsKeyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_hookThread.IsAlive && !PostThreadMessage(_threadId, 0x0012, UIntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to stop the global keyboard hook thread.");
            if (Thread.CurrentThread != _hookThread) _hookThread.Join();
            GC.KeepAlive(_hookProc);
        }

        private sealed class HotkeyPreferences { public string Hotkey { get; set; } = nameof(Keys.F6); }
    }
}
