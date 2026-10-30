using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace MouseClicker.Tests;

public class HelpFormTests
{
    private static HelpCatalog Catalog(string title = "Guide") => new(title, "Topics", "Find a feature", "No matching topics", "Close", "Image condition guide",
    [
        new(HelpTopic.QuickStart, "Start", "Basics", [new("Steps", "Add a click region")]),
        new(HelpTopic.ImageConditionSetup, "Image setup", "Separate areas", [new("Template", "Find distinctive pixels")]),
        new(HelpTopic.ImageConditionParameters, "Threshold", "Settings", [new("Score", "Scores are not probabilities")])
    ]);

    [Fact]
    public void InitialTopicAndNavigationSelectRequestedPage() => Sta(() =>
    {
        using var form = new HelpForm(Catalog(), HelpTopic.ImageConditionSetup);
        Assert.Equal(HelpTopic.ImageConditionSetup, form.SelectedTopic);
        var list = (ListBox)typeof(HelpForm).GetField("topics", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        var article = (RichTextBox)typeof(HelpForm).GetField("article", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        Assert.Equal("Start", list.GetItemText(list.Items[0]));
        Assert.Equal("Image setup", article.AccessibleName);
        Assert.Contains("distinctive pixels", form.ArticleText);
        form.ShowTopic(HelpTopic.ImageConditionParameters);
        Assert.Contains("not probabilities", form.ArticleText);
        Assert.DoesNotContain("distinctive pixels", form.ArticleText);
    });

    [Fact]
    public void SearchMatchesBodyAndNoResultsClearsOldArticleThenRestores() => Sta(() =>
    {
        using var form = new HelpForm(Catalog());
        form.SetSearch("distinctive");
        Assert.Equal(1, form.VisibleTopicCount);
        Assert.Equal(HelpTopic.ImageConditionSetup, form.SelectedTopic);
        form.SetSearch("no-such-topic");
        Assert.Equal(0, form.VisibleTopicCount);
        Assert.Null(form.SelectedTopic);
        Assert.Equal("No matching topics", form.ArticleText);
        form.ShowTopic(HelpTopic.QuickStart);
        Assert.Equal(3, form.VisibleTopicCount);
        Assert.Contains("Add a click region", form.ArticleText);
        form.ReloadCatalog(Catalog("Updated guide"));
        Assert.Equal("Updated guide", form.Text);
        Assert.Equal(HelpTopic.QuickStart, form.SelectedTopic);
    });

    [Fact]
    public void ModelessHelpKeepsMainEnabledReusesInstanceAndDisposesWithOwner() => Sta(() =>
    {
        using var main = new Form1(null, null) { Opacity = 0, ShowInTaskbar = false };
        _ = main.Handle;
        int initialDepth = (int)typeof(Form1).GetField("_modalDepth", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
        main.ShowHelp(HelpTopic.QuickStart);
        var help = main.OpenHelpForm!;
        help.Opacity = 0;
        Assert.True(main.Enabled);
        Assert.False(help.Modal);
        Assert.Equal(initialDepth, typeof(Form1).GetField("_modalDepth", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main));
        main.ShowHelp(HelpTopic.ImageConditionSetup);
        Assert.Same(help, main.OpenHelpForm);
        Assert.Equal(HelpTopic.ImageConditionSetup, help.SelectedTopic);
        main.Dispose();
        Assert.True(help.IsDisposed);
    });

    [Fact]
    public void ConditionGuideRequestsSetupWithoutTemplate() => Sta(() =>
    {
        HelpTopic? requested = null;
        using var form = new ConditionSettingsForm(null, topic => requested = topic);
        _ = form.Handle;
        var guide = Assert.IsType<Button>(form.Controls.Find("conditionGuideButton", true).Single());
        // 直接调用点击处理器，不需要显示模态编辑窗口。
        typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(guide, [EventArgs.Empty]);
        Assert.Equal(HelpTopic.ImageConditionSetup, requested);
        Assert.Null(form.ResultCondition);
    });

    private static void Sta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
