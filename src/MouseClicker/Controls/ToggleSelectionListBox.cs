using System.Drawing;
using System.Windows.Forms;

namespace MouseClicker
{
    internal sealed class ToggleSelectionListBox : ListBox
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONDBLCLK = 0x0203;

        internal event EventHandler? SelectionClearedByClick;

        protected override void WndProc(ref Message m)
        {
            int hitIndex = NoMatches;
            bool clearSelection = false;
            if (Enabled && SelectionMode == SelectionMode.One && m.Msg is WM_LBUTTONDOWN or WM_LBUTTONDBLCLK)
            {
                long coordinates = m.LParam.ToInt64();
                var point = new Point((short)(coordinates & 0xFFFF), (short)((coordinates >> 16) & 0xFFFF));
                if (ClientRectangle.Contains(point)) hitIndex = IndexFromPoint(point);
                // Native processing can change selection before managed MouseDown is raised.
                clearSelection = hitIndex != NoMatches && hitIndex == SelectedIndex;
            }

            base.WndProc(ref m);

            if (clearSelection && !IsDisposed && !Disposing && Enabled && SelectedIndex == hitIndex)
            {
                ClearSelected();
                SelectionClearedByClick?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
