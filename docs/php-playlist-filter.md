# Playlist filtering — port to PHP (NameFN)

The Windows EXE used to **allow-list** Battle Royale and Reload and **hold back** empty/unknown playlists. That is the wrong place for season tokens: a new island never leaves the PC until you ship a new EXE.

**EXE (thin):** parse completed files, drop bots / bad Epic ids, POST every replay that has `replay_id` + ≥1 human. It does **not** decide BR vs Creative.

**API:** deny **known junk** playlists. Empty string and unknown tokens **ingest**. Later you only edit `config/replay.php` / `PlatformNamespace::fromReplay`.

This document is the C# logic we ran (Ch7 S4 catalog: https://fortnite-stats.azurewebsites.net/Playlists) so PHP can reproduce the **deny** side with the same tokens, without copying the old allow-list as a POST gate.

---

## 1. Input

Replay field: `GameData.CurrentPlaylist` (string). Examples:

| Id | Meaning |
| --- | --- |
| `Playlist_Habanero_NoBuild_DashBerry_Solo` | Ranked Reload, Zero Build, DashBerry island, Solo |
| `Playlist_Habanero_NoBuild_JumpBear_Solo` | Ranked Reload, JumpBear |
| `Playlist_RopeSmileNoBuildSolo` | Pubs BR Zero Build, RopeSmile island |
| `Playlist_DefaultSolo` | Classic BR Solo |
| `Playlist_ShowdownTournament_FN_NPM_Solo` | Cash cup / FNCS-style (still BR) |
| `Playlist_ShowdownTournament_RE_DashBerrySolo_NPM` | Reload cash cup |
| `Playlist_PlaygroundV2` / `Playlist_Creative_*` | Creative |
| `Playlist_Habanero_SunflowerVKPlaySolo` | Ranked UEFN (VK Play) |
| `Playlist_DelMar_*` | Rocket Racing |
| *(empty / missing)* | Parser did not fill playlist (still ingest on API) |

Normalize: `strtolower(trim($playlist))`. Match with `str_contains` (needles below are already lowercase). **Order matters:** deny first, then you may classify Reload vs BR for metrics only — classification must **not** block ingest.

---

## 2. Do **not** copy the old EXE allow-list as a deny-of-unknown

The C# `PlaylistFilter` used to:

1. Deny drop-tokens → `Skipped`
2. Reload needles → `Reload` (**POST**)
3. `playlist_trios` exact / prefix → `BattleRoyale` (**POST**)
4. BR needles including `habanero` and `showdown` → `BattleRoyale` (**POST**)
5. Else → `Unknown` (**no POST**)

Step 5 lost weeks of matches when Epic shipped a new island name. PHP must **ingest** step 5.

Reload needles are **not** denials: `reload`, `dashberry`, `jumpbear`, `blastberry`.

---

## 3. Deny list (PHP should skip ingest: `skipped: playlist`, no `replay_ingests` row)

If **any** needle is a substring of the normalized playlist, deny. Source: `PlaylistFilter.DropTokens` in `src/ReplayAssistant.Core/PlaylistFilter.cs`.

| Needle | Why |
| --- | --- |
| `creative` | Creative / Discover |
| `playground` | Creative Playground |
| `papaya` | Party Royale |
| `delmar` | Rocket Racing (also `Playlist_DelMar_Habanero_*` — deny **before** treating `habanero` as ranked BR) |
| `juno` | Lego / Odysseus |
| `sparks` | Festival |
| `festival` | Festival |
| `figment` | Festival island / `Playlist_Habanero_Figment_*` |
| `pilgrim` | non-BR (Ballistic-era naming) |
| `campaign` / `saveourworld` / `stw` | Save the World |
| `vkplay` | UEFN ranked (`SunflowerVKPlay*`) — must deny **before** `sunflower` / `habanero` as BR |
| `forbiddenfruit` | UEFN cups |
| `melt` | Floor is Lava LTM |
| `toss` | Food Fight LTM |
| `respawn` | Team Rumble (`Playlist_Respawn_24`) |
| `solidgold` | LTM |
| `bigbattle` | 50v50-style (`Playlist_NoBuildBR_BigBattle`) — optional; we treated as junk |
| `itemtest` | internal |
| `bluecheese` | Late Game Arena test |
| `thanos` / `avengers` | LTM |

**Needle order in PHP:** run this deny list first. `vkplay` and `delmar` and `figment` must win over `habanero` / `sunflower`.

Suggested PHP:

