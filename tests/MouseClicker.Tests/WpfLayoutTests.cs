using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using MouseClicker.Wpf;
using Xunit;

namespace MouseClicker.Tests;

public class WpfLayoutTests
{
    public static IEnumerable<object[]> LanguagesAndSizes() =>
        Localization.SupportedLanguages.Keys.SelectMany(language => new[]
        {
            new object[] { language, 520d, 420d },
            new object[] { language, 560d, 640d },
            new object[] { language, 720d, 780d },
            new object[] { language, 980d, 780d }
        });

    public static IEnumerable<object[]> Languages() =>
        Localization.SupportedLanguages.Keys.Select(language => new object[] { language });

    [Fact]
    public void DefaultWindowKeepsCompactToolDimensions() => WpfTestHost.Run(() =>
    {
        var window = new MainWindow(null, enableHotkeys: false);
        try
        {
            Assert.InRange(window.Width, window.MinWidth, 600);
            Assert.InRange(window.Height, window.MinHeight, 700);
        }
        finally { window.Close(); }
    });

    [Theory]
    [MemberData(nameof(Languages))]
    public void DefaultSizeShowsAllSettingsWithoutScrolling(string language) => WpfTestHost.Run(() =>
    {
        string previous = Localization.Language;
        Localization.Language = language;
        var window = new MainWindow(null, enableHotkeys: false);
        try
        {
            // 区域列表允许内部滚动，但主页面须在有更新提醒时仍完整呈现。
            window.ApplyConfig(new ConfigData
            {
                Interval = 1000, Jitter = 100, SequenceIndex = 0,
                ClickAreas = Enumerable.Range(1, 3).Select(index => new ClickArea
                {
                    Label = "区域 " + index,
                    Area = new System.Drawing.Rectangle(index * 50, 20, 80, 40),
                    RightButton = index == 2
                }).ToList()
            });
            ((FrameworkElement)window.FindName("UpdateButton")).Visibility = Visibility.Visible;
            var content = (FrameworkElement)window.Content;
            content.Measure(new System.Windows.Size(window.Width - 16, window.Height - 40));
            content.Arrange(new Rect(0, 0, window.Width - 16, window.Height - 40));
            content.UpdateLayout();
            var scroll = Assert.IsType<ScrollViewer>(window.FindName("MainScroll"));
            Assert.True(scroll.ScrollableHeight <= 1, $"{language}: 默认主页面仍需滚动 {scroll.ScrollableHeight:F1} DIP。");
            var viewport = Descendants<ScrollContentPresenter>(scroll).First();
            foreach (string name in new[] { "IntervalInput", "JitterInput", "SequenceInput", "HardwareInput", "OverlayInput", "DetectionInput", "HighlightInput", "AddButton", "RemoveButton", "UpButton", "DownButton", "ConditionButton" })
            {
                var input = (FrameworkElement)window.FindName(name);
                var bounds = input.TransformToAncestor(viewport).TransformBounds(new Rect(input.RenderSize));
                Assert.True(input.Visibility == Visibility.Visible && bounds.Width > 30 && bounds.Height > 0);
                Assert.True(bounds.Top >= -1 && bounds.Bottom <= viewport.ActualHeight + 1, $"{language}: {name} 超出主页面可视高度。");
                Assert.True(bounds.Left >= -1 && bounds.Right <= viewport.ActualWidth + 1, $"{language}: {name} 超出主页面可视宽度。");
            }
        }
        finally { window.Close(); Localization.Language = previous; }
    });

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    [Theory]
    [MemberData(nameof(LanguagesAndSizes))]
    public void MainLayoutKeepsPrimaryActionAndInputsWithinWidth(string language, double width, double height) => WpfTestHost.Run(() =>
    {
        string previous = Localization.Language;
        Localization.Language = language;
        var window = new MainWindow(null, enableHotkeys: false);
        try
        {
            // 直接测量内容，不启动网络检查，也不显示覆盖层。
            var content = (FrameworkElement)window.Content;
            content.Measure(new System.Windows.Size(width - 16, height - 40));
            content.Arrange(new Rect(0, 0, width - 16, height - 40));
            content.UpdateLayout();
            var start = (FrameworkElement)window.FindName("StartButton");
            var startBounds = start.TransformToAncestor(content).TransformBounds(new Rect(start.RenderSize));
            Assert.True(startBounds.Width > 100);
            Assert.InRange(startBounds.Top, 0, content.ActualHeight);
            Assert.True(startBounds.Bottom <= content.ActualHeight + 1);
            foreach (string name in new[] { "AreasList", "IntervalInput", "JitterInput", "SequenceInput" })
            {
                var input = (FrameworkElement)window.FindName(name);
                Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(input)));
                var bounds = input.TransformToAncestor(content).TransformBounds(new Rect(input.RenderSize));
                Assert.True(bounds.Left >= 0 && bounds.Right <= content.ActualWidth + 1, $"{language}: {name} 超出可用宽度。");
                Assert.True(bounds.Width > 30);
            }
        }
        finally { window.Close(); Localization.Language = previous; }
    });
}

