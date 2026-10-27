using System.Drawing;
using System.Windows.Forms;

namespace MouseClicker
{
    internal sealed class AboutForm : Form
    {
        private readonly Form1 _source;
        private readonly Label versionLabel = new() { AutoSize = true };
        private readonly Label projectAddressLabel = new() { AutoSize = true };
        private readonly LinkLabel projectLinkLabel = new() { AutoSize = true };
        private readonly Label updateStatusLabel = new() { AutoSize = true };
        private readonly LinkLabel releaseLinkLabel = new() { AutoSize = true };
        private readonly Button skipVersionButton = new() { AutoSize = true };
        private readonly Button closeButton = new() { AutoSize = true, DialogResult = DialogResult.Cancel };

        internal AboutForm(Form1 source)
        {
            _source = source;
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Icon = source.Icon;
            Controls.AddRange(new Control[]
            {
                versionLabel, projectAddressLabel, projectLinkLabel, updateStatusLabel,
                releaseLinkLabel, skipVersionButton, closeButton
            });
            projectLinkLabel.LinkClicked += (_, _) =>
                Form1.OpenExternalLink(GitHubUpdateService.ProjectUrl, this);
            releaseLinkLabel.LinkClicked += (_, _) =>
            {
                if (_source.AvailableUpdate is { } update)
                    Form1.OpenExternalLink(update.ReleaseUrl, this);
            };
            skipVersionButton.Click += (_, _) => _source.SkipAvailableUpdate(this);
            closeButton.Click += (_, _) => Close();
            CancelButton = closeButton;
            _source.UpdateStateChanged += Source_UpdateStateChanged;
            UpdateContent();
        }

        private void Source_UpdateStateChanged(object? sender, EventArgs e)
        {
            if (!IsDisposed && !Disposing) UpdateContent();
        }

        private void UpdateContent()
        {
            Text = Localization.Get("About");
            versionLabel.Text = string.Format(Localization.Get("AboutMessage"), _source.DisplayVersion).TrimEnd();
            projectAddressLabel.Text = Localization.Get("ProjectAddress");
            projectLinkLabel.Text = GitHubUpdateService.ProjectUrl.AbsoluteUri;
            releaseLinkLabel.Text = Localization.Get("ViewUpdate");
            skipVersionButton.Text = Localization.Get("SkipThisVersion");
            closeButton.Text = Localization.Get("CloseButton");

            var update = _source.AvailableUpdate;
            bool hasUpdate = update != null;
            updateStatusLabel.Text = _source.CheckingForUpdates
                ? Localization.Get("CheckingForUpdates")
                : hasUpdate
                    ? string.Format(Localization.Get("UpdateAvailableVersion"), FormatVersion(update!.Version))
                    : _source.SkippedUpdateVersion is { } skipped
                        ? string.Format(Localization.Get("UpdateSkipped"), FormatVersion(skipped))
                        : Localization.Get(_source.UpdateCheckSucceeded ? "NoUpdateAvailable" : "UpdateCheckUnavailable");
            releaseLinkLabel.Visible = hasUpdate;
            skipVersionButton.Visible = hasUpdate;
            LayoutContent(hasUpdate);
        }

        private static string FormatVersion(Version version) =>
            version.ToString(version.Revision > 0 ? 4 : 3);

        private void LayoutContent(bool hasUpdate)
        {
            const int padding = 16;
            int closeWidth = Math.Max(90, closeButton.GetPreferredSize(Size.Empty).Width + 16);
            int skipWidth = Math.Max(120, skipVersionButton.GetPreferredSize(Size.Empty).Width + 16);
            int width = Math.Max(520, closeWidth + skipWidth + padding * 3);
            int y = padding;
            foreach (var label in new Label[] { versionLabel, projectAddressLabel, projectLinkLabel, updateStatusLabel })
            {
                label.MaximumSize = new Size(width - padding * 2, 0);
                label.Location = new Point(padding, y);
                label.Size = label.GetPreferredSize(new Size(width - padding * 2, 0));
                y = label.Bottom + 12;
            }
            if (hasUpdate)
            {
                releaseLinkLabel.MaximumSize = new Size(width - padding * 2, 0);
                releaseLinkLabel.Location = new Point(padding, y);
                releaseLinkLabel.Size = releaseLinkLabel.GetPreferredSize(new Size(width - padding * 2, 0));
                y = releaseLinkLabel.Bottom + padding;
            }
            int buttonHeight = Math.Max(32, Math.Max(closeButton.PreferredSize.Height, skipVersionButton.PreferredSize.Height));
            closeButton.SetBounds(width - padding - closeWidth, y, closeWidth, buttonHeight);
            skipVersionButton.SetBounds(closeButton.Left - padding - skipWidth, y, skipWidth, buttonHeight);
            ClientSize = new Size(width, closeButton.Bottom + padding);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _source.UpdateStateChanged -= Source_UpdateStateChanged;
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _source.UpdateStateChanged -= Source_UpdateStateChanged;
            base.Dispose(disposing);
        }
    }
}
