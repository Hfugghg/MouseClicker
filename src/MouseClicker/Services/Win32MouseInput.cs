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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
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

        public void SoftwareClick()
        {
            SendMouseClick(IntPtr.Zero);
        }

        public void HardwareClick()
        {
            // dwExtraInfo is metadata only; it does not make synthetic input physical hardware input.
            SendMouseClick(HardwareExtraInfo);
        }

        private static void SendMouseClick(IntPtr extraInfo)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSEEVENTF_LEFTDOWN,
                        dwExtraInfo = extraInfo
                    }
                },
                new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSEEVENTF_LEFTUP,
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

            // SendInput reports a prefix count. If only the button-down event was accepted,
            // make a best-effort attempt to release the button before reporting failure.
            if (sent == 1)
            {
                var release = new[]
                {
                    new INPUT
                    {
                        type = INPUT_MOUSE,
                        mi = new MOUSEINPUT
                        {
                            dwFlags = MOUSEEVENTF_LEFTUP,
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
