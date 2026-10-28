using Xunit;

namespace MouseClicker.Tests;

public class HelpContentTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    [InlineData("zh-TW")]
    [InlineData("ja-JP")]
    [InlineData("ko-KR")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("ru-RU")]
    [InlineData("pt-BR")]
    public void EverySupportedLanguageHasCompleteDistinctFeatureGuides(string language)
    {
        var catalog = HelpContent.GetCatalog(language);
        Assert.False(string.IsNullOrWhiteSpace(catalog.SearchPlaceholder));
        Assert.False(string.IsNullOrWhiteSpace(catalog.SearchNoResults));
        Assert.Equal(Enum.GetValues<HelpTopic>().Order(), catalog.Pages.Select(page => page.Topic).Order());
        foreach (var page in catalog.Pages)
        {
            Assert.False(string.IsNullOrWhiteSpace(page.Title));
            Assert.False(string.IsNullOrWhiteSpace(page.Summary));
            Assert.True(page.Sections.Count >= 2);
            Assert.All(page.Sections, section =>
            {
                Assert.False(string.IsNullOrWhiteSpace(section.Heading));
                Assert.False(string.IsNullOrWhiteSpace(section.Body));
            });
            if (language != "en-US")
                Assert.NotEqual(HelpContent.GetCatalog("en-US").Pages.Single(item => item.Topic == page.Topic).Title, page.Title);
        }
    }

    [Fact]
    public void UnknownLanguageFallsBackToEnglish()
    {
        Assert.Same(HelpContent.GetCatalog("en-US"), HelpContent.GetCatalog("not-supported"));
    }

    [Fact]
    public void ChineseSetupExplainsSeparateAreasAndAllEightSteps()
    {
        var page = HelpContent.GetCatalog("zh-CN").Pages.Single(item => item.Topic == HelpTopic.ImageConditionSetup);
        var body = string.Join("\n", page.Sections.Select(section => section.Body));
        Assert.Contains("点击区域 =", body);
        Assert.Contains("检测区域 =", body);
        Assert.Contains("模板图片 =", body);
        Assert.Contains("不重叠", body);
        Assert.Contains("不点击鼠标", body);
        Assert.Contains("自动恢复", body);
        Assert.Contains("检测区外", body);
        foreach (int step in Enumerable.Range(1, 8)) Assert.Contains($"{step}.", body);
    }

    [Fact]
    public void CatalogAndSectionsCannotBeChangedByHelpWindows()
    {
        var catalog = HelpContent.GetCatalog("zh-CN");
        Assert.Throws<NotSupportedException>(() => ((IList<HelpPage>)catalog.Pages)[0] = catalog.Pages[0]);
        var page = catalog.Pages[0];
        Assert.Throws<NotSupportedException>(() => ((IList<HelpSection>)page.Sections)[0] = page.Sections[0]);
    }
}
