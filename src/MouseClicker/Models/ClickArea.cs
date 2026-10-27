using System.Drawing;

namespace MouseClicker
{
    public class ClickArea
    {
        public Rectangle Area { get; set; }
        public string Label { get; set; } = string.Empty;

        // 旧版 JSON 配置缺少此属性时默认为 false（鼠标左键）。
        public bool RightButton { get; set; }

        public override string ToString()
        {
            string button = RightButton ? "R" : "L";
            return $"[{button}] '{Label}' @ (X:{Area.X}, Y:{Area.Y}, W:{Area.Width}, H:{Area.Height})";
        }
    }
}
