using System;
using System.Runtime.InteropServices;

namespace MouseClicker
{
    /// <summary>
    /// 基于 Win32 API 的真实鼠标输入实现。
    /// </summary>
    internal class Win32MouseInput : IMouseInput
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_MOUSE = 0;
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
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
            SetCursorPos(x, y);
        }

        public void SoftwareClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public void HardwareClick()
        {
            var down = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dwFlags = MOUSEEVENTF_LEFTDOWN,
                    dwExtraInfo = HardwareExtraInfo
                }
            };
            var up = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dwFlags = MOUSEEVENTF_LEFTUP,
                    dwExtraInfo = HardwareExtraInfo
                }
            };
            SendInput(1, new[] { down }, Marshal.SizeOf<INPUT>());
            SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
        }
    }
}
