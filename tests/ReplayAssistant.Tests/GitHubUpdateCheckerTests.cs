using System.Net;
using System.Text;
using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class GitHubUpdateCheckerTests
{
    [Fact]
    public void ParsesVersionTags()
    {
        Assert.True(GitHubUpdateChecker.TryParseVersion("v0.2.0", out var v));
        Assert.Equal(new Version(0, 2, 0), v);
        Assert.True(GitHubUpdateChecker.TryParseVersion("0.1.0", out var v2));
        Assert.Equal(new Version(0, 1, 0), v2);
    }

    [Fact]
    public async Task ReturnsNewerReleaseAsset()
    {
        var json = """
            {
              "tag_name": "v0.2.0",
              "assets": [
                {
                  "name": "ReplayAssistant.exe",
                  "browser_download_url": "https://github.com/example/releases/ReplayAssistant.exe"
                }
              ]
            }
            """;
        var handler = new JsonHandler(json);
        using var http = new HttpClient(handler);
        var checker = new GitHubUpdateChecker(
            http,
            new AppSettings { GitHubOwner = "o", GitHubRepo = "r" }
        );
        var newer = await checker.GetNewerReleaseAsync(new Version(0, 1, 0), CancellationToken.None);
        Assert.NotNull(newer);
        Assert.Equal(new Version(0, 2, 0), newer!.Version);
        Assert.Equal("ReplayAssistant.exe", newer.AssetName);
    }

    [Fact]
    public async Task IgnoresSameOrOlderRelease()
    {
        var json = """{ "tag_name": "v0.1.0", "assets": [] }""";
        using var http = new HttpClient(new JsonHandler(json));
        var checker = new GitHubUpdateChecker(
            http,
            new AppSettings { GitHubOwner = "o", GitHubRepo = "r" }
        );
        Assert.Null(await checker.GetNewerReleaseAsync(new Version(0, 1, 0), CancellationToken.None));
    }

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }
            );
    }
}
