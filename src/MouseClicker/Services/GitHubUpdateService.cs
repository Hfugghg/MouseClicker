using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MouseClicker
{
    internal sealed class GitHubUpdateService
    {
        public static readonly Uri ProjectUrl = new("https://github.com/Hfugghg/MouseClicker");
        public static readonly Uri ReleasesUrl = new("https://github.com/Hfugghg/MouseClicker/releases");
        private static readonly Uri LatestReleaseApi = new("https://api.github.com/repos/Hfugghg/MouseClicker/releases/latest");
        private readonly HttpClient _httpClient;

        public GitHubUpdateService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default)
        {
            Version current = UpdateVersion.Normalize(currentVersion);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MouseClicker", current.ToString()));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new UpdateCheckResult(false, null);

#if NET48
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#endif
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement release = document.RootElement;
                if (release.ValueKind != JsonValueKind.Object ||
                    !release.TryGetProperty("draft", out JsonElement draft) ||
                    draft.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !release.TryGetProperty("prerelease", out JsonElement prerelease) ||
                    prerelease.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return new UpdateCheckResult(false, null);
                }
                if (draft.GetBoolean() || prerelease.GetBoolean()) return new UpdateCheckResult(true, null);

                if (!release.TryGetProperty("tag_name", out JsonElement tag) || tag.ValueKind != JsonValueKind.String ||
                    !UpdateVersion.TryParseStable(tag.GetString(), out Version? version) ||
                    !release.TryGetProperty("html_url", out JsonElement url) || url.ValueKind != JsonValueKind.String ||
                    !Uri.TryCreate(url.GetString(), UriKind.Absolute, out Uri? releaseUrl) || !IsProjectReleaseUrl(releaseUrl))
                {
                    return new UpdateCheckResult(false, null);
                }

                return new UpdateCheckResult(true, version > current ? new UpdateInfo(version, releaseUrl) : null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new UpdateCheckResult(false, null);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException)
            {
                return new UpdateCheckResult(false, null);
            }
        }

        private static bool IsProjectReleaseUrl(Uri url)
        {
            return url.Scheme == Uri.UriSchemeHttps &&
                url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
                url.IsDefaultPort && string.IsNullOrEmpty(url.UserInfo) &&
                url.AbsolutePath.StartsWith("/Hfugghg/MouseClicker/releases/", StringComparison.OrdinalIgnoreCase) &&
                url.AbsolutePath.Length > "/Hfugghg/MouseClicker/releases/".Length;
        }
    }
}
