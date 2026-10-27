using System.Drawing;

namespace MouseClicker
{
    public class ClickArea
    {
        public Rectangle Area { get; set; }
        public string Label { get; set; } = string.Empty;

        // Missing property in older JSON configurations defaults to false (left button).
        public bool RightButton { get; set; }

        public override string ToString()
        {
            string button = RightButton ? "R" : "L";
            return $"[{button}] '{Label}' @ (X:{Area.X}, Y:{Area.Y}, W:{Area.Width}, H:{Area.Height})";
        }
    }
}
