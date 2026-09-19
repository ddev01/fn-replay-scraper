# Replay Assistant

Windows background app that watches **completed** Fortnite replays, extracts unique human Epic IDs (bots excluded), and POSTs them to a placeholder API. It starts with Windows, has no console and no tray icon, and shows up in Task Manager as **Replay Assistant**.

This is not stealth software. The process is visible under a normal name and icon. There is no tray hiding, no phishing, and no attempt to conceal the program.

## Requirements

- Windows x64
- .NET 10 SDK (see `global.json`) for building
- Fortnite Demos folder (default `%LOCALAPPDATA%\FortniteGame\Saved\Demos`)

## Build

```powershell
dotnet tool restore
dotnet csharpier format
dotnet test
.\publish.ps1
```

Published files land in `dist\`: `ReplayAssistant.exe`, `ReplayAssistant.Update.exe`, `appsettings.example.json`.

## Install (set and forget)

1. Publish (or copy the two EXEs).
2. Edit `appsettings.json` next to the EXE, or copy `appsettings.example.json` to `%LOCALAPPDATA%\ReplayAssistant\appsettings.json`.
3. Run once (still no console):

```powershell
.\dist\ReplayAssistant.exe --install-startup
```

That copies the EXEs into `%LOCALAPPDATA%\ReplayAssistant\`, writes `appsettings.json` there if missing, and registers a **logon scheduled task** named Replay Assistant (current user, no admin). It also deletes any old Startup-folder `.lnk`.

The task does **not** show under Task Manager → Startup Apps. There is no tray / hidden-icons entry. When it is running you will still see **Replay Assistant** under Processes (that is intentional). Task Scheduler → Task Scheduler Library → Replay Assistant is the registration if you need to check it.

Remove:

```powershell
& "$env:LOCALAPPDATA\ReplayAssistant\ReplayAssistant.exe" --uninstall-startup
```

A single-instance mutex prevents duplicate Startup launches.

## Config

| Key / env | Purpose |
| --- | --- |
| `ApiUrl` / `REPLAY_ASSISTANT_API_URL` | Origin. Client POSTs `{ApiUrl}/api/replay/players` |
| `ApiKey` / `REPLAY_ASSISTANT_API_KEY` | `X-Api-Key` and `Authorization: Bearer` |
| `GitHubOwner` / `GitHubRepo` | Public Releases for auto-update (`ddev01` / `fn-replay-scraper`) |
| `GitHubToken` | Leave empty. Public Releases do not need a token |
| `DemosPath` | Override Demos folder |
| `StableSeconds` | Size must be unchanged this long (default 60) |
| `WatchOnlyWhenFortniteClosed` | Watch and parse only while `FortniteClient-Win64-Shipping` is not running (avoids a parse spike in the next match) |
| `FortniteProcessPollMinutes` | How often to check that process (default 5) |
| `ApiChunkSize` | Max players per replay POST (capped at 1000) |

Do not commit real API keys. `appsettings.example.json` is the template.

```http
POST /api/replay/players
X-Api-Key: <secret>
Authorization: Bearer <secret>
Content-Type: application/json

{
  "replay_id": "<Header.Guid>",
  "session_id": "<GameSessionId>",
  "observed_at": "2026-09-19T18:00:00Z",
  "playlist": "Playlist_DefaultSolo",
  "players": [
    { "epic_id": "...", "name": "...", "platform": "WIN", "is_bot": false }
  ]
}
```

One POST per completed replay with a `replay_id` and at least one human (`replay_id` makes retries idempotent). Playlist and platform **policy is on NameFN**, not the EXE: Creative/unknown playlists are still POSTed; the API returns `skipped: playlist` or ingest. Humans only (`is_bot` dropped). `PlayerName` + raw `Platform` (`WIN`, `PS5`, `AND`, …) are sent when present; `StreamerModeName` / custom override are never sent. At most 60 POSTs per flush (1s apart) to stay under 60/min.

PHP deny-list and token notes: `docs/php-playlist-filter.md`.

On non-2xx the replay stays queued (`posted_at` null) and is retried after the next parse and every hour. A 2xx `{ "duplicate": true }` is treated as success.

## How it stays light on FPS

- Idle wait is `FileSystemWatcher` (kernel events), not a tight poll of replay bytes.
- Optional Fortnite process check is every few **minutes**. With `WatchOnlyWhenFortniteClosed`, parse and the Demos watcher stay off while the client is running; when it exits, catch-up runs.
- Parse runs on a `BelowNormal` worker, one file at a time, only after size is stable, **never** while the header `IsLive` flag is set, and **never** while Fortnite is open (unless that setting is false).
- SQLite writes happen after parse, not in the watcher callback.
- After each parse the large replay graph is dropped, the GC compacting, and the working set trimmed so Task Manager RAM can fall back toward idle. Idle floor is still the self-contained .NET runtime (typically tens of MB), not a few MB.

## Auto-update

On each process start the host calls GitHub Releases `latest` (no token, public repo). If the tag is newer, it downloads `ReplayAssistant.exe` to `update\`, starts `ReplayAssistant.Update.exe` (also WinExe), and exits. The updater waits for the host PID, replaces the running EXE, and relaunches it. Closing Replay Assistant and opening it again is enough to pick up a new Release.

Publish a Release asset named `ReplayAssistant.exe` and tag `v1.0.1` (four-part `1.0.1.0` is fine). The running copy must be older than that tag.

## Logs

`%LOCALAPPDATA%\ReplayAssistant\logs\replay-assistant.log` (rotates around 512 KB). Startup and double-click stay silent (WinExe, no console flash).

## Console test run

Production `--install-startup` must not include `--console`.

```powershell
.\dist\ReplayAssistant.exe --console
```

You should see catch-up parse lines, then idle watcher logs. Ctrl+C stops it. If nothing prints and it exits immediately, another instance is already running (Task Manager → Replay Assistant).

`dotnet run` also works:

```powershell
dotnet run --project src\ReplayAssistant -- --console
```

## Parser

Uses NuGet `FortniteReplayReader` 3.1.0 (Shiqan, MIT) with `ParseMode.Minimal`. See `FINDINGS.md` for the fork bakeoff and real-file results. Replay format notes: `.cursor/skills/unreal-fortnite-replays/`.

## License

MIT (Shiqan parser). If a GPL fork were vendored, this repo would need GPL-3.0 instead.