```php
function replay_playlist_denied(?string $playlist): bool
{
    $p = strtolower(trim((string) $playlist));
    if ($p === '') {
        return false; // empty → ingest
    }
    static $deny = [
        'creative', 'playground', 'papaya', 'delmar', 'juno', 'sparks', 'festival',
        'figment', 'pilgrim', 'campaign', 'saveourworld', 'stw', 'vkplay',
        'forbiddenfruit', 'melt', 'toss', 'respawn', 'solidgold', 'bigbattle',
        'itemtest', 'bluecheese', 'thanos', 'avengers',
    ];
    foreach ($deny as $needle) {
        if (str_contains($p, $needle)) {
            return true;
        }
    }
    return false;
}
```

On deny: HTTP 200 with `{ "skipped": "playlist" }`, log  
`warning Replay ingest skipped playlist (replay_id, playlist, player_count)`.  
Do **not** insert `replay_ingests`. The EXE will not retry a 200; a **later API change does not backfill that replay unless the EXE is told to re-POST** (wipe local `posted_at` / delete `state.db`). If you need “deny now, accept later without touching EXEs”, return **4xx** so the client keeps `posted_at` null — product choice. Current EXE treats **any 2xx as posted**. Prefer **non-2xx** (e.g. 422) for skip-if-you-want-retry, **or** 200 skip if junk should never retry.

**NameFN note:** if skips are 200, a future allow of RopeSmile-like tokens will **not** replay old Creative files (good) **nor** old unknown-island files that were 200-skipped (bad). For unknown playlists you must **ingest**, not 200-skip.

---

## 4. Optional classify (metrics only — never gate ingest)

After deny-list miss:

**Reload** if substring: `reload`, `dashberry`, `jumpbear`, `blastberry`.

**Battle Royale** if:

- `$p === 'playlist_trios'` or `str_starts_with($p, 'playlist_trios_')`
- or substring: `ropesmile`, `punchberry`, `piperboot`, `matchmist`, `sourspawn`, `squareclub`, `sunflower`, `timberstake`, `defaultsolo`, `defaultduo`, `defaulttrio`, `defaultsquad`, `nobuildbr`, `showdown`, `habanero`

`habanero` = ranked (any mode). Check Reload needles **before** `habanero` so ranked Reload is Reload, ranked BR is BR.

`showdown` / `showdownalt` / `showdowntournament` = Arena, cash cups, FNCS. Combined with Reload island tokens → Reload cups.

Anything else (including `Playlist_RusticPepperSolo`): **unknown → still ingest**.

---

## 5. Platforms (API `PlatformNamespace::fromReplay`)

EXE sends **raw** Fortnite tokens (`WIN`, `PS5`, `XSX`, `AND`, `IOS`, `SWT2`, …) plus `PlayerName` when present. It does **not** map families anymore.

API should:

| Raw (uppercase) | Action |
| --- | --- |
| `WIN`, `WINDOWS`, `PC`, `EPIC` | Epic track, **unverified** name |
| `PSN`, `PS4`, `PS5`, `PS4PRO`, `PLAYSTATION` | PSN link, **never** Epic `current_name` |
| `XBL`, `XBOX`, `XB1`, `XSX`, `XSS`, … | Xbox link, never Epic name |
| `SWT`, `SWITCH`, `NSW`, `NINTENDO` | Nintendo |
| `AND`, `IOS`, `SWT2`, missing, anything else | Player by `epic_id` only; **strip name and platform**. Do **not** map AND/IOS to Epic names. Log `notice Replay ingest unknown platforms (replay_id, platforms)` |

Nickname without a **mapped** platform → ID only (same as strip).

Ignore `StreamerModeName` / `PlayerNameCustomOverride` if a client ever sent them (EXE does not). Custom override on disk was bot Simpsons labels in our Demos.

---

## 6. Why this is more specific than “keep BR and Reload”

- Ranked vs pubs vs cups is encoded in `Habanero` vs `Default*` vs `Showdown*`, not a separate field.
- Island names rotate every season; **deny Creative/Racing/Festival/Rumble/LTM**, do **not** deny unknown islands.
- `Habanero` + `VKPlay` is ranked Creative, not ranked BR — `vkplay` deny must run first.
- Stats site titles (e.g. “Ranked Reload” on `Habanero_NoBuild_RopeSmile_*`) are often wrong; use **id tokens**, not UI titles.
- `MapInfo` is empty in completed Demos; playlist is the only mode signal.

C# reference (classify + deny tokens, no longer used as a POST gate): `src/ReplayAssistant.Core/PlaylistFilter.cs`.
