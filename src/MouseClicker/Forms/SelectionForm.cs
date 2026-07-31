using System;
using System.Drawing;
using System.Windows.Forms;

namespace MouseClicker
{
    // 一个用于在屏幕上选择一个矩形区域的窗体。
    // 它表现为一个半透明的覆盖层，用户可以通过拖动鼠标来定义一个区域。
    public partial class SelectionForm : Form
    {
        private Point _startPoint; // 鼠标拖动的起始点
        private Rectangle _selectionRectangle; // 正在绘制的选择矩形
        private bool _isDrawing = false; // 标记是否正在拖动鼠标

        // 获取用户最终选择的矩形区域。
        public Rectangle SelectedArea { get; private set; }

        public SelectionForm()
        {
            // --- 初始化窗口属性，使其成为一个半透明的覆盖层 ---
            this.WindowState = FormWindowState.Maximized; // 最大化以覆盖整个屏幕
            this.FormBorderStyle = FormBorderStyle.None;  // 无边框
            this.BackColor = Color.Black;                 // 背景色设为黑色
            this.Opacity = 0.3;                           // 30% 的不透明度，产生“变暗”效果
            this.Cursor = Cursors.Cross;                  // 设置光标为十字形
            this.TopMost = true;                          // 确保窗口在最顶层

            // 启用双缓冲以减少绘制时的闪烁
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        // 当鼠标按下时触发，开始绘制选择框。
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // 仅当按下鼠标左键时响应
            if (e.Button == MouseButtons.Left)
            {
                _startPoint = e.Location; // 记录起始点
                _isDrawing = true;
            }
        }

        // 当鼠标移动时触发，更新选择框的大小。
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDrawing)
            {
                // 根据起始点和当前鼠标位置计算矩形的左上角坐标、宽度和高度
                int x = Math.Min(_startPoint.X, e.X);
                int y = Math.Min(_startPoint.Y, e.Y);
                int width = Math.Abs(_startPoint.X - e.X);
                int height = Math.Abs(_startPoint.Y - e.Y);
                _selectionRectangle = new Rectangle(x, y, width, height);

                // 使窗口的当前区域无效，并触发重绘消息（调用OnPaint）
                this.Invalidate();
            }
        }

        // 当鼠标松开时触发，完成选择。
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDrawing = false;
                // 保存最终选择的矩形区域
                SelectedArea = _selectionRectangle;
                // 将对话框结果设置为OK，通知调用方选择已完成
                this.DialogResult = DialogResult.OK;
                this.Close(); // 关闭选择窗口
            }
        }

        // 绘制选择框。
        protected override void OnPaint(PaintEventArgs e)
        {
            // 仅在矩形有效时（宽度和高度都大于0）才进行绘制
            if (_selectionRectangle.Width > 0 && _selectionRectangle.Height > 0)
            {
                // 使用半透明的蓝色画刷填充矩形内部，使其在暗色背景上突出显示
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 72, 145, 220)))
                {
                    e.Graphics.FillRectangle(brush, _selectionRectangle);
                }
                // 使用白色的笔绘制矩形边框
                using (Pen pen = new Pen(Color.White, 1))
                {
                    e.Graphics.DrawRectangle(pen, _selectionRectangle);
                }
            }
        }
    }
}
