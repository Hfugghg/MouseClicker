using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MouseClicker
{
    public class OverlayForm : Form
    {
        private const int WS_EX_TRANSPARENT = 0x20;
        private ClickArea? _previewArea;
        private ClickArea? _activeArea;
        internal static readonly Color PreviewColor = Color.DodgerBlue;
        internal static readonly Color ActiveColor = Color.LimeGreen;

        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public List<ClickArea> AreasToDraw { get; set; } = new List<ClickArea>();

        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal ClickArea? PreviewArea
        {
            get => _previewArea;
            set
            {
                if (ReferenceEquals(_previewArea, value)) return;
                _previewArea = value;
                Invalidate();
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal ClickArea? ActiveArea
        {
            get => _activeArea;
            set
            {
                if (ReferenceEquals(_activeArea, value)) return;
                _activeArea = value;
                Invalidate();
            }
        }

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            // 覆盖整个虚拟桌面，包括主显示器左侧或上方的显示器。
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = SystemInformation.VirtualScreen;
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

        internal static IEnumerable<ClickArea> GetDrawOrder(IEnumerable<ClickArea> areas, ClickArea? preview, ClickArea? active) =>
            areas.OrderBy(area => ReferenceEquals(area, active) ? 2 : ReferenceEquals(area, preview) ? 1 : 0);

        internal static Color GetBorderColor(ClickArea area, ClickArea? preview, ClickArea? active) =>
            ReferenceEquals(area, active) ? ActiveColor : ReferenceEquals(area, preview) ? PreviewColor : Color.Red;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (AreasToDraw == null) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var virtualScreen = SystemInformation.VirtualScreen;
            ClickArea? preview = PreviewArea;
            ClickArea? active = ActiveArea;
            foreach (var area in GetDrawOrder(AreasToDraw, preview, active))
            {
                // 绘制坐标相对于覆盖层窗口，而点击区域使用屏幕坐标。
                var localArea = new Rectangle(
                    area.Area.X - virtualScreen.Left,
                    area.Area.Y - virtualScreen.Top,
                    area.Area.Width,
                    area.Area.Height);
                Color borderColor = GetBorderColor(area, preview, active);
                bool highlighted = ReferenceEquals(area, active) || ReferenceEquals(area, preview);
                Color textColor = highlighted ? borderColor : Color.FromArgb(150, 255, 255, 0);
                using (var pen = new Pen(borderColor, highlighted ? 3 : 2))
                {
                    e.Graphics.DrawRectangle(pen, localArea);
                }
                using (var font = new Font("Arial", 16, FontStyle.Bold))
                using (var brush = new SolidBrush(textColor))
                {
                    var textSize = e.Graphics.MeasureString(area.Label, font);
                    var textX = localArea.X + (localArea.Width - textSize.Width) / 2;
                    var textY = localArea.Y + (localArea.Height - textSize.Height) / 2;
                    e.Graphics.DrawString(area.Label, font, brush, textX, textY);
                }
            }
        }
    }
}
