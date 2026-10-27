namespace MouseClicker
{
    internal sealed record UpdateInfo(Version Version, Uri ReleaseUrl);
    internal sealed record UpdateCheckResult(bool Succeeded, UpdateInfo? Update);
}
