using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ReplayAssistant.Core;

public sealed class StateStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();

    public StateStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString());
        _connection.Open();
        using (var pragma = _connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL;";
            pragma.ExecuteNonQuery();
        }

        Initialize();
    }

    public bool IsReplayFinished(string path)
    {
        var status = GetReplayStatus(path);
        return status is ReplayStatus.Parsed or ReplayStatus.Unsupported;
    }

    public string? GetReplayStatus(string path)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT status FROM replays WHERE path = $path;";
            cmd.Parameters.AddWithValue("$path", Normalize(path));
            var value = cmd.ExecuteScalar() as string;
            return value;
        }
    }

    public void UpsertReplay(
        string path,
        long size,
        DateTime lastWriteUtc,
        string status,
        string? error
    )
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO replays(path, size, last_write_utc, status, error, parsed_at)
                VALUES($path, $size, $write, $status, $error, $parsed)
                ON CONFLICT(path) DO UPDATE SET
                  size = excluded.size,
                  last_write_utc = excluded.last_write_utc,
                  status = excluded.status,
                  error = excluded.error,
                  parsed_at = excluded.parsed_at;
                """;
            cmd.Parameters.AddWithValue("$path", Normalize(path));
            cmd.Parameters.AddWithValue("$size", size);
            cmd.Parameters.AddWithValue("$write", lastWriteUtc.ToUniversalTime().ToString("O"));
            cmd.Parameters.AddWithValue("$status", status);
            cmd.Parameters.AddWithValue("$error", (object?)error ?? DBNull.Value);
            cmd.Parameters.AddWithValue(
                "$parsed",
                status is ReplayStatus.Parsed or ReplayStatus.Unsupported
                    ? DateTime.UtcNow.ToString("O")
                    : DBNull.Value
            );
            cmd.ExecuteNonQuery();
        }
    }

    public void SaveIngest(ReplayIngest ingest)
    {
        lock (_gate)
        {
            using var tx = _connection.BeginTransaction();
            using (var match = _connection.CreateCommand())
            {
                match.Transaction = tx;
                match.CommandText = """
                    INSERT INTO match_observations(
                      replay_guid, path, session_id, playlist, playlist_kind, observed_at, recorded_at)
                    VALUES($guid, $path, $session, $playlist, $kind, $observed, $recorded)
                    ON CONFLICT(replay_guid) DO UPDATE SET
                      path = excluded.path,
                      session_id = excluded.session_id,
                      playlist = excluded.playlist,
                      playlist_kind = excluded.playlist_kind,
                      observed_at = excluded.observed_at;
                    """;
                match.Parameters.AddWithValue("$guid", string.IsNullOrWhiteSpace(ingest.ReplayId) ? ingest.Path : ingest.ReplayId);
                match.Parameters.AddWithValue("$path", Normalize(ingest.Path));
                match.Parameters.AddWithValue("$session", (object?)ingest.SessionId ?? DBNull.Value);
                match.Parameters.AddWithValue("$playlist", (object?)ingest.Playlist ?? DBNull.Value);
                match.Parameters.AddWithValue("$kind", ingest.PlaylistKind.ToString());
                match.Parameters.AddWithValue(
                    "$observed",
                    ingest.ObservedAt is { } at ? at.ToUniversalTime().ToString("O") : DBNull.Value
                );
                match.Parameters.AddWithValue("$recorded", DateTime.UtcNow.ToString("O"));
                match.ExecuteNonQuery();
            }

            var guid = string.IsNullOrWhiteSpace(ingest.ReplayId) ? ingest.Path : ingest.ReplayId;
            foreach (var player in ingest.Players)
            {
                using var row = _connection.CreateCommand();
                row.Transaction = tx;
                row.CommandText = """
                    INSERT INTO player_observations(
                      replay_guid, epic_id, name, platform, platform_raw, platform_unique_net_id, is_bot)
                    VALUES($guid, $epic, $name, $platform, $raw, $net, $bot)
                    ON CONFLICT(replay_guid, epic_id) DO UPDATE SET
                      name = excluded.name,
                      platform = excluded.platform,
                      platform_raw = excluded.platform_raw,
                      platform_unique_net_id = excluded.platform_unique_net_id,
                      is_bot = excluded.is_bot;
                    """;
                row.Parameters.AddWithValue("$guid", guid);
                row.Parameters.AddWithValue("$epic", player.EpicId);
                row.Parameters.AddWithValue("$name", (object?)player.Name ?? DBNull.Value);
                row.Parameters.AddWithValue("$platform", (object?)player.Platform ?? DBNull.Value);
                row.Parameters.AddWithValue("$raw", (object?)player.PlatformRaw ?? DBNull.Value);
                row.Parameters.AddWithValue("$net", (object?)player.PlatformUniqueNetId ?? DBNull.Value);
                row.Parameters.AddWithValue("$bot", player.IsBot ? 1 : 0);
                row.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }

    public IReadOnlyList<ReplayIngest> GetQueuedReplays(int limit = 60)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                SELECT replay_guid FROM match_observations
                WHERE posted_at IS NULL
                  AND replay_guid IS NOT NULL
                  AND length(replay_guid) > 0
                ORDER BY recorded_at
                LIMIT $limit;
                """;
            cmd.Parameters.AddWithValue("$limit", limit);
            using var reader = cmd.ExecuteReader();
            var guids = new List<string>();
            while (reader.Read())
            {
                guids.Add(reader.GetString(0));
            }

            reader.Close();
            return guids.Select(g => GetIngestUnlocked(g)).Where(x => x is { Players.Count: > 0 }).Cast<ReplayIngest>().ToArray();
        }
    }

    public void MarkReplayPosted(string replayGuid)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                UPDATE match_observations
                SET posted_at = $sent
                WHERE replay_guid = $guid;
                """;
            cmd.Parameters.AddWithValue("$sent", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("$guid", replayGuid);
            cmd.ExecuteNonQuery();
        }
    }

    public ReplayIngest? GetIngest(string replayGuid)
    {
        lock (_gate)
        {
            return GetIngestUnlocked(replayGuid);
        }
    }

    private ReplayIngest? GetIngestUnlocked(string replayGuid)
    {
        using var match = _connection.CreateCommand();
            match.CommandText = """
                SELECT replay_guid, path, session_id, playlist, playlist_kind, observed_at
                FROM match_observations WHERE replay_guid = $guid;
                """;
            match.Parameters.AddWithValue("$guid", replayGuid);
            using var reader = match.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var kind = Enum.TryParse<PlaylistKind>(reader.GetString(4), out var parsed)
                ? parsed
                : PlaylistKind.Unknown;
            DateTimeOffset? observed = null;
            if (
                !reader.IsDBNull(5)
                && DateTimeOffset.TryParse(
                    reader.GetString(5),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var at
                )
            )
            {
                observed = at;
            }

            var path = reader.GetString(1);
            var session = reader.IsDBNull(2) ? null : reader.GetString(2);
            var playlist = reader.IsDBNull(3) ? null : reader.GetString(3);
            reader.Close();

            using var players = _connection.CreateCommand();
            players.CommandText = """
                SELECT epic_id, name, platform, platform_raw, platform_unique_net_id, is_bot
                FROM player_observations WHERE replay_guid = $guid ORDER BY epic_id;
                """;
            players.Parameters.AddWithValue("$guid", replayGuid);
            using var pr = players.ExecuteReader();
            var list = new List<PlayerIngest>();
            while (pr.Read())
            {
                list.Add(
                    new PlayerIngest
                    {
                        EpicId = pr.GetString(0),
                        Name = pr.IsDBNull(1) ? null : pr.GetString(1),
                        Platform = pr.IsDBNull(2) ? null : pr.GetString(2),
                        PlatformRaw = pr.IsDBNull(3) ? null : pr.GetString(3),
                        PlatformUniqueNetId = pr.IsDBNull(4) ? null : pr.GetString(4),
                        IsBot = pr.GetInt32(5) != 0,
                    }
                );
            }

            return new ReplayIngest
            {
                ReplayId = replayGuid,
                Path = path,
                SessionId = session,
                Playlist = playlist,
                PlaylistKind = kind,
                ObservedAt = observed,
                Players = list,
            };
    }

    public void EnqueueIdentities(IEnumerable<string> ids)
    {
        lock (_gate)
        {
            using var tx = _connection.BeginTransaction();
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                using var cmd = _connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO identities(id, queued, sent_at)
                    VALUES($id, 1, NULL)
                    ON CONFLICT(id) DO UPDATE SET
                      queued = CASE WHEN identities.sent_at IS NULL THEN 1 ELSE identities.queued END;
                    """;
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }

    public IReadOnlyList<string> GetQueuedIdentities(int limit = 10_000)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                SELECT id FROM identities
                WHERE queued = 1 AND sent_at IS NULL
                ORDER BY id
                LIMIT $limit;
                """;
            cmd.Parameters.AddWithValue("$limit", limit);
            using var reader = cmd.ExecuteReader();
            var list = new List<string>();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }

            return list;
        }
    }

    public void MarkSent(IEnumerable<string> ids)
    {
        var now = DateTime.UtcNow.ToString("O");
        lock (_gate)
        {
            using var tx = _connection.BeginTransaction();
            foreach (var id in ids)
            {
                using var cmd = _connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    UPDATE identities
                    SET queued = 0, sent_at = $sent
                    WHERE id = $id;
                    """;
                cmd.Parameters.AddWithValue("$sent", now);
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }

    public bool WasSent(string id)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT sent_at FROM identities WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            var value = cmd.ExecuteScalar();
            return value is string;
        }
    }

    public void Dispose() => _connection.Dispose();

    private void Initialize()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS replays (
              path TEXT PRIMARY KEY,
              size INTEGER NOT NULL,
              last_write_utc TEXT NOT NULL,
              status TEXT NOT NULL,
              error TEXT,
              parsed_at TEXT
            );
            CREATE TABLE IF NOT EXISTS identities (
              id TEXT PRIMARY KEY,
              queued INTEGER NOT NULL DEFAULT 1,
              sent_at TEXT
            );
            CREATE TABLE IF NOT EXISTS match_observations (
              replay_guid TEXT PRIMARY KEY,
              path TEXT NOT NULL,
              session_id TEXT,
              playlist TEXT,
              playlist_kind TEXT NOT NULL,
              observed_at TEXT,
              recorded_at TEXT NOT NULL,
              posted_at TEXT
            );
            CREATE TABLE IF NOT EXISTS player_observations (
              replay_guid TEXT NOT NULL,
              epic_id TEXT NOT NULL,
              name TEXT,
              platform TEXT,
              platform_raw TEXT,
              platform_unique_net_id TEXT,
              is_bot INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY (replay_guid, epic_id)
            );
            """;
        cmd.ExecuteNonQuery();
        EnsureColumn("match_observations", "posted_at", "TEXT");
    }

    private void EnsureColumn(string table, string column, string type)
    {
        using var info = _connection.CreateCommand();
        info.CommandText = $"PRAGMA table_info({table});";
        using var reader = info.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        reader.Close();
        using var alter = _connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type};";
        alter.ExecuteNonQuery();
    }

    private static string Normalize(string path) => Path.GetFullPath(path);
}
