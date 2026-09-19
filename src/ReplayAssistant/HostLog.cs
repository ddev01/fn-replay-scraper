using Microsoft.Extensions.Logging;
using ReplayAssistant.Core;

namespace ReplayAssistant;

internal static partial class HostLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Replay Assistant {Version} starting")]
    public static partial void Starting(ILogger logger, Version? version);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "API configured={Configured} host={Host}"
    )]
    public static partial void ApiReady(ILogger logger, bool configured, string host);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Update check failed")]
    public static partial void UpdateFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Catch-up queued {Count} replays")]
    public static partial void CatchUp(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Fortnite running={Running}; watcher={Watcher}"
    )]
    public static partial void FortniteWatch(ILogger logger, bool running, bool watcher);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parsing {Path} size={Size}")]
    public static partial void Parsing(ILogger logger, string path, long size);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped live replay {Path}")]
    public static partial void SkippedLive(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Parsed {Path} humans={Count} kind={Kind} playlist={Playlist}"
    )]
    public static partial void Parsed(
        ILogger logger,
        string path,
        int count,
        PlaylistKind kind,
        string? playlist
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Skipped POST for {Path} playlist={Playlist} kind={Kind}; observations stored"
    )]
    public static partial void SkippedPlaylist(
        ILogger logger,
        string path,
        string? playlist,
        PlaylistKind kind
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Unknown platform token {Platform}; sending name and raw platform for API policy"
    )]
    public static partial void UnknownPlatform(ILogger logger, string platform);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Parse failed {Path} status={Status} error={Error}"
    )]
    public static partial void ParseFailed(
        ILogger logger,
        string path,
        string status,
        string? error
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Posted replay {ReplayId} players={Count}"
    )]
    public static partial void Posted(ILogger logger, string replayId, int count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "API post failed; keeping {Count} replays queued"
    )]
    public static partial void ApiFailed(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloading update {Version}")]
    public static partial void DownloadingUpdate(ILogger logger, Version version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updater started; exiting host")]
    public static partial void UpdaterStarted(ILogger logger);
}
