using System.Drawing;
using System.Windows.Forms;

namespace MouseClicker;

public partial class Form1
{
    private ToolStripMenuItem settingsMenuItem = null!;
    private Button editConditionButton = null!;
    private Label areasTitle = null!, timingTitle = null!, displayTitle = null!;
    private Label areasHint = null!, emptyAreasLabel = null!, runHint = null!;
    private Panel areasCard = null!, timingCard = null!, displayCard = null!;
    private static readonly Color Accent = Color.FromArgb(37, 99, 185);
    private bool appearanceLayout;
    private Panel contentViewport = null!;
    private Size layoutWorkingAreaSize;
    private int layoutDpi;

    private void InitializeAppearance()
    {
        BackColor = Color.FromArgb(243, 245, 248);
        ForeColor = Color.FromArgb(32, 42, 57);
        menuStrip.BackColor = Color.White;
        menuStrip.Padding = new Padding(10, 5, 10, 5);
        areasCard = CreateCard();
        timingCard = CreateCard();
        displayCard = CreateCard();
        contentViewport = new Panel { AutoScroll = true, TabStop = false, TabIndex = 1 };
        Controls.Add(contentViewport);
        areasCard.Parent = timingCard.Parent = displayCard.Parent = contentViewport;
        areasCard.TabIndex = 0;
        timingCard.TabIndex = 1;
        displayCard.TabIndex = 2;
        startButton.TabIndex = 2;
        areasTitle = CreateHeading();
        timingTitle = CreateHeading();
        displayTitle = CreateHeading();
        areasHint = CreateHint();
        areasHint.AutoSize = false;
        jitterExceedsIntervalHintLabel.AutoSize = false;
        runHint = CreateHint();
        runHint.AutoSize = false;
        emptyAreasLabel = CreateHint();
        emptyAreasLabel.AutoSize = false;
        emptyAreasLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyAreasLabel.BackColor = Color.White;
        emptyAreasLabel.Cursor = Cursors.Hand;
        emptyAreasLabel.Click += (_, _) => addAreaButton.PerformClick();
        editConditionButton = new Button();
        Controls.Add(editConditionButton);
        editConditionButton.Click += (_, _) => EditSelectedCondition();
        foreach (var button in new[] { addAreaButton, removeAreaButton, moveUpButton, moveDownButton, editConditionButton, startButton })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(211, 218, 227);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 239, 249);
            button.BackColor = Color.White;
            button.Cursor = Cursors.Hand;
        }
        startButton.FlatAppearance.BorderSize = 0;
        startButton.ForeColor = Color.White;
        areasListBox.BorderStyle = BorderStyle.FixedSingle;
        areasListBox.IntegralHeight = false;
        areasListBox.HorizontalScrollbar = true;
        foreach (var control in new Control[] { intervalLabel, jitterLabel, sequenceLabel,
            showOverlayCheckBox, showDetectionOverlayCheckBox, highlightCurrentAreaCheckBox,
            hardwareModeCheckBox, jitterExceedsIntervalHintLabel }) control.BackColor = Color.White;
        foreach (var control in new Control[] { areasTitle, areasHint, areasListBox, emptyAreasLabel,
            addAreaButton, removeAreaButton, moveUpButton, moveDownButton, editConditionButton }) control.Parent = areasCard;
        foreach (var control in new Control[] { timingTitle, intervalLabel, intervalNumericUpDown, jitterLabel,
            jitterNumericUpDown, jitterExceedsIntervalHintLabel, sequenceLabel, sequenceComboBox, hardwareModeCheckBox }) control.Parent = timingCard;
        foreach (var control in new Control[] { displayTitle, showOverlayCheckBox, showDetectionOverlayCheckBox,
            highlightCurrentAreaCheckBox }) control.Parent = displayCard;
        emptyAreasLabel.BringToFront();
        int tabIndex = 0;
        foreach (var control in new Control[] { menuStrip, areasListBox, addAreaButton, removeAreaButton,
            moveUpButton, moveDownButton, editConditionButton, intervalNumericUpDown, jitterNumericUpDown,
            sequenceComboBox, hardwareModeCheckBox, showOverlayCheckBox, showDetectionOverlayCheckBox,
            highlightCurrentAreaCheckBox, startButton }) control.TabIndex = tabIndex++;
        startButton.TabIndex = 2;
        DpiChanged += (_, _) => PerformLayoutLayout();
        ResizeEnd += (_, _) =>
        {
            if (layoutWorkingAreaSize != Screen.FromControl(this).WorkingArea.Size || layoutDpi != DeviceDpi)
                PerformLayoutLayout();
        };
    }

    private Panel CreateCard()
    {
        var card = new Panel { BackColor = Color.White, TabStop = false };
        Controls.Add(card);
        card.SendToBack();
        return card;
    }

    private Label CreateHeading()
    {
        var label = new Label { AutoSize = true, BackColor = Color.White,
            Font = new Font(Font, FontStyle.Bold) };
        Controls.Add(label);
        return label;
    }

    private Label CreateHint()
    {
        var label = new Label { AutoSize = true, ForeColor = Color.FromArgb(96, 108, 125) };
        Controls.Add(label);
        return label;
    }

    private void UpdateAppearanceStrings()
    {
        settingsMenuItem.Text = Localization.Get("Settings");
        areasTitle.Text = Localization.Get("MainAreasTitle");
        timingTitle.Text = Localization.Get("MainTimingTitle");
        displayTitle.Text = Localization.Get("MainDisplayTitle");
        areasHint.Text = Localization.Get("MainAreasHint");
        areasHint.BackColor = Color.White;
        emptyAreasLabel.Text = Localization.Get("MainEmptyAreas");
        editConditionButton.Text = Localization.Get("ConditionEdit");
        runHint.Text = _hotkeyInitializationError != null ? string.Empty :
            string.Format(Localization.Get("MainRunHint"), _hotkeyManager?.Hotkey.ToString() ?? "F6");
    }

    private void RefreshAreaActions()
    {
        if (editConditionButton == null) return;
        int index = areasListBox.SelectedIndex;
        bool editable = areasListBox.Enabled && !_isClicking && _clickSession == null;
        void Enable(Control control, bool enabled) { if (control.Enabled != enabled) control.Enabled = enabled; }
        Enable(removeAreaButton, editable && index >= 0);
        Enable(editConditionButton, editable && index >= 0);
        Enable(moveUpButton, editable && index > 0);
        Enable(moveDownButton, editable && index >= 0 && index < areasListBox.Items.Count - 1);
        bool empty = areasListBox.Items.Count == 0;
        if (emptyAreasLabel.Visible != empty) emptyAreasLabel.Visible = empty;
        Enable(emptyAreasLabel, editable);
        Color background = _isClicking ? Color.FromArgb(180, 48, 48) : Accent;
        Color hover = _isClicking ? Color.FromArgb(155, 38, 38) : Color.FromArgb(28, 78, 150);
        if (startButton.BackColor != background) startButton.BackColor = background;
        if (startButton.FlatAppearance.MouseOverBackColor != hover) startButton.FlatAppearance.MouseOverBackColor = hover;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (areasTitle == null || timingTitle == null || displayTitle == null) return;
        foreach (var title in new[] { areasTitle, timingTitle, displayTitle })
        {
            Font previous = title.Font;
            title.Font = new Font(Font, FontStyle.Bold);
            previous.Dispose();
        }
        PerformLayoutLayout();
    }

    private bool jitterHintShown;

    private void TimingValueChanged(object? sender, EventArgs e)
    {
        if ((jitterNumericUpDown.Value > intervalNumericUpDown.Value) != jitterHintShown)
            PerformLayoutLayout();
    }

    private void LayoutAppearance()
    {
        if (areasCard == null || appearanceLayout) return;
        appearanceLayout = true;
        try
        {
            int S(int value) => (int)Math.Round(value * DeviceDpi / 96.0);
            int gap = S(6), pad = S(10), row = Math.Max(S(28), intervalNumericUpDown.PreferredSize.Height + S(4));
            int W(Control c) => c.PreferredSize.Width;
            int editWidth = Math.Max(S(160), TextRenderer.MeasureText(editConditionButton.Text, editConditionButton.Font).Width + S(24));
            int buttonWidth = new[] { addAreaButton, removeAreaButton, moveUpButton, moveDownButton }
                .Max(b => TextRenderer.MeasureText(b.Text, b.Font).Width + S(24));
            int width = Math.Max(S(500), buttonWidth * 4 + gap * 3 + pad * 4);
            width = Math.Max(width, W(intervalLabel) + W(jitterLabel) + S(220) + pad * 4 + gap * 3);
            width = Math.Max(width, new[] { showOverlayCheckBox, showDetectionOverlayCheckBox, highlightCurrentAreaCheckBox, hardwareModeCheckBox }.Max(W) + pad * 4);
            width = Math.Max(width, menuStrip.Items.Cast<ToolStripItem>().Sum(i => i.GetPreferredSize(Size.Empty).Width) + pad * 2);
            width = Math.Max(width, W(areasTitle) + editWidth + pad * 4 + gap);
            int inner = width - pad * 4;
            var bounds = new Dictionary<Control, Rectangle>();
            void Put(Control c, int x, int y, int w, int h) => bounds[c] = new Rectangle(x, y, w, h);
            void Natural(Control c, int x, int y) => Put(c, x, y, c.PreferredSize.Width, c.PreferredSize.Height);
            Size Wrapped(Label label, int w)
            {
                Size measured = TextRenderer.MeasureText(label.Text, label.Font,
                    new Size(Math.Max(1, w - S(4)), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                return new Size(w, measured.Height + S(2));
            }
            void Wrap(Label label, int x, int y, int w)
            {
                Size size = Wrapped(label, w);
                Put(label, x, y, w, size.Height);
            }
            bool badge = _availableUpdate != null;
            bool warning = jitterNumericUpDown.Value > intervalNumericUpDown.Value;
            int contentTop = menuStrip.Height + pad;
            if (badge)
            {
                Natural(updateAvailableLinkLabel, pad, contentTop);
                contentTop += bounds[updateAvailableLinkLabel].Height + gap;
            }
            int x = pad, y = pad;
            Natural(areasTitle, x, y);
            Put(editConditionButton, width - pad * 3 - editWidth, y - S(3), editWidth, row);
            y = Math.Max(bounds[areasTitle].Bottom, bounds[editConditionButton].Bottom) + gap;
            Wrap(areasHint, x, y, inner);
            y = bounds[areasHint].Bottom + gap;
            Put(areasListBox, x, y, inner, S(88));
            Put(emptyAreasLabel, x + 1, y + 1, inner - 2, S(86));
            y += S(88) + gap;
            int bw = (inner - gap * 3) / 4;
            var buttons = new[] { addAreaButton, removeAreaButton, moveUpButton, moveDownButton };
            for (int i = 0; i < buttons.Length; i++) Put(buttons[i], x + i * (bw + gap), y, bw, row);
            y += row + pad;
            Put(areasCard, pad, 0, width - pad * 2, y);
            int cardTop = y + gap;
            y = pad;
            Natural(timingTitle, x, y);
            y = bounds[timingTitle].Bottom + gap;
            Natural(intervalLabel, x, y + S(4));
            Put(intervalNumericUpDown, bounds[intervalLabel].Right + gap, y, S(94), intervalNumericUpDown.PreferredSize.Height);
            Natural(jitterLabel, bounds[intervalNumericUpDown].Right + pad, y + S(4));
            Put(jitterNumericUpDown, bounds[jitterLabel].Right + gap, y, S(94), jitterNumericUpDown.PreferredSize.Height);
            y += row + gap;
            Wrap(jitterExceedsIntervalHintLabel, x, y, inner);
            if (warning) y += bounds[jitterExceedsIntervalHintLabel].Height + gap;
            Natural(sequenceLabel, x, y + S(4));
            Put(sequenceComboBox, bounds[sequenceLabel].Right + gap, y, inner - W(sequenceLabel) - gap, sequenceComboBox.PreferredHeight);
            y += row + gap;
            Natural(hardwareModeCheckBox, x, y);
            y += hardwareModeCheckBox.PreferredSize.Height + pad;
            Put(timingCard, pad, cardTop, width - pad * 2, y);
            cardTop += y + gap;
            y = pad;
            Natural(displayTitle, x, y);
            y = bounds[displayTitle].Bottom + gap;
            int checkX = x, lineHeight = 0;
            foreach (var check in new[] { showOverlayCheckBox, showDetectionOverlayCheckBox, highlightCurrentAreaCheckBox })
            {
                if (checkX > x && checkX + W(check) > x + inner)
                { y += lineHeight + gap; checkX = x; lineHeight = 0; }
                Natural(check, checkX, y);
                checkX += W(check) + pad;
                lineHeight = Math.Max(lineHeight, check.PreferredSize.Height);
            }
            y += lineHeight + pad;
            Put(displayCard, pad, cardTop, width - pad * 2, y);
            int contentHeight = cardTop + y;
            Size hintSize = Wrapped(runHint, width - pad * 2);
            int maxClientHeight = Screen.FromControl(this).WorkingArea.Height - SizeFromClientSize(Size.Empty).Height - pad * 2;
            int footerHeight = pad + S(42) + gap + hintSize.Height + pad;
            int viewportHeight = Math.Min(contentHeight, Math.Max(S(80), maxClientHeight - contentTop - footerHeight));
            int scrollbar = viewportHeight < contentHeight ? SystemInformation.VerticalScrollBarWidth : 0;
            Put(contentViewport, 0, contentTop, width + scrollbar, viewportHeight);
            Put(startButton, pad, contentTop + viewportHeight + pad, width - pad * 2, S(42));
            Put(runHint, pad, bounds[startButton].Bottom + gap, hintSize.Width, hintSize.Height);
            Size client = new(width + scrollbar, bounds[runHint].Bottom + pad);
            Size scrollExtent = new(0, contentHeight);
            Point scroll = contentViewport.AutoScrollPosition;
            foreach (var card in new[] { areasCard, timingCard, displayCard })
            {
                Rectangle logical = bounds[card];
                logical.Offset(scroll);
                bounds[card] = logical;
            }
            bool changed = ClientSize != client || contentViewport.AutoScrollMinSize != scrollExtent ||
                bounds.Any(pair => pair.Key.Bounds != pair.Value) || jitterHintShown != warning ||
                updateAvailableLinkLabel.Visible != badge;
            if (changed)
            {
                Control[] containers = { this, contentViewport, areasCard, timingCard, displayCard };
                foreach (Control container in containers) container.SuspendLayout();
                try
                {
                    foreach (var label in new[] { areasHint, jitterExceedsIntervalHintLabel })
                        if (label.MaximumSize != new Size(inner, 0)) label.MaximumSize = new Size(inner, 0);
                    if (runHint.MaximumSize != new Size(width - pad * 2, 0)) runHint.MaximumSize = new Size(width - pad * 2, 0);
                    if (jitterHintShown != warning) jitterExceedsIntervalHintLabel.Visible = warning;
                    if (updateAvailableLinkLabel.Visible != badge) updateAvailableLinkLabel.Visible = badge;
                    foreach (var (control, rectangle) in bounds)
                        if (control.Bounds != rectangle) control.Bounds = rectangle;
                    if (sequenceComboBox.DropDownWidth != bounds[sequenceComboBox].Width) sequenceComboBox.DropDownWidth = bounds[sequenceComboBox].Width;
                    if (contentViewport.AutoScrollMinSize != scrollExtent) contentViewport.AutoScrollMinSize = scrollExtent;
                    if (ClientSize != client) ClientSize = client;
                }
                finally
                {
                    foreach (Control container in containers.Reverse()) container.ResumeLayout(true);
                }
                Point desired = new(Math.Max(0, -scroll.X), Math.Clamp(-scroll.Y, 0, Math.Max(0, contentHeight - viewportHeight)));
                if (contentViewport.AutoScrollPosition != new Point(-desired.X, -desired.Y)) contentViewport.AutoScrollPosition = desired;
            }
            jitterHintShown = warning;
            layoutWorkingAreaSize = Screen.FromControl(this).WorkingArea.Size;
            layoutDpi = DeviceDpi;
        }
        finally { appearanceLayout = false; }
    }
}
