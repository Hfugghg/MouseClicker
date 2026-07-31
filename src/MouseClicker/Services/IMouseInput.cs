using System.Drawing;

namespace MouseClicker
{
    /// <summary>
    /// 鼠标输入抽象接口，便于测试时替换为假实现。
    /// </summary>
    public interface IMouseInput
    {
        void SetCursorPosition(int x, int y);
        void SoftwareClick();
        void HardwareClick();
    }
}
