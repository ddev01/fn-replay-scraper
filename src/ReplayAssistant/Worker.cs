using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReplayAssistant.Core;

namespace ReplayAssistant;

internal sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly AppSettings _settings;
    private readonly AppPaths _paths;
    private readonly StateStore _store;
    private readonly ReplayParser _parser;
    private readonly IdentityApiClient _api;
    private readonly GitHubUpdateChecker _updates;
    private readonly HttpClient _downloadClient;
    private readonly ChannelWork _queue = new();

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime lifetime,
        AppSettings settings,
        AppPaths paths,
        StateStore store,
        ReplayParser parser,
        IdentityApiClient api,
        GitHubUpdateChecker updates,
        HttpClient downloadClient
    )
    {
        _logger = logger;
        _lifetime = lifetime;
        _settings = settings;
        _paths = paths;
        _store = store;
        _parser = parser;
        _api = api;
        _updates = updates;
        _downloadClient = downloadClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        _paths.EnsureDirectories();

        try
        {
            await TryUpdateAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            HostLog.UpdateFailed(_logger, ex);
        }

        try
        {
            EnqueueCatchUp();
            await FlushQueueAsync(stoppingToken).ConfigureAwait(false);
            await FlushApiAsync(stoppingToken).ConfigureAwait(false);
            HostLog.BootFinished(_logger);
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private void EnqueueCatchUp()
    {
        var candidates = CatchUpScanner.FindCompletedCandidates(
            _settings.ResolvedDemosPath,
            _store.IsReplayFinished,
            DateTimeOffset.UtcNow,
            _settings.StableWindow
        );
        foreach (var path in candidates)
        {
            _queue.Enqueue(path);
        }

        HostLog.CatchUp(_logger, candidates.Count);
    }

    private async Task FlushQueueAsync(CancellationToken stoppingToken)
    {
        while (_queue.TryDequeue(out var path))
        {
            stoppingToken.ThrowIfCancellationRequested();
            if (_store.IsReplayFinished(path))
            {
                continue;
            }

            ParseOne(path);
            await FlushApiAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private void ParseOne(string path)
    {
        var (size, write) = DemosFolder.Snapshot(path);
        HostLog.Parsing(_logger, path, size);
        var result = _parser.ParseCompleted(path);
        if (result.IsLive)
        {
            HostLog.SkippedLive(_logger, path);
            return;
        }

        _store.UpsertReplay(path, size, write, result.Status, result.Error);
        if (result.Success && result.Ingest is { } ingest)
        {
            _store.SaveIngest(ingest);
            foreach (var unknown in ingest.UnknownPlatforms)
            {
                HostLog.UnknownPlatform(_logger, unknown);
            }

            HostLog.Parsed(_logger, path, ingest.Players.Count, ingest.PlaylistKind, ingest.Playlist);
        }
        else if (result.Success)
        {
            HostLog.Parsed(_logger, path, result.HumanIds.Count, PlaylistKind.Unknown, null);
        }
        else
        {
            HostLog.ParseFailed(_logger, path, result.Status, result.Error);
        }

        MemoryPressure.ReleaseAfterParse();
    }

    private async Task FlushApiAsync(CancellationToken stoppingToken)
    {
        var queued = _store.GetQueuedReplays(limit: 60);
        if (queued.Count == 0)
        {
            return;
        }

        var posted = 0;
        for (var i = 0; i < queued.Count; i++)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var ingest = queued[i];
            var ok = await _api.PostReplayAsync(ingest, stoppingToken).ConfigureAwait(false);
            if (!ok)
            {
                HostLog.ApiFailed(_logger, queued.Count - posted);
                return;
            }

            _store.MarkReplayPosted(ingest.ReplayId);
            posted++;
            HostLog.Posted(_logger, ingest.ReplayId, ingest.Players.Count);
            if (i < queued.Count - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task TryUpdateAsync(CancellationToken stoppingToken)
    {
        var current =
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0, 0);
        var newer = await _updates.GetNewerReleaseAsync(current, stoppingToken).ConfigureAwait(false);
        if (newer is null)
        {
            return;
        }

        HostLog.DownloadingUpdate(_logger, newer.Version);
        var dest = Path.Combine(_paths.UpdateDirectory, AppPaths.ExeFileName);
        Directory.CreateDirectory(_paths.UpdateDirectory);
        await using (var input = await _downloadClient.GetStreamAsync(newer.AssetUri, stoppingToken)
            .ConfigureAwait(false))
        await using (var output = File.Create(dest))
        {
            await input.CopyToAsync(output, stoppingToken).ConfigureAwait(false);
        }

        var running = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, AppPaths.ExeFileName);
        var updater = File.Exists(_paths.InstalledUpdaterPath)
            ? _paths.InstalledUpdaterPath
            : Path.Combine(AppContext.BaseDirectory, AppPaths.UpdaterFileName);
        if (UpdateLauncher.TryStartUpdater(updater, dest, running))
        {
            HostLog.UpdaterStarted(_logger);
            Environment.Exit(0);
        }
    }

    private sealed class ChannelWork
    {
        private readonly Queue<string> _queue = new();
        private readonly HashSet<string> _set = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new();

        public void Enqueue(string path)
        {
            var key = Path.GetFullPath(path);
            lock (_gate)
            {
                if (_set.Add(key))
                {
                    _queue.Enqueue(key);
                }
            }
        }

        public bool TryDequeue(out string path)
        {
            lock (_gate)
            {
                if (_queue.Count == 0)
                {
                    path = "";
                    return false;
                }

                path = _queue.Dequeue();
                _set.Remove(path);
                return true;
            }
        }
    }
}
