using System.Globalization;
using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class StateStoreTests
{
    [Fact]
    public void ParsedReplayIsFinishedAndUnsupportedIsNotRetried()
    {
        using var store = Open();
        var parsed = Path.Combine(Path.GetTempPath(), "a.replay");
        var bad = Path.Combine(Path.GetTempPath(), "b.replay");
        store.UpsertReplay(parsed, 10, DateTime.UtcNow, ReplayStatus.Parsed, null);
        store.UpsertReplay(bad, 10, DateTime.UtcNow, ReplayStatus.Unsupported, "old season");
        Assert.True(store.IsReplayFinished(parsed));
        Assert.True(store.IsReplayFinished(bad));
        Assert.False(store.IsReplayFinished(Path.Combine(Path.GetTempPath(), "c.replay")));
    }

    [Fact]
    public void QueuedIdsStayUntilMarkedSent()
    {
        using var store = Open();
        store.EnqueueIdentities(["epic-a", "epic-b", "epic-a"]);
        var queued = store.GetQueuedIdentities();
        Assert.Equal(2, queued.Count);
        Assert.False(store.WasSent("epic-a"));
        store.MarkSent(["epic-a"]);
        Assert.True(store.WasSent("epic-a"));
        Assert.False(store.WasSent("epic-b"));
        Assert.Equal(["epic-b"], store.GetQueuedIdentities());
    }

    [Fact]
    public void AlreadySentIdIsNotRequeued()
    {
        using var store = Open();
        store.EnqueueIdentities(["epic-a"]);
        store.MarkSent(["epic-a"]);
        store.EnqueueIdentities(["epic-a"]);
        Assert.Empty(store.GetQueuedIdentities());
        Assert.True(store.WasSent("epic-a"));
    }

    [Fact]
    public void PersistsNameAndPlatformForBackfill()
    {
        using var store = Open();
        var ingest = new ReplayIngest
        {
            Path = Path.Combine(Path.GetTempPath(), "x.replay"),
            ReplayId = "GUID1",
            SessionId = "sess",
            Playlist = "Playlist_RopeSmileNoBuildSolo",
            PlaylistKind = PlaylistKind.BattleRoyale,
            ObservedAt = DateTimeOffset.Parse("2026-09-17T20:00:00Z", CultureInfo.InvariantCulture),
            Players =
            [
                new PlayerIngest
                {
                    EpicId = "9A869FE1A02E44238C965778C201F9C9",
                    Name = "nick",
                    Platform = PlatformMapper.Psn,
                    PlatformRaw = "PS5",
                },
            ],
        };
        store.SaveIngest(ingest);
        var loaded = store.GetIngest("GUID1");
        Assert.NotNull(loaded);
        Assert.Equal("sess", loaded!.SessionId);
        Assert.Equal("nick", loaded.Players[0].Name);
        Assert.Equal(PlatformMapper.Psn, loaded.Players[0].Platform);
        Assert.Single(store.GetQueuedReplays());
        store.MarkReplayPosted("GUID1");
        Assert.Empty(store.GetQueuedReplays());
    }

    [Fact]
    public void CreativeAndUnknownPlaylistsAreQueuedForPost()
    {
        using var store = Open();
        store.SaveIngest(
            new ReplayIngest
            {
                Path = Path.Combine(Path.GetTempPath(), "c.replay"),
                ReplayId = "CREATIVE",
                Playlist = "Playlist_PlaygroundV2",
                PlaylistKind = PlaylistKind.Skipped,
                Players =
                [
                    new PlayerIngest { EpicId = "9A869FE1A02E44238C965778C201F9C9" },
                ],
            }
        );
        store.SaveIngest(
            new ReplayIngest
            {
                Path = Path.Combine(Path.GetTempPath(), "u.replay"),
                ReplayId = "UNKNOWN",
                Playlist = null,
                PlaylistKind = PlaylistKind.Unknown,
                Players =
                [
                    new PlayerIngest { EpicId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" },
                ],
            }
        );
        Assert.Equal(2, store.GetQueuedReplays().Count);
    }

    private static StateStore Open()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ra-db-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return new StateStore(Path.Combine(dir, "state.db"));
    }
}
