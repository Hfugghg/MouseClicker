using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MouseClicker
{
    public class OverlayForm : Form
    {
        private const int WS_EX_TRANSPARENT = 0x20;

        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public List<ClickArea> AreasToDraw { get; set; } = new List<ClickArea>();

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = this.BackColor;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (AreasToDraw == null) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var area in AreasToDraw)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, area.Area);
                }
                using (var font = new Font("Arial", 16, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(150, 255, 255, 0)))
                {
                    var textSize = e.Graphics.MeasureString(area.Label, font);
                    var textX = area.Area.X + (area.Area.Width - textSize.Width) / 2;
                    var textY = area.Area.Y + (area.Area.Height - textSize.Height) / 2;
                    e.Graphics.DrawString(area.Label, font, brush, textX, textY);
                }
            }
        }
    }
}
