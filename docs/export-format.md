# Export format

The file the exporter writes, and the contract the UnrankedSmurfs importer
reads. Version this document alongside `ExportWriter.Schema`.

## Envelope

```json
{
  "schema": "unrankedsmurfs.account-export",
  "version": 3,
  "generator": "UnrankedSmurfs Account Exporter 1.0.0",
  "exportedAt": "2026-09-09T10:24:31.0000000+00:00",
  "accounts": []
}
```

| Field | Type | Notes |
| --- | --- | --- |
| `schema` | string | Always `unrankedsmurfs.account-export`. Reject anything else. |
| `version` | int | Currently `3`. See [Version history](#version-history). |
| `generator` | string | Tool name and version, for support triage. |
| `exportedAt` | string | ISO 8601 round-trip (`o`) timestamp, UTC. |
| `accounts` | array | One entry per captured account. May be empty. |

## Account entry

```json
{
  "game": "league-of-legends",
  "accountData": {
    "region": "EUW",
    "rank": "GOLD",
    "level": 142,
    "blueEssence": 24500,
    "riotPoint": 1350,
    "champions": [1, 2, 3],
    "skins": [1000, 1001],
    "chromas": [103029, 103030],
    "summonerIcons": [7, 4090],
    "tftCompanions": [60001, 60009],
    "tftSkins": [60001004],
    "tftChromas": [60001305]
  }
}
```

`accountData` deliberately mirrors the shape the site's own appraisal wizard
produces, field name for field name — including the singular `riotPoint` — so
an imported account and a hand-filled one are indistinguishable downstream.

| Field | Type | Maps to |
| --- | --- | --- |
| `region` | string | `regions.name` — `NA`, `EUW`, `EUNE`, `OCE`, `BR`, `LAN`, `LAS`, `RU`, `TR`, `JP`, `PBE` |
| `rank` | string | Upper-case solo-queue tier, or `UNRANKED`. The site lower-cases it for `AccountProfile::RANK_ORDER`. |
| `level` | int | `AccountProfile`'s `level` |
| `blueEssence` | int | `AccountProfile`'s `blue_essence` |
| `riotPoint` | int | `AccountProfile`'s `riot_points` |
| `champions` | int[] | Riot champion keys, matching `champions.key` (Annie = 1) |
| `skins` | int[] | Riot skin ids, matching `skins.id` (Annie's base skin = 1000) |
| `chromas` | int[] | Riot chroma ids. No table on the site holds these yet — see below |
| `summonerIcons` | int[] | Riot summoner-icon ids, matching `summoner_icons.provider_id` |
| `tftCompanions` | int[] | Teamfight Tactics tacticians. Nothing on the site maps these yet |
| `tftSkins` | int[] | Tactician skins |
| `tftChromas` | int[] | Tactician chromas |

Every id array is sorted ascending and de-duplicated.

### Chromas are not skins

Riot does not give chromas a numbering space of their own. A chroma takes the
next free `championKey * 1000 + n` slot, right alongside the champion's real
skins — K/DA Ahri is `103028` and her chromas run from `103029`. So a chroma id
looks exactly like a skin id and matches no row in `skins`.

The exporter therefore keeps them apart. The client tags them
(`subInventoryType: "RECOLOR"` in the skin catalog) and the exporter splits on
that tag, so `skins` stays a list the site can resolve and `chromas` is a
separate count. An importer that does not care about chromas can ignore the
array entirely; what it must not do is merge the two back together.

**Caveat.** The `RECOLOR` tag has not been observed against a live client by
anyone who worked on this file — there is no League install on the machine this
is built on. The parser is written so that an absent or unrecognised tag files
the item as a skin, which is precisely what every owned item was before, so the
worst case is that `chromas` comes back empty and `skins` behaves as it did in
version 1. It is not a case where ids go missing.

### Teamfight Tactics is not League

The client files TFT tacticians under the same `CHAMPION` and `CHAMPION_SKIN`
inventory types it uses for League champions and skins. Read those routes
naively and a tactician counts as a champion and its skins as champion skins.

That is not a rounding error. On the first real capture — an EUW account,
exporter `0.9.0` — **63 of 236 "champions" and 226 of 767 "skins" were TFT**. A
listing built from that file would have advertised 767 skins when 539 were
League skins.

Riot gives skin ids as `championKey * 1000 + n`, so classifying a skin is one
question about its champion. League champion keys run below 1200; TFT
companions start at 60001. The exporter draws the line at **1200**, in the
59,000 keys of empty space between the two — the largest League key observed on
a real account was 950.

The TFT content is kept rather than dropped, in `tftCompanions`, `tftSkins` and
`tftChromas`. An account that owns 63 tacticians owns something, and the
exporter has no standing to decide it is worthless. It just must not be counted
as League.

**An importer reading a version 1 or 2 file must filter these itself** — those
files have the ids folded in, and no amount of care on the site's side can
recover which is which except by the same id rule.

### Profile icons

`summonerIcons` carries Riot's summoner-icon ids, which is what the website's
`summoner_icons.provider_id` column already stores. "Profile icon" is what a
player sees the thing called in the client; the two names mean the same thing.

### Why ids and not names

Riot's skin ids are the same numbers UnrankedSmurfs already keys its `skins`
table on, and the champion keys match `champions.key`. Exporting ids means the
importer needs no name matching, no slug normalisation, and no per-patch lookup
table — a skin released after the last deploy still imports as a valid id.

`AccountProfile::resolveChampionKeys()` accepts numeric entries as keys
directly, so `champions` needs no special handling on the site side.

## Version history

| Version | Change |
| --- | --- |
| 1 | `region`, `rank`, `level`, `blueEssence`, `riotPoint`, `champions`, `skins`. |
| 2 | Added `chromas` and `summonerIcons`. Additive only — every version 1 field kept its name, position and type, so a reader written against version 1 can read a version 2 file by ignoring the two new arrays. |
| 3 | Moved Teamfight Tactics content out of `champions`, `skins` and `chromas` into `tftCompanions`, `tftSkins` and `tftChromas`. **Not additive:** those three arrays keep their names and types but no longer carry the same ids. Versions 1 and 2 counted a tactician as a champion — roughly a quarter of both counts on a real account. Treat a version 2 file's `champions` and `skins` as upper bounds, not as League totals. |

An importer should accept any `version` it knows and reject one it does not,
rather than assuming the highest it has seen.

## Fields that are deliberately absent

No username, password, email, PUUID, summoner name or Riot ID appears anywhere
in the file. The exporter never reads them.

An importer should therefore **reject** any file containing a `username`,
`password`, `email` or `puuid` key inside `accountData`: a file carrying those
did not come from this tool.

## Edge cases

| Case | Behaviour |
| --- | --- |
| Unranked account | `rank` is `"UNRANKED"` |
| Region the tool does not know | passed through upper-cased rather than nulled, so a new shard is visible instead of silently lost |
| Client returned no wallet | `blueEssence` and `riotPoint` are `0` |
| Client returned no inventory | `champions` / `skins` / `chromas` / `summonerIcons` are `[]` |
| Client does not tag chromas | every owned item files as a skin; `chromas` is `[]` (see above) |
| Account owns no TFT content | `tftCompanions` / `tftSkins` / `tftChromas` are `[]` |

A capture that cannot read the current summoner fails outright and adds no row,
rather than writing a half-populated account.
