# Replay docs and Fortnite deltas (completed-only app)

Fetch these at work time. Epic pages describe **stock Unreal**. Fortnite behavior is the delta list below.

## Epic (container / streamers)

- [UE 4.27 Replay System](https://docs.unrealengine.com/4.27/en-US/TestingAndOptimization/ReplaySystem/)
- [UE 4.27 Streamers](https://docs.unrealengine.com/4.27/en-US/TestingAndOptimization/ReplaySystem/Streamers/) (Local File Streamer)

## FortniteReplayDecompressor

- Repo: [Shiqan/FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor)
- Fork (GPL, often lagging net-fields): [SL-x-TnT/FortniteReplayDecompressor](https://github.com/SL-x-TnT/FortniteReplayDecompressor)
- Parse modes: [introduction](https://fortnitereplaydecompressor.readthedocs.io/en/latest/introduction/)
- NuGet: `FortniteReplayReader` 3.1.0 (MIT, `net10.0`)

Local completed-replay folder: `%LOCALAPPDATA%\FortniteGame\Saved\Demos`

Samples used by `fn-lobby-info` tests: `F:\Dev\fn-lobby-info\testdata\` (gitignored `.replay` binaries). This app’s tests also look at the live Demos folder.

## Verified

| Fact | Implication here |
| --- | --- |
| Header then chunks | `ReplayHeader.TryInspect` |
| `IsLive` while recording | **Do not parse** |
| File grows; last chunk may truncate | Wait until size is stable |
| AES on completed files | Stock `ReplayReader` decrypts finished files |
| Live `IsLive && Encrypted` | Out of scope; wait for the completed save |
| `EventsOnly` skips player list | Use `Minimal` |
| Parser can hang | Hard timeout |

## Do not assume

- Live header key decrypts in-progress ReplayData.
- Stale C# forks match current-season net-field maps.
- Shipping `Oodle.dll` from Fortnite is required (NuGet uses OozSharp).
