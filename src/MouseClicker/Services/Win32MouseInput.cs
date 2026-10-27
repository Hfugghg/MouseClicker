using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MouseClicker
{
    /// <summary>
    /// 基于 Win32 API 的鼠标输入实现。
    /// </summary>
    internal class Win32MouseInput : IMouseInput
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private static readonly IntPtr HardwareExtraInfo = (IntPtr)0x4B49434D;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        public void SetCursorPosition(int x, int y)
        {
            if (!SetCursorPos(x, y))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"SetCursorPos failed for ({x}, {y}).");
            }
        }

        public void SoftwareClick(bool rightButton = false)
        {
            // 旧版 mouse_event API 没有可供检查的返回值。
            uint down = rightButton ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN;
            uint up = rightButton ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_LEFTUP;
            mouse_event(down | up, 0, 0, 0, 0);
        }

        public void HardwareClick(bool rightButton = false)
        {
            // dwExtraInfo 只是附加元数据；它不会把合成输入变成真实的硬件输入。
            SendMouseClick(HardwareExtraInfo, rightButton);
        }

        private static void SendMouseClick(IntPtr extraInfo, bool rightButton)
        {
            uint down = rightButton ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN;
            uint up = rightButton ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_LEFTUP;
            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dwFlags = down,
                        dwExtraInfo = extraInfo
                    }
                },
                new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dwFlags = up,
                        dwExtraInfo = extraInfo
                    }
                }
            };

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (sent == (uint)inputs.Length)
            {
                return;
            }

            int error = Marshal.GetLastWin32Error();

            // SendInput 会返回成功注入的事件数量。如果只接受了按下事件，
            // 则在报告失败前尽力尝试释放鼠标按键。
            if (sent == 1)
            {
                var release = new[]
                {
                    new INPUT
                    {
                        type = INPUT_MOUSE,
                        mi = new MOUSEINPUT
                        {
                            dwFlags = up,
                            dwExtraInfo = extraInfo
                        }
                    }
                };
                SendInput(1, release, Marshal.SizeOf<INPUT>());
            }

            throw new Win32Exception(error, $"SendInput accepted {sent} of {inputs.Length} mouse events.");
        }
    }
}
