using System.Collections.Generic;
using MouseClicker;

namespace MouseClicker.Tests.TestDoubles
{
    /// <summary>
    /// 用于测试的假鼠标输入实现，记录所有调用但不操作真实硬件。
    /// </summary>
    internal class FakeMouseInput : IMouseInput
    {
        /// <summary>记录所有 SetCursorPosition 调用的坐标。</summary>
        public List<(int X, int Y)> CursorPositions { get; } = new();

        /// <summary>记录软件点击次数。</summary>
        public int SoftwareClickCount { get; private set; }

        /// <summary>记录硬件点击次数。</summary>
        public int HardwareClickCount { get; private set; }

        public List<bool> SoftwareClickButtons { get; } = new();
        public List<bool> HardwareClickButtons { get; } = new();

        /// <summary>总点击次数（软件 + 硬件）。</summary>
        public int TotalClickCount => SoftwareClickCount + HardwareClickCount;

        public void SetCursorPosition(int x, int y)
        {
            CursorPositions.Add((x, y));
        }

        public void SoftwareClick(bool rightButton = false)
        {
            SoftwareClickCount++;
            SoftwareClickButtons.Add(rightButton);
        }

        public void HardwareClick(bool rightButton = false)
        {
            HardwareClickCount++;
            HardwareClickButtons.Add(rightButton);
        }

        /// <summary>重置所有记录。</summary>
        public void Reset()
        {
            CursorPositions.Clear();
            SoftwareClickCount = 0;
            HardwareClickCount = 0;
            SoftwareClickButtons.Clear();
            HardwareClickButtons.Clear();
        }
    }
}
