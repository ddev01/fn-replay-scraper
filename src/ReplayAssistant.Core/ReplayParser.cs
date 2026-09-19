namespace ReplayAssistant.Core;

public sealed class ReplayParseResult
{
    public bool Success { get; init; }

    public bool TimedOut { get; init; }

    public bool IsLive { get; init; }

    public string Status { get; init; } = ReplayStatus.Failed;

    public string? Error { get; init; }

    public string SourcePath { get; init; } = "";

    public IReadOnlyList<string> HumanIds { get; init; } = [];

    public ReplayIngest? Ingest { get; init; }

    public static ReplayParseResult Ok(string source, ReplayIngest ingest) =>
        new()
        {
            Success = true,
            SourcePath = source,
            Ingest = ingest,
            HumanIds = ingest.Players.Select(p => p.EpicId).ToArray(),
            Status = ReplayStatus.Parsed,
        };

    public static ReplayParseResult Live(string source) =>
        new()
        {
            Success = false,
            IsLive = true,
            SourcePath = source,
            Status = ReplayStatus.Live,
            Error = "replay is still live; skipping parse",
        };

    public static ReplayParseResult Fail(
        string source,
        string error,
        bool timedOut = false,
        string status = ReplayStatus.Unsupported
    ) =>
        new()
        {
            Success = false,
            SourcePath = source,
            Error = error,
            TimedOut = timedOut,
            Status = timedOut ? ReplayStatus.Failed : status,
        };
}

public static class ReplayStatus
{
    public const string Pending = "pending";
    public const string Parsed = "parsed";
    public const string Failed = "failed";
    public const string Unsupported = "unsupported";
    public const string Live = "live";
}

public sealed class ReplayParser
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    private readonly IReplayFileReader _reader;
    private readonly TimeSpan _timeout;

    public ReplayParser(IReplayFileReader? reader = null, TimeSpan? timeout = null)
    {
        _reader = reader ?? new FortniteReplayFileReader();
        _timeout = timeout ?? DefaultTimeout;
    }

    public ReplayParseResult ParseCompleted(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string? working = null;
        try
        {
            if (ReplayHeader.TryInspect(sourcePath, out var header) && header.IsLive)
            {
                return ReplayParseResult.Live(sourcePath);
            }

            working = ReplayFileAccess.CopyWithReadWriteShare(sourcePath);
            if (ReplayHeader.TryInspect(working, out var copyHeader) && copyHeader.IsLive)
            {
                return ReplayParseResult.Live(sourcePath);
            }

            var task = Task.Run(() => _reader.ReadMinimal(working));
            if (!task.Wait(_timeout))
            {
                return ReplayParseResult.Fail(
                    sourcePath,
                    $"parse timed out after {_timeout.TotalSeconds:0}s",
                    timedOut: true
                );
            }

            var replay = task.Result;
            if (replay.Info?.IsLive == true)
            {
                return ReplayParseResult.Live(sourcePath);
            }

            var ingest = NameIngestMapper.FromReplay(replay, sourcePath);
            return ReplayParseResult.Ok(sourcePath, ingest);
        }
        catch (AggregateException ex)
        {
            var inner = ex.InnerException ?? ex;
            return ReplayParseResult.Fail(sourcePath, inner.Message);
        }
        catch (Exception ex)
        {
            return ReplayParseResult.Fail(sourcePath, ex.Message);
        }
        finally
        {
            if (working is not null)
            {
                try
                {
                    File.Delete(working);
                }
                catch (IOException)
                {
                    // Best-effort temp cleanup.
                }
            }
        }
    }
}
