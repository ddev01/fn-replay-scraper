---
name: unreal-fortnite-replays
description: >-
  Applies Unreal Engine vs Fortnite replay facts when reading completed
  `.replay` files, IsLive/chunks, parse modes, player ids, Oodle/encryption,
  or Demos-folder watcher strategy. Use when working on FortniteReplayReader,
  ParseMode, FileShare, identity mapping, or replay tests.
---

# Unreal / Fortnite replays (completed files)

This app parses **finished** Demos only. See [reference.md](reference.md).

1. Epic Replay System + Local File Streamer (UE 4.27).
2. FortniteReplayDecompressor parse modes; `PlayerData` / `FortPlayerState`.
3. Map lobby **ids** from the library — do not reimplement the UE container.

## Watcher / parse

- Watch `%LOCALAPPDATA%\FortniteGame\Saved\Demos`.
- Copy/read with `FileShare.ReadWrite`.
- Size unchanged for N seconds, then skip if header `IsLive`.
- `ParseMode.Minimal` + timeout. Library: NuGet `FortniteReplayReader` (MIT).
