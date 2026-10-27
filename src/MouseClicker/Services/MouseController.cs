using System;
using System.Drawing;

namespace MouseClicker
{
    /// <summary>
    /// 鼠标点击控制器，在指定区域内执行随机位置的点击。
    /// </summary>
    internal class MouseController
    {
        private readonly IMouseInput _mouseInput;
        private readonly Random _random = new();

        public MouseController(IMouseInput mouseInput)
        {
            _mouseInput = mouseInput ?? throw new ArgumentNullException(nameof(mouseInput));
        }

        /// <param name="hardwareMode">true 使用硬件模拟点击，false 使用软件点击</param>
        public void PerformRandomClickInArea(Rectangle area, bool hardwareMode = false, bool rightButton = false)
        {
            if (area.Width <= 0 || area.Height <= 0) return;
            int randomX = _random.Next(area.Left, area.Right);
            int randomY = _random.Next(area.Top, area.Bottom);
            _mouseInput.SetCursorPosition(randomX, randomY);

            if (hardwareMode)
                _mouseInput.HardwareClick(rightButton);
            else
                _mouseInput.SoftwareClick(rightButton);
        }
    }
}
