using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace MouseClicker
{
    /// <summary>Owns the global keyboard hook and persists the user's selected toggle key.</summary>
    internal sealed class GlobalHotkeyManager : IDisposable
    {
        private const int WhKeyboardLl = 13, WmKeyDown = 0x0100, WmKeyUp = 0x0101, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105;
        private static readonly LowLevelKeyboardProc HookProc = HookCallback;
        private static GlobalHotkeyManager? _instance;
        private readonly HashSet<int> _pressedKeys = new();
        private readonly string _settingsPath;
        private IntPtr _hookId;
        private bool _disposed;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr module, uint threadId);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        public GlobalHotkeyManager()
        {
            _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MouseClicker", "hotkey.json");
            Hotkey = LoadHotkey();
            if (_instance != null) throw new InvalidOperationException("Only one global hotkey manager can be active.");
            _instance = this;
            _hookId = SetWindowsHookEx(WhKeyboardLl, HookProc, IntPtr.Zero, 0);
            if (_hookId == IntPtr.Zero)
            {
                _instance = null;
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install the global keyboard hook.");
            }
        }

        public Keys Hotkey { get; private set; } = Keys.F6;
        public event Action<Keys>? KeyPressed;
        public Func<bool>? ShouldSuppressEscape { get; set; }
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
            catch { /* Ignore a missing or invalid preference and use the default. */ }
            return Keys.F6;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var instance = _instance;
            if (instance != null && nCode >= 0)
            {
                int message = wParam.ToInt32();
                int keyCode = Marshal.ReadInt32(lParam);
                if (message == WmKeyDown || message == WmSysKeyDown)
                {
                    if (instance._pressedKeys.Add(keyCode))
                    {
                        var key = (Keys)keyCode;
                        if (key == instance.Hotkey || key == Keys.Escape) instance.KeyPressed?.Invoke(key);
                    }
                }
                else if (message == WmKeyUp || message == WmSysKeyUp) instance._pressedKeys.Remove(keyCode);
            }
            if (instance != null && nCode >= 0 &&
                (wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown) &&
                Marshal.ReadInt32(lParam) == (int)Keys.Escape &&
                instance.ShouldSuppressEscape?.Invoke() == true)
                return (IntPtr)1;

            return CallNextHookEx(instance?._hookId ?? IntPtr.Zero, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_hookId != IntPtr.Zero) { UnhookWindowsHookEx(_hookId); _hookId = IntPtr.Zero; }
            if (ReferenceEquals(_instance, this)) _instance = null;
            _pressedKeys.Clear();
        }

        private sealed class HotkeyPreferences { public string Hotkey { get; set; } = nameof(Keys.F6); }
    }
}
