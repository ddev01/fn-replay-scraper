using FortniteReplayReader.Models;

namespace ReplayAssistant.Core;

public sealed class ReplayIngest
{
    public string Path { get; init; } = "";

    public string ReplayId { get; init; } = "";

    public string? SessionId { get; init; }

    public DateTimeOffset? ObservedAt { get; init; }

    public string? Playlist { get; init; }

    public PlaylistKind PlaylistKind { get; init; }

    public IReadOnlyList<PlayerIngest> Players { get; init; } = [];

    public IReadOnlyList<string> UnknownPlatforms { get; init; } = [];
}

public sealed class PlayerIngest
{
    public required string EpicId { get; init; }

    public string? Name { get; init; }

    public string? Platform { get; init; }

    public string? PlatformRaw { get; init; }

    public string? PlatformUniqueNetId { get; init; }

    public bool IsBot { get; init; }
}

public static class NameIngestMapper
{
    public static ReplayIngest FromReplay(FortniteReplay replay, string sourcePath)
    {
        var playlist = replay.GameData?.CurrentPlaylist;
        var kind = PlaylistFilter.Classify(playlist);
        var observed = replay.GameData?.UtcTimeStartedMatch is DateTime utc
            ? new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
            : replay.Info?.Timestamp is DateTime ts
                ? new DateTimeOffset(DateTime.SpecifyKind(ts, DateTimeKind.Utc))
                : (DateTimeOffset?)null;

        var unknownPlatforms = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var players = new List<PlayerIngest>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (replay.PlayerData is not null)
        {
            foreach (var player in replay.PlayerData)
            {
                var mappedPlayer = TryMapHuman(player, seen, unknownPlatforms);
                if (mappedPlayer is not null)
                {
                    players.Add(mappedPlayer);
                }
            }
        }

        return new ReplayIngest
        {
            Path = sourcePath,
            ReplayId = replay.Header?.Guid ?? "",
            SessionId = string.IsNullOrWhiteSpace(replay.GameData?.GameSessionId)
                ? null
                : replay.GameData!.GameSessionId,
            ObservedAt = observed,
            Playlist = playlist,
            PlaylistKind = kind,
            Players = players,
            UnknownPlatforms = unknownPlatforms.ToArray(),
        };
    }

    public static PlayerIngest? TryMapHuman(
        bool isBot,
        string? epicId,
        string? playerId,
        string? playerName,
        string? streamerModeName,
        string? playerNameCustomOverride,
        string? platform,
        string? platformUniqueNetId,
        HashSet<string>? seen = null,
        SortedSet<string>? unknownPlatforms = null
    )
    {
        _ = streamerModeName;
        _ = playerNameCustomOverride;
        if (isBot)
        {
            return null;
        }

        var epic = EpicAccountId.Normalize(epicId) ?? EpicAccountId.Normalize(playerId);
        if (epic is null || seen is not null && !seen.Add(epic))
        {
            return null;
        }

        var rawPlatform = string.IsNullOrWhiteSpace(platform) ? null : platform.Trim();
        var mapped = PlatformMapper.ToNameFn(rawPlatform);
        var visibleName = string.IsNullOrWhiteSpace(playerName) ? null : playerName.Trim();
        if (mapped is null && rawPlatform is not null)
        {
            unknownPlatforms?.Add(rawPlatform);
        }

        return new PlayerIngest
        {
            EpicId = epic,
            IsBot = false,
            PlatformRaw = rawPlatform,
            Platform = mapped,
            PlatformUniqueNetId = string.IsNullOrWhiteSpace(platformUniqueNetId)
                ? null
                : platformUniqueNetId,
            Name = visibleName,
        };
    }

    private static PlayerIngest? TryMapHuman(
        PlayerData player,
        HashSet<string> seen,
        SortedSet<string> unknownPlatforms
    ) =>
        TryMapHuman(
            player.IsBot,
            player.EpicId,
            player.PlayerId,
            player.PlayerName,
            player.StreamerModeName,
            player.PlayerNameCustomOverride,
            player.Platform,
            player.PlatformUniqueNetId,
            seen,
            unknownPlatforms
        );
}
