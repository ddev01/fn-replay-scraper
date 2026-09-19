# Parser findings

## Libraries compared

| Library | License | Used here |
| --- | --- | --- |
| NuGet `FortniteReplayReader` 3.1.0 (Shiqan / FortniteReplayDecompressor) | MIT | **Yes** — completed Demos, `ParseMode.Minimal` + timeout |
| [SL-x-TnT/FortniteReplayDecompressor](https://github.com/SL-x-TnT/FortniteReplayDecompressor) | GPL-3.0 | **No** — skip/`Ignore*` flags are nice, but the fork is GPL and historically lags Athena net-field maps. Current-season files already parse in 16–500ms with Shiqan; vendoring GPL is not worth it until the fork beats 3.1.0 on human IDs |
| Other Node/unmaintained C# parsers | mixed | No |

Cheap optimizations without the fork: completed-only (never live), copy with `FileShare.ReadWrite`, header `IsLive` skip, 20s timeout, `BelowNormal` worker, size-stable gate.

## Sample locations

- `%LOCALAPPDATA%\FortniteGame\Saved\Demos\*.replay` (9 files on the operator PC, 2026-09-16 and 2026-09-17)
- `F:\Dev\fn-lobby-info\testdata\UnsavedReplay-2026.09.17-20.45.30.replay` (same as a Demos file)
- Live slices under `fn-lobby-info\testdata\live\` are **not** parsed by this app

## Bakeoff (Shiqan 3.1.0, 2026-09-18)

All completed files produced human IDs. None were `IsLive`. Times include copy + Minimal parse.

| File | Bytes | Humans | ms | Notes |
| --- | ---: | ---: | ---: | --- |
| UnsavedReplay-2026.09.16-22.58.08.replay | 7,795,425 | 24 | 507 | Older Demos (Fortnite UI may refuse playback) |
| UnsavedReplay-2026.09.16-23.05.22.replay | 24,119,267 | 11 | 328 | Older Demos |
| UnsavedReplay-2026.09.16-23.22.25.replay | 10,820,700 | 40 | 158 | Older Demos |
| UnsavedReplay-2026.09.16-23.30.42.replay | 21,275,881 | 39 | 264 | Older Demos |
| UnsavedReplay-2026.09.17-20.45.30.replay | 3,223,030 | 24 | 52 | Current-season / gold |
| UnsavedReplay-2026.09.17-21.28.08.replay | 3,131,528 | 30 | 52 | Current-season |
| UnsavedReplay-2026.09.17-21.44.57.replay | 9,191,144 | 19 | 118 | Current-season |
| UnsavedReplay-2026.09.17-22.06.28.replay | 4,270,799 | 25 | 60 | Current-season |
| UnsavedReplay-2026.09.17-23.13.08.replay | 278,194 | 9 | 16 | Smallest current file |

Bots are excluded via `PlayerData.IsBot`. POST body is unique Epic ids only.
