namespace MouseClicker;

internal sealed class HelpForm : Form
{
    private HelpCatalog catalog;
    private readonly TextBox search = new() { Dock = DockStyle.Fill };
    private readonly ListBox topics = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawVariable };
    private readonly RichTextBox article = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, DetectUrls = false, WordWrap = true, ScrollBars = RichTextBoxScrollBars.Vertical };
    private readonly Font headingFont;
    private readonly Font summaryFont;
    private readonly Font sectionFont;
    private readonly Func<bool> isRunning;
    internal HelpTopic? SelectedTopic => (topics.SelectedItem as HelpPage)?.Topic;
    internal string ArticleText => article.Text;
    internal int VisibleTopicCount => topics.Items.Count;

    internal HelpForm(HelpTopic initialTopic = HelpTopic.QuickStart) : this(HelpContent.GetCatalog(Localization.Language), initialTopic) { }

    internal HelpForm(HelpCatalog catalog, HelpTopic initialTopic = HelpTopic.QuickStart, Func<bool>? isRunning = null)
    {
        this.catalog = catalog;
        this.isRunning = isRunning ?? (() => false);
        Text = catalog.Title;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(980, 660);
        MinimumSize = new Size(760, 520);
        BackColor = Color.FromArgb(243, 246, 250);
        Font = new Font("Microsoft YaHei UI", 10);
        headingFont = new Font(Font.FontFamily, 21, FontStyle.Bold);
        summaryFont = new Font(Font.FontFamily, 10.5f);
        sectionFont = new Font(Font.FontFamily, 12, FontStyle.Bold);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(16) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 0, 14, 0) };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.Controls.Add(new Label { Text = catalog.NavigationTitle, Dock = DockStyle.Fill, Font = sectionFont, AutoEllipsis = true }, 0, 0);
        search.PlaceholderText = catalog.SearchPlaceholder;
        search.AccessibleName = catalog.SearchPlaceholder;
        search.Margin = new Padding(0, 0, 0, 12);
        sidebar.Controls.Add(search, 0, 1);
        topics.BackColor = BackColor;
        topics.DisplayMember = nameof(HelpPage.Title);
        topics.AccessibleName = catalog.NavigationTitle;
        topics.MeasureItem += (_, e) => e.ItemHeight = LogicalToDeviceUnits(52);
        topics.DrawItem += DrawTopic;
        topics.SelectedIndexChanged += (_, _) => RenderPage(topics.SelectedItem as HelpPage);
        sidebar.Controls.Add(topics, 0, 2);
        var readingPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(26, 22, 22, 22) };
        readingPanel.Controls.Add(article);
        article.AccessibleName = catalog.Title;
        layout.Controls.Add(sidebar, 0, 0);
        layout.Controls.Add(readingPanel, 1, 0);
        Controls.Add(layout);
        search.TextChanged += (_, _) => FilterTopics();
        ShowTopic(initialTopic);
    }

    private void DrawTopic(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var page = (HelpPage)topics.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? Color.FromArgb(221, 234, 251) : BackColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(34, 98, 172));
            e.Graphics.FillRectangle(accent, new Rectangle(e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height));
        }
        var bounds = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 9, e.Bounds.Width - 22, e.Bounds.Height - 16);
        TextRenderer.DrawText(e.Graphics, page.Title, Font, bounds, selected ? Color.FromArgb(20, 70, 130) : Color.FromArgb(45, 55, 70), TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }

    internal void SetSearch(string value) => search.Text = value;

    internal void ReloadCatalog(HelpCatalog updated)
    {
        if (IsDisposed) return;
        var selected = SelectedTopic ?? HelpTopic.QuickStart;
        catalog = updated;
        Text = catalog.Title;
        search.PlaceholderText = catalog.SearchPlaceholder;
        search.AccessibleName = catalog.SearchPlaceholder;
        topics.AccessibleName = catalog.NavigationTitle;
        if (search.Parent is TableLayoutPanel sidebar && sidebar.GetControlFromPosition(0, 0) is Label label) label.Text = catalog.NavigationTitle;
        ShowTopic(selected);
    }

    internal void ShowTopic(HelpTopic topic)
    {
        search.Clear();
        FilterTopics();
        for (int i = 0; i < topics.Items.Count; i++)
            if (((HelpPage)topics.Items[i]).Topic == topic) { topics.SelectedIndex = i; return; }
    }

    private void FilterTopics()
    {
        var previous = SelectedTopic;
        string query = search.Text.Trim();
        topics.BeginUpdate();
        try
        {
            topics.Items.Clear();
            foreach (var page in catalog.Pages)
            {
                string searchable = page.Title + "\n" + page.Summary + "\n" + string.Join("\n", page.Sections.Select(s => s.Heading + "\n" + s.Body));
                if (query.Length == 0 || searchable.Contains(query, StringComparison.CurrentCultureIgnoreCase)) topics.Items.Add(page);
            }
            int selection = -1;
            for (int i = 0; i < topics.Items.Count; i++) if (((HelpPage)topics.Items[i]).Topic == previous) selection = i;
            topics.SelectedIndex = selection >= 0 ? selection : topics.Items.Count > 0 ? 0 : -1;
        }
        finally { topics.EndUpdate(); }
        RenderPage(topics.SelectedItem as HelpPage);
    }

    private void RenderPage(HelpPage? page)
    {
        article.AccessibleName = page?.Title ?? catalog.Title;
        article.Clear();
        if (page == null) { Append(catalog.SearchNoResults, sectionFont, Color.DimGray); return; }
        Append(page.Title + "\n", headingFont, Color.FromArgb(28, 45, 65));
        Append(page.Summary + "\n\n", summaryFont, Color.FromArgb(80, 95, 110));
        foreach (var section in page.Sections)
        {
            Append(section.Heading + "\n", sectionFont, Color.FromArgb(34, 98, 172));
            Append(section.Body + "\n\n", Font, Color.FromArgb(35, 43, 52));
        }
        article.SelectionStart = 0;
        article.SelectionLength = 0;
        article.ScrollToCaret();
    }

    private void Append(string text, Font font, Color color)
    {
        article.SelectionStart = article.TextLength;
        article.SelectionLength = 0;
        article.SelectionFont = font;
        article.SelectionColor = color;
        article.AppendText(text);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F)) { search.Focus(); search.SelectAll(); return true; }
        if (keyData == Keys.Escape && !isRunning()) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { headingFont.Dispose(); summaryFont.Dispose(); sectionFont.Dispose(); }
    }
}
