using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;
using ListBox = System.Windows.Controls.ListBox;

namespace MouseClicker.Wpf;

internal sealed class HelpWindow : Window
{
    private HelpCatalog catalog;
    private readonly TextBox search = new();
    private readonly ListBox topics = new() { DisplayMemberPath = nameof(HelpPage.Title) };
    private readonly TextBox article = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(24), FontSize = 15, Background = System.Windows.Media.Brushes.White, VerticalContentAlignment = VerticalAlignment.Top };
    internal HelpTopic? SelectedTopic => (topics.SelectedItem as HelpPage)?.Topic;
    internal string ArticleText => article.Text;
    internal int VisibleTopicCount => topics.Items.Count;

    internal HelpWindow(HelpTopic topic = HelpTopic.QuickStart, Func<bool>? isRunning = null)
    {
        DialogTheme.Apply(this);
        catalog = HelpContent.GetCatalog(Localization.Language);
        Title = catalog.Title;
        Width = 980; Height = 660; MinWidth = 600; MinHeight = 380;
        var grid = new Grid { Margin = new Thickness(24) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        var sidebar = new DockPanel { Margin = new Thickness(0, 0, 20, 0) };
        search.Margin = new Thickness(0, 0, 0, 10);
        search.ToolTip = catalog.SearchPlaceholder;
        System.Windows.Automation.AutomationProperties.SetName(search, catalog.SearchPlaceholder);
        System.Windows.Automation.AutomationProperties.SetName(topics, catalog.NavigationTitle);
        System.Windows.Automation.AutomationProperties.SetName(article, catalog.Title);
        DockPanel.SetDock(search, Dock.Top);
        sidebar.Children.Add(search); sidebar.Children.Add(topics);
        grid.Children.Add(sidebar); Grid.SetColumn(article, 1); grid.Children.Add(article); Content = grid;
        search.TextChanged += (_, _) => FilterTopics();
        topics.SelectionChanged += (_, _) => RenderPage();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) { search.Focus(); search.SelectAll(); e.Handled = true; }
            if (e.Key == Key.Escape && !(isRunning?.Invoke() ?? false)) { Close(); e.Handled = true; }
        };
        ShowTopic(topic);
    }

    internal void SetSearch(string value) => search.Text = value;
    internal void ReloadCatalog(HelpCatalog updated)
    {
        var selected = SelectedTopic ?? HelpTopic.QuickStart;
        catalog = updated; Title = catalog.Title; search.ToolTip = catalog.SearchPlaceholder;
        System.Windows.Automation.AutomationProperties.SetName(search, catalog.SearchPlaceholder);
        System.Windows.Automation.AutomationProperties.SetName(topics, catalog.NavigationTitle);
        System.Windows.Automation.AutomationProperties.SetName(article, catalog.Title);
        ShowTopic(selected);
    }
    internal void ShowTopic(HelpTopic topic)
    {
        search.Clear(); FilterTopics();
        topics.SelectedItem = topics.Items.Cast<HelpPage>().FirstOrDefault(page => page.Topic == topic);
    }
    private void FilterTopics()
    {
        var previous = SelectedTopic;
        var query = search.Text.Trim();
        topics.Items.Clear();
        foreach (var page in catalog.Pages)
        {
            var text = page.Title + page.Summary + string.Join("\n", page.Sections.Select(section => section.Heading + section.Body));
            if (text.Contains(query, StringComparison.CurrentCultureIgnoreCase)) topics.Items.Add(page);
        }
        topics.SelectedItem = topics.Items.Cast<HelpPage>().FirstOrDefault(page => page.Topic == previous);
        if (topics.SelectedIndex < 0 && topics.Items.Count > 0) topics.SelectedIndex = 0;
        RenderPage();
    }
    private void RenderPage()
    {
        article.Text = topics.SelectedItem is HelpPage page
            ? page.Title + "\n\n" + page.Summary + "\n\n" + string.Join("\n\n", page.Sections.Select(section => section.Heading + "\n" + section.Body))
            : catalog.SearchNoResults;
        article.ScrollToHome();
    }
}

