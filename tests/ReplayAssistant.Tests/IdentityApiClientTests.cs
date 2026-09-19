using System.Globalization;
using System.Net;
using System.Text;
using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class IdentityApiClientTests
{
    [Fact]
    public async Task SuccessfulPostSendsReplayBodyNotEpicIds()
    {
        var handler = new StubHandler { Status = HttpStatusCode.OK };
        using var http = new HttpClient(handler);
        var settings = new AppSettings { ApiUrl = "http://localhost:9", ApiKey = "secret" };
        var client = new IdentityApiClient(http, settings);
        var ingest = Sample("GUID1", "WIN", "pc-name");
        Assert.True(await client.PostReplayAsync(ingest, CancellationToken.None));
        Assert.DoesNotContain("epic_ids", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"replay_id\":\"GUID1\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"session_id\":\"sess\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"playlist\":\"Playlist_DefaultSolo\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"epic_id\":\"9A869FE1A02E44238C965778C201F9C9\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"pc-name\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"platform\":\"WIN\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"is_bot\":false", handler.Body, StringComparison.Ordinal);
        Assert.Equal("secret", handler.ApiKey);
        Assert.Equal("Bearer secret", handler.Authorization);
    }

    [Fact]
    public async Task ServiceUnavailableKeepsFailure()
    {
        var handler = new StubHandler { Status = HttpStatusCode.ServiceUnavailable };
        using var http = new HttpClient(handler);
        var settings = new AppSettings { ApiUrl = "http://localhost:9", ApiKey = "k" };
        var client = new IdentityApiClient(http, settings);
        Assert.False(await client.PostReplayAsync(Sample("G", "WIN", "n"), CancellationToken.None));
    }

    [Fact]
    public async Task RetryFlowLeavesReplayQueuedOn503ThenFlushesOn200()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ra-api-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        using var store = new StateStore(Path.Combine(dir, "state.db"));
        var ingest = Sample("GUID1", "PS5", "console-nick");
        store.SaveIngest(ingest);
        Assert.Single(store.GetQueuedReplays());

        var handler = new StubHandler { Status = HttpStatusCode.ServiceUnavailable };
        using var http = new HttpClient(handler);
        var settings = new AppSettings { ApiUrl = "http://localhost:9", ApiKey = "k" };
        var client = new IdentityApiClient(http, settings);
        Assert.False(await client.PostReplayAsync(ingest, CancellationToken.None));
        Assert.Single(store.GetQueuedReplays());

        handler.Status = HttpStatusCode.OK;
        Assert.True(await client.PostReplayAsync(ingest, CancellationToken.None));
        store.MarkReplayPosted(ingest.ReplayId);
        Assert.Empty(store.GetQueuedReplays());
    }

    [Fact]
    public void SendsRawPlatformEvenWhenFamilyIsUnknown()
    {
        var player = ReplayPlayersRequestFactory.ToPlayer(
            new PlayerIngest
            {
                EpicId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                Name = "android-nick",
                Platform = null,
                PlatformRaw = "AND",
            }
        );
        Assert.Equal("android-nick", player.Name);
        Assert.Equal("AND", player.Platform);
        Assert.Equal("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", player.EpicId);
    }

    private static ReplayIngest Sample(string guid, string platformRaw, string name) =>
        new()
        {
            Path = Path.Combine(Path.GetTempPath(), "a.replay"),
            ReplayId = guid,
            SessionId = "sess",
            Playlist = "Playlist_DefaultSolo",
            PlaylistKind = PlaylistKind.BattleRoyale,
            ObservedAt = DateTimeOffset.Parse("2026-09-19T18:00:00Z", CultureInfo.InvariantCulture),
            Players =
            [
                new PlayerIngest
                {
                    EpicId = "9A869FE1A02E44238C965778C201F9C9",
                    Name = name,
                    Platform = PlatformMapper.ToNameFn(platformRaw),
                    PlatformRaw = platformRaw,
                },
            ],
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        public string Body { get; private set; } = "";

        public string? ApiKey { get; private set; }

        public string? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (request.Headers.TryGetValues(AppSettings.ApiKeyHeaderName, out var values))
            {
                ApiKey = values.FirstOrDefault();
            }

            if (request.Headers.TryGetValues(AppSettings.AuthorizationHeaderName, out var auth))
            {
                Authorization = auth.FirstOrDefault();
            }

            if (request.Content is not null)
            {
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(Status)
            {
                Content = new StringContent("{\"duplicate\":true}", Encoding.UTF8, "application/json"),
            };
        }
    }
}
