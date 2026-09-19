namespace ReplayAssistant.Core;

public enum PlaylistKind
{
    Unknown = 0,
    BattleRoyale = 1,
    Reload = 2,
    Skipped = 3,
}

/// <summary>
/// Token catalog for NameFN PHP (deny-list + optional classify). The EXE does not
/// use this as a POST gate — see docs/php-playlist-filter.md.
/// </summary>
public static class PlaylistFilter
{
    private static readonly string[] DropTokens =
    [
        "creative",
        "playground",
        "papaya",
        "delmar",
        "juno",
        "sparks",
        "festival",
        "figment",
        "pilgrim",
        "campaign",
        "saveourworld",
        "stw",
        "vkplay",
        "forbiddenfruit",
        "melt",
        "toss",
        "respawn",
        "solidgold",
        "bigbattle",
        "itemtest",
        "bluecheese",
        "thanos",
        "avengers",
    ];

    private static readonly string[] ReloadTokens =
    [
        "reload",
        "dashberry",
        "jumpbear",
        "blastberry",
    ];

    private static readonly string[] BattleRoyaleTokens =
    [
        "ropesmile",
        "punchberry",
        "piperboot",
        "matchmist",
        "sourspawn",
        "squareclub",
        "sunflower",
        "timberstake",
        "defaultsolo",
        "defaultduo",
        "defaulttrio",
        "defaultsquad",
        "nobuildbr",
        "showdown",
        "habanero",
    ];

    public static PlaylistKind Classify(string? currentPlaylist)
    {
        if (string.IsNullOrWhiteSpace(currentPlaylist))
        {
            return PlaylistKind.Unknown;
        }

        var p = currentPlaylist.Trim().ToLowerInvariant();
        if (DropTokens.Any(t => p.Contains(t, StringComparison.Ordinal)))
        {
            return PlaylistKind.Skipped;
        }

        if (ReloadTokens.Any(t => p.Contains(t, StringComparison.Ordinal)))
        {
            return PlaylistKind.Reload;
        }

        if (p is "playlist_trios" || p.StartsWith("playlist_trios_", StringComparison.Ordinal))
        {
            return PlaylistKind.BattleRoyale;
        }

        if (BattleRoyaleTokens.Any(t => p.Contains(t, StringComparison.Ordinal)))
        {
            return PlaylistKind.BattleRoyale;
        }

        return PlaylistKind.Unknown;
    }

    public static bool AllowPost(PlaylistKind kind) =>
        kind is PlaylistKind.BattleRoyale or PlaylistKind.Reload;
}
