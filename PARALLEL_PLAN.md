# Build-in-Parallel: Replay Assistant

Target: `F:\Dev\fn-replay-scraper`  
Source of parser/tooling: `F:\Dev\fn-lobby-info`  
Product: Windows **WinExe** `ReplayAssistant.exe` (display name **Replay Assistant**), silent background, no tray.

This file is the workstream map. Independent streams may run in parallel **only** with the file ownership below. The repo started empty, so a single coherent implementation is preferred; use this table if splitting.

## Extra vs old plan (`replay_assistant_exe_a08e2c5d`)

Include all of the old plan, plus:

- Single-instance mutex (Startup must not double-launch).
- Config via `appsettings.json` + env (`REPLAY_ASSISTANT_API_URL`, `REPLAY_ASSISTANT_API_KEY`); example file committed, secrets not.
- Rotating file logs under `%LOCALAPPDATA%\ReplayAssistant\logs\` (no console).
- `--install-startup` / `--uninstall-startup` (still WinExe, no console flash).
- Process-gated watching (slow Fortnite process poll, minutes) **and** always-on boot catch-up.
- Local ID dedup **and** unique-only POST batches.
- Tests for parser, SQLite, API retry, completion detection, WinExe output type.
- No phishing, no hiding from Task Manager; friendly name + icon; no tray.

## Shared conventions (all streams)

- .NET 10, CSharpier 1.3.0, `Directory.Build.props` analyzers, xUnit `Assert` only.
- Namespaces: `ReplayAssistant.Core`, `ReplayAssistant`, `ReplayAssistant.Update`.
- Completed replays only. Never parse `IsLive` files. No Kestrel/HTML/live PKCS7 hacks.
- Parser library: NuGet `FortniteReplayReader` 3.1.0 (Shiqan, MIT) unless bakeoff proves otherwise.

---

## Stream A — Scaffold, identity, docs, tooling

**Goal:** Buildable solution, WinExe metadata, icon, Cursor/tooling copy, README.

**Owns:**

- `ReplayAssistant.sln`
- `global.json`, `Directory.Build.props`, `.editorconfig`, `.gitignore`
- `.config/dotnet-tools.json`, `.vscode/settings.json`
- `.cursor/**` (rules/skills/mcp rewritten for completed-only)
- `assets/replay-assistant.ico`
- `LICENSE`, `README.md`, `FINDINGS.md`, `appsettings.example.json`
- `publish.ps1`
- `src/ReplayAssistant/ReplayAssistant.csproj` (WinExe, icon, version, Product name)
- `src/ReplayAssistant/app.manifest` (if needed)
- `PARALLEL_PLAN.md` (this file)

**Do not touch:** Core parser/SQLite/API/watcher logic files once Stream B/C/D have created them (csproj PackageReferences may be added only in the initial scaffold).

**Depends on:** none.

**Parallel with:** B/C/D after csproj + sln exist.

---

## Stream B — Completed-only parser + header inspect

**Goal:** Copy/adapt lobby-info completed path: `FileShare.ReadWrite` copy, header `IsLive` skip, `ParseMode.Minimal` + timeout, humans = not bot + Epic id. Bakeoff note in `FINDINGS.md`.

**Owns:**

- `src/ReplayAssistant.Core/ReplayAssistant.Core.csproj`
- `src/ReplayAssistant.Core/DemosFolder.cs`
- `src/ReplayAssistant.Core/ReplayHeader.cs` (inspect only; **no** live `ClearLiveFlag`)
- `src/ReplayAssistant.Core/ReplayFileAccess.cs`
- `src/ReplayAssistant.Core/IReplayFileReader.cs`
- `src/ReplayAssistant.Core/FortniteReplayFileReader.cs` (stock reader, no live decrypt workaround)
- `src/ReplayAssistant.Core/ReplayParser.cs`
- `src/ReplayAssistant.Core/HumanIdentityMapper.cs`
- `tests/ReplayAssistant.Tests/Parser*.cs`, `ReplayHeaderTests.cs`, `TestData.cs`
- `testdata/README.md`

**Do not touch:** `StateStore`, HTTP client, watcher worker, updater, `Program.cs`.

**Depends on:** Stream A csproj/sln (or create Core csproj here if A has not).

**Parallel with:** C, D, E (different files).

---

## Stream C — SQLite + placeholder API

**Goal:** Slim Microsoft.Data.Sqlite; local replay status; identity queue; unique POST; retry/backlog.

**Owns:**

- `src/ReplayAssistant.Core/AppPaths.cs`
- `src/ReplayAssistant.Core/AppSettings.cs`
- `src/ReplayAssistant.Core/StateStore.cs`
- `src/ReplayAssistant.Core/IdentityApiClient.cs`
- `tests/ReplayAssistant.Tests/StateStoreTests.cs`
- `tests/ReplayAssistant.Tests/IdentityApiClientTests.cs`

**Do not touch:** parser files, FileSystemWatcher, updater.

**Depends on:** Core csproj (package refs: `Microsoft.Data.Sqlite`).

**Parallel with:** B, D, E.

---

## Stream D — Watcher, completion gate, process gate

**Goal:** Event-based Demos watch; size-stable complete detection; optional Fortnite process gate; catch-up enumeration; BelowNormal parse queue.

**Owns:**

- `src/ReplayAssistant.Core/ReplayCompletionGate.cs`
- `src/ReplayAssistant.Core/FortniteProcessGate.cs`
- `src/ReplayAssistant.Core/DemosWatcher.cs`
- `tests/ReplayAssistant.Tests/ReplayCompletionGateTests.cs`
- `tests/ReplayAssistant.Tests/CatchUpTests.cs`

**Do not touch:** parser internals, SQLite schema, updater, WinExe Program.

**Depends on:** `DemosFolder.cs` exists.

**Parallel with:** B, C, E.

---

## Stream E — Host, mutex, logging, install, updater

**Goal:** Silent boot, single-instance, file logs, Startup `.lnk` to `%LOCALAPPDATA%\ReplayAssistant\ReplayAssistant.exe`, GitHub Releases update + helper EXE.

**Owns:**

- `src/ReplayAssistant/Program.cs`
- `src/ReplayAssistant/Worker.cs`
- `src/ReplayAssistant/FileLoggerProvider.cs`
- `src/ReplayAssistant/HostLog.cs`
- `src/ReplayAssistant/appsettings.json`
- `src/ReplayAssistant.Core/SingleInstance.cs`
- `src/ReplayAssistant.Core/StartupShortcut.cs`
- `src/ReplayAssistant.Core/InstallLayout.cs`
- `src/ReplayAssistant.Core/GitHubUpdateChecker.cs`
- `src/ReplayAssistant.Update/**`
- `tests/ReplayAssistant.Tests/WinExeProjectTests.cs`
- `tests/ReplayAssistant.Tests/InstallAndUpdateTests.cs` (pure unit: version compare, unique batches)

**Do not touch:** parser mapping, SQLite SQL, completion-gate algorithm.

**Depends on:** Core types from B/C/D for Worker wiring (can stub interfaces if parallel).

**Sequential after:** B+C+D for a working Worker that parses and POSTs.

---

## Merge / verify order (sequential)

1. **A** — `dotnet restore` / solution loads.
2. **B** — parser unit tests; optional real Demos parse (current + old).
3. **C** — SQLite + fake HTTP tests.
4. **D** — completion-gate tests (growing file never ready; stable size becomes ready).
5. **E** — wire Worker; `dotnet build`; `dotnet test`; `publish.ps1`.
6. **Bakeoff write-up** — parse the 9 Demos files; record Shiqan results in `FINDINGS.md`. Do **not** vendor the GPL fork unless it beats Shiqan on current-season human IDs.

### Verify commands

```powershell
dotnet tool restore
dotnet csharpier format
dotnet test
.\publish.ps1
```

Confirm `ReplayAssistant.csproj` `OutputType` is `WinExe`. Run published EXE: no console. Task Manager name **Replay Assistant**. `--install-startup` writes Startup `.lnk` to the LocalAppData path.

---

## File collision rules

- One owner per path. If two streams need a type, put a small interface in Core owned by the first stream (`IStateStore`, `IReplayParser`).
- Do not reformat files you do not own (CSharpier at verify step).
- Do not copy `FnLobbyInfo` Kestrel, `wwwroot`, live PKCS7, `ClearEncrypted`, or Tracker/Overwolf code.
