using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class NameIngestMapperTests
{
    [Fact]
    public void AnonymousPsnKeepsVisibleNameWithPlatform()
    {
        var row = NameIngestMapper.TryMapHuman(
            isBot: false,
            epicId: "9A869FE1A02E44238C965778C201F9C9",
            playerId: null,
            playerName: "console-nick",
            streamerModeName: "FAKE",
            playerNameCustomOverride: "override",
            platform: "PS5",
            platformUniqueNetId: "08134443882203426723"
        );
        Assert.NotNull(row);
        Assert.Equal("console-nick", row!.Name);
        Assert.Equal(PlatformMapper.Psn, row.Platform);
        Assert.Equal("PS5", row.PlatformRaw);
    }

    [Fact]
    public void StreamerOverlayIsNotUsedAsName()
    {
        var row = NameIngestMapper.TryMapHuman(
            isBot: false,
            epicId: "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            playerId: null,
            playerName: "pc-name",
            streamerModeName: "StreamerOverlay",
            playerNameCustomOverride: null,
            platform: "WIN",
            platformUniqueNetId: null
        );
        Assert.Equal("pc-name", row!.Name);
        Assert.Equal(PlatformMapper.Epic, row.Platform);
    }

    [Fact]
    public void UnknownPlatformStillSendsPlayerNameAndRawToken()
    {
        var unknown = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var row = NameIngestMapper.TryMapHuman(
            isBot: false,
            epicId: "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
            playerId: null,
            playerName: "android-nick",
            streamerModeName: null,
            playerNameCustomOverride: null,
            platform: "AND",
            platformUniqueNetId: null,
            unknownPlatforms: unknown
        );
        Assert.Equal("android-nick", row!.Name);
        Assert.Null(row.Platform);
        Assert.Equal("AND", row.PlatformRaw);
        Assert.Contains("AND", unknown);
    }

    [Fact]
    public void BotsAndInvalidIdsAreDropped()
    {
        Assert.Null(
            NameIngestMapper.TryMapHuman(
                true,
                "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
                null,
                "bot",
                null,
                null,
                "WIN",
                null
            )
        );
        Assert.Null(
            NameIngestMapper.TryMapHuman(false, null, "not-an-id", "x", null, null, "WIN", null)
        );
    }
}

public class PlaylistFilterTests
{
    [Theory]
    [InlineData("Playlist_Habanero_NoBuild_DashBerry_Solo", PlaylistKind.Reload)]
    [InlineData("Playlist_Habanero_NoBuild_JumpBear_Solo", PlaylistKind.Reload)]
    [InlineData("Playlist_Habanero_NoBuild_DashBerry_Squads", PlaylistKind.Reload)]
    [InlineData("Playlist_Habanero_DashBerry_Solo", PlaylistKind.Reload)]
    [InlineData("Playlist_JumpBearNoBuildSolo", PlaylistKind.Reload)]
    [InlineData("Playlist_BlastBerryNoBuildTrio", PlaylistKind.Reload)]
    [InlineData("Playlist_RopeSmileNoBuildSolo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_RopeSmileSolo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_Habanero_RopeSmile_Solo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_Habanero_NoBuild_RopeSmile_Solo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_DefaultSolo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_DefaultDuo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_Trios", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_NoBuildBR_Habanero_Solo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_ShowdownAlt_Duos", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_Habanero_Solo", PlaylistKind.BattleRoyale)]
    [InlineData("Playlist_Melt_Solo", PlaylistKind.Skipped)]
    [InlineData("Playlist_Toss_Duos", PlaylistKind.Skipped)]
    [InlineData("Playlist_Respawn_24", PlaylistKind.Skipped)]
    [InlineData("Playlist_Papaya", PlaylistKind.Skipped)]
    [InlineData("Playlist_Habanero_SunflowerVKPlaySolo", PlaylistKind.Skipped)]
    [InlineData("Playlist_Habanero_Figment_Trio", PlaylistKind.Skipped)]
    [InlineData("Playlist_PlaygroundV2", PlaylistKind.Skipped)]
    [InlineData("Playlist_Creative_Discover", PlaylistKind.Skipped)]
    [InlineData("Playlist_DelMar_Habanero_Competitive_QA_1", PlaylistKind.Skipped)]
    [InlineData("", PlaylistKind.Unknown)]
    [InlineData("Playlist_SomethingWeird", PlaylistKind.Unknown)]
    public void ClassifiesKnownTokens(string playlist, PlaylistKind kind)
    {
        Assert.Equal(kind, PlaylistFilter.Classify(playlist));
    }

    [Fact]
    public void EmptyPlaylistIsUnknown()
    {
        Assert.Equal(PlaylistKind.Unknown, PlaylistFilter.Classify(null));
        Assert.Equal(PlaylistKind.Unknown, PlaylistFilter.Classify(""));
    }
}

public class PlatformMapperTests
{
    [Theory]
    [InlineData("WIN", "epic")]
    [InlineData("PS5", "psn")]
    [InlineData("PSN", "psn")]
    [InlineData("XSX", "xbl")]
    [InlineData("SWITCH", "nintendo")]
    [InlineData("AND", null)]
    [InlineData("", null)]
    public void MapsOrLogsUnknown(string raw, string? expected)
    {
        Assert.Equal(expected, PlatformMapper.ToNameFn(raw));
    }
}

public class EpicAccountIdTests
{
    [Fact]
    public void Accepts32HexAndHyphenated()
    {
        Assert.Equal(
            "9A869FE1A02E44238C965778C201F9C9",
            EpicAccountId.Normalize("9A869FE1A02E44238C965778C201F9C9")
        );
        Assert.Equal(
            "9A869FE1A02E44238C965778C201F9C9",
            EpicAccountId.Normalize("9a869fe1-a02e-4423-8c96-5778c201f9c9")
        );
        Assert.Null(EpicAccountId.Normalize("not-an-id"));
        Assert.Null(EpicAccountId.Normalize("08134443882203426723"));
    }
}
