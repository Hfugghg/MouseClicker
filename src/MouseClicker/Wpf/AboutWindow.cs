using System.Windows;
using System.Windows.Controls;

namespace MouseClicker.Wpf;

internal sealed class AboutWindow : Window
{
    private readonly MainWindow source;
    private readonly TextBlock status = DialogLayout.Text(string.Empty);
    private readonly System.Windows.Controls.Button release;
    private readonly System.Windows.Controls.Button skip;

    internal AboutWindow(MainWindow source)
    {
        this.source = source;
        var panel = DialogLayout.Initialize(this, Localization.Get("About"));
        panel.Children.Add(DialogLayout.Text(string.Format(Localization.Get("AboutMessage"), source.DisplayVersion).TrimEnd()));
        panel.Children.Add(DialogLayout.Text(Localization.Get("ProjectAddress")));
        panel.Children.Add(DialogLayout.Button(GitHubUpdateService.ProjectUrl.AbsoluteUri, () => source.OpenExternalLink(GitHubUpdateService.ProjectUrl)));
        panel.Children.Add(status);
        release = DialogLayout.Button(Localization.Get("ViewUpdate"), () => { if (source.AvailableUpdate is { } update) source.OpenExternalLink(update.ReleaseUrl); });
        skip = DialogLayout.Button(Localization.Get("SkipThisVersion"), () => source.SkipAvailableUpdate());
        panel.Children.Add(release); panel.Children.Add(skip);
        panel.Children.Add(DialogLayout.Button(Localization.Get("CloseButton"), Close, true));
        source.UpdateStateChanged += UpdateStateChanged;
        Closed += (_, _) => source.UpdateStateChanged -= UpdateStateChanged;
        Refresh();
    }
    private void UpdateStateChanged(object? sender, EventArgs e) => Refresh();
    private static string Format(Version version) => version.ToString(version.Revision > 0 ? 4 : 3);
    private void Refresh()
    {
        status.Text = source.CheckingForUpdates ? Localization.Get("CheckingForUpdates")
            : source.AvailableUpdate is { } update ? string.Format(Localization.Get("UpdateAvailableVersion"), Format(update.Version))
            : source.SkippedUpdateVersion is { } skipped ? string.Format(Localization.Get("UpdateSkipped"), Format(skipped))
            : Localization.Get(source.UpdateCheckSucceeded ? "NoUpdateAvailable" : "UpdateCheckUnavailable");
        release.Visibility = skip.Visibility = source.AvailableUpdate == null ? Visibility.Collapsed : Visibility.Visible;
    }
}
