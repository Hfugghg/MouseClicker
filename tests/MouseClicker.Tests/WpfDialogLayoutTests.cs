using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseClicker.Wpf;
using Xunit;
using CheckBox = System.Windows.Controls.CheckBox;

namespace MouseClicker.Tests;

public class WpfDialogLayoutTests
{
    [Theory]
    [InlineData("es-ES")]
    [InlineData("pt-BR")]
    [InlineData("de-DE")]
    public void ConditionOptionsWrapWithinMinimumWindowWidth(string language) => WpfTestHost.Run(() =>
    {
        string previous = Localization.Language;
        Localization.Language = language;
        var window = new ConditionSettingsWindow(null) { Width = 440, Height = 380 };
        try
        {
            // 按最小窗口的客户区尺寸真实排版，验证长翻译不会被复选框裁切。
            var content = (FrameworkElement)window.Content;
            content.Measure(new System.Windows.Size(424, 340));
            content.Arrange(new Rect(0, 0, 424, 340));
            content.UpdateLayout();
            foreach (string name in new[] { "enabled", "multiScale" })
            {
                var option = (CheckBox)typeof(ConditionSettingsWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
                var label = Assert.IsType<TextBlock>(option.Content);
                Assert.True(label.ActualWidth > 30);
                var bounds = label.TransformToAncestor(content).TransformBounds(new Rect(label.RenderSize));
                Assert.True(bounds.Left >= 0 && bounds.Right <= content.ActualWidth + 1, $"{language}: {name} 标签超出客户区。");
                Assert.True(label.DesiredSize.Width <= label.ActualWidth + 1);
                Assert.True(label.DesiredSize.Height <= label.ActualHeight + 1);
                // 直接用实际字号测量排版结果，防止模板虽然有边界却仍将单行内容截断。
                var text = new FormattedText(label.Text, System.Globalization.CultureInfo.CurrentCulture,
                    label.FlowDirection, new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
                    label.FontSize, label.Foreground, VisualTreeHelper.GetDpi(label).PixelsPerDip)
                { MaxTextWidth = label.ActualWidth };
                Assert.True(text.Width <= label.ActualWidth + 1);
                Assert.True(text.Height <= label.ActualHeight + 1, $"{language}: {name} 缺少换行所需高度。");
            }
        }
        finally { window.Close(); Localization.Language = previous; }
    });
}
