using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MouseClicker.Tests
{
    public class GitHubUpdateServiceTests
    {
        private static readonly Version Current = new(1, 2, 1);
        private const string ReleaseUrl = "https://github.com/Hfugghg/MouseClicker/releases/tag/v1.3.0";

        [Fact]
        public async Task NewStableRelease_ReturnsNormalizedVersionAndSendsGitHubHeaders()
        {
            using var client = Client((request, _) =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://api.github.com/repos/Hfugghg/MouseClicker/releases/latest", request.RequestUri!.AbsoluteUri);
                Assert.Equal("MouseClicker/1.2.1.0", request.Headers.UserAgent.ToString());
                Assert.Contains(request.Headers.Accept, value => value.MediaType == "application/vnd.github+json");
                return Task.FromResult(Response(Release("v1.3.0")));
            });
            UpdateCheckResult result = await new GitHubUpdateService(client).CheckForUpdateAsync(Current);
            Assert.True(result.Succeeded);
            Assert.Equal(new Version(1, 3, 0, 0), result.Update!.Version);
            Assert.Equal(new Uri(ReleaseUrl), result.Update.ReleaseUrl);
        }

        [Theory]
        [InlineData("v1.2.1")]
        [InlineData("1.2.1.0")]
        [InlineData("v1.2.0")]
        public async Task SameOrOlderRelease_ReturnsSuccessfulNoUpdate(string tag)
        {
            using var client = Client((_, _) => Task.FromResult(Response(Release(tag))));
            Assert.Equal(new UpdateCheckResult(true, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task DraftOrPrerelease_IsIgnored(bool draft, bool prerelease)
        {
            using var client = Client((_, _) => Task.FromResult(Response(Release("v2.0.0-beta", draft: draft, prerelease: prerelease))));
            Assert.Equal(new UpdateCheckResult(true, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Theory]
        [InlineData(403)]
        [InlineData(404)]
        [InlineData(429)]
        [InlineData(500)]
        public async Task HttpFailure_IsNotReportedAsAlreadyUpToDate(int status)
        {
            using var client = Client((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
            Assert.Equal(new UpdateCheckResult(false, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Theory]
        [InlineData("{")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("{}")]
        [InlineData("{\"draft\":\"false\",\"prerelease\":false}")]
        [InlineData("{\"draft\":false,\"prerelease\":false,\"tag_name\":13,\"html_url\":null}")]
        public async Task MalformedPayload_ReturnsFailure(string json)
        {
            using var client = Client((_, _) => Task.FromResult(Response(json)));
            Assert.Equal(new UpdateCheckResult(false, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Theory]
        [InlineData("v1.3.0-beta")]
        [InlineData("v1.3.0+metadata")]
        [InlineData("release-1.3.0")]
        public async Task UnstableOrUnrecognizedTag_ReturnsFailure(string tag)
        {
            using var client = Client((_, _) => Task.FromResult(Response(Release(tag))));
            Assert.Equal(new UpdateCheckResult(false, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Theory]
        [InlineData("http://github.com/Hfugghg/MouseClicker/releases/tag/v1.3.0")]
        [InlineData("https://github.com.evil.example/Hfugghg/MouseClicker/releases/tag/v1.3.0")]
        [InlineData("https://evil.example/Hfugghg/MouseClicker/releases/tag/v1.3.0")]
        [InlineData("https://account@github.com/Hfugghg/MouseClicker/releases/tag/v1.3.0")]
        [InlineData("https://github.com:444/Hfugghg/MouseClicker/releases/tag/v1.3.0")]
        [InlineData("https://github.com/Other/MouseClicker/releases/tag/v1.3.0")]
        [InlineData("https://github.com/Hfugghg/Other/releases/tag/v1.3.0")]
        [InlineData("https://github.com/Hfugghg/MouseClicker/releases-evil/tag/v1.3.0")]
        [InlineData("https://github.com/Hfugghg/MouseClicker/releases/")]
        [InlineData("https://github.com/Hfugghg/MouseClicker/issues/1")]
        [InlineData("file:///C:/temp/update.exe")]
        [InlineData("/Hfugghg/MouseClicker/releases/tag/v1.3.0")]
        public async Task ReleaseLinkOutsideProjectHttpsReleases_IsRejected(string url)
        {
            using var client = Client((_, _) => Task.FromResult(Response(Release("v1.3.0", url))));
            Assert.Equal(new UpdateCheckResult(false, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Fact]
        public async Task NetworkFailure_ReturnsFailure()
        {
            using var client = Client((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")));
            Assert.Equal(new UpdateCheckResult(false, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Fact]
        public async Task ResponseReadFailure_ReturnsFailure()
        {
            using var client = Client((_, _) => Task.FromException<HttpResponseMessage>(new IOException("connection closed")));
            Assert.Equal(new UpdateCheckResult(false, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Fact]
        public async Task ConfiguredHttpTimeout_ReturnsFailure()
        {
            using var client = Client(async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                throw new InvalidOperationException("The timeout must cancel the request.");
            });
            client.Timeout = TimeSpan.FromMilliseconds(25);
            Assert.Equal(new UpdateCheckResult(false, null), await new GitHubUpdateService(client).CheckForUpdateAsync(Current));
        }

        [Fact]
        public async Task CallerCancellation_PropagatesAndStopsRequest()
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var client = Client(async (_, token) =>
            {
                entered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                throw new InvalidOperationException("The caller must cancel the request.");
            });
            using var cancellation = new CancellationTokenSource();
            Task<UpdateCheckResult> pending = new GitHubUpdateService(client).CheckForUpdateAsync(Current, cancellation.Token);
            await entered.Task;
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }

        [Fact]
        public async Task AlreadyCancelled_DoesNotSendHttpRequest()
        {
            int requests = 0;
            using var client = Client((_, _) => { requests++; return Task.FromResult(Response(Release("v1.3.0"))); });
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new GitHubUpdateService(client).CheckForUpdateAsync(Current, cancellation.Token));
            Assert.Equal(0, requests);
        }

        private static string Release(string tag, string url = ReleaseUrl, bool draft = false, bool prerelease = false) =>
            JsonSerializer.Serialize(new { tag_name = tag, html_url = url, draft, prerelease });
        private static HttpResponseMessage Response(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        private static HttpClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) => new(new FakeHandler(send));
        private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
        }
    }
}
