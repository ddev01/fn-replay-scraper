using System.Globalization;
using System.Text.Json.Serialization;

namespace ReplayAssistant.Core;

public sealed class ReplayPlayersRequest
{
    [JsonPropertyName("replay_id")]
    public required string ReplayId { get; init; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("observed_at")]
    public string? ObservedAt { get; init; }

    [JsonPropertyName("playlist")]
    public string? Playlist { get; init; }

    [JsonPropertyName("players")]
    public required ReplayPlayerRequest[] Players { get; init; }
}

public sealed class ReplayPlayerRequest
{
    [JsonPropertyName("epic_id")]
    public required string EpicId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("platform")]
    public string? Platform { get; init; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; init; }
}

public static class ReplayPlayersRequestFactory
{
    public const int MaxPlayers = 1000;

    public static ReplayPlayersRequest? TryCreate(ReplayIngest ingest)
    {
        if (string.IsNullOrWhiteSpace(ingest.ReplayId) || ingest.Players.Count == 0)
        {
            return null;
        }

        var players = ingest
            .Players.Take(MaxPlayers)
            .Select(ToPlayer)
            .ToArray();
        if (players.Length == 0)
        {
            return null;
        }

        return new ReplayPlayersRequest
        {
            ReplayId = ingest.ReplayId,
            SessionId = ingest.SessionId,
            ObservedAt = ingest.ObservedAt is { } at
                ? at.ToUniversalTime()
                    .UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
                : null,
            Playlist = ingest.Playlist,
            Players = players,
        };
    }

    public static ReplayPlayerRequest ToPlayer(PlayerIngest player)
    {
        var canName = !string.IsNullOrWhiteSpace(player.Name);
        var canPlatform = !string.IsNullOrWhiteSpace(player.PlatformRaw);
        return new ReplayPlayerRequest
        {
            EpicId = player.EpicId,
            IsBot = false,
            Name = canName ? player.Name : null,
            Platform = canPlatform ? player.PlatformRaw : null,
        };
    }
}
