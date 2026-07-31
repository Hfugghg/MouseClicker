using System.Drawing;

namespace MouseClicker
{
    public class ClickArea
    {
        public Rectangle Area { get; set; }
        public string Label { get; set; } = string.Empty;
        public override string ToString()
        {
            return $"'{Label}' @ (X:{Area.X}, Y:{Area.Y}, W:{Area.Width}, H:{Area.Height})";
        }
    }
}
