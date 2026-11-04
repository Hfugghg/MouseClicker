using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace MouseClicker.Wpf;

/// <summary>统一对话框的间距与可换行文本。</summary>
internal static class DialogLayout
{
    internal static StackPanel Initialize(Window window, string title, double width = 540)
    {
        DialogTheme.Apply(window);
        window.Title = title;
        window.Width = width;
        window.SizeToContent = SizeToContent.Height;
        window.MaxHeight = SystemParameters.WorkArea.Height;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.ShowInTaskbar = false;
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 24, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 20) });
        window.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return panel;
    }

    internal static TextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 8) };
    internal static Button Button(string text, Action action, bool cancel = false)
    {
        var button = new Button { Content = text, Padding = new Thickness(16, 9, 16, 9), Margin = new Thickness(0, 4, 8, 4), IsCancel = cancel };
        AutomationProperties.SetName(button, text);
        button.Click += (_, _) => action();
        return button;
    }
}

