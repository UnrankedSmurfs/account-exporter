# Prompt: capture the cosmetics the exporter still ignores

> **Handoff.** Start the session in `/home/lcurva/projects/account-exporter`, then: *"Read COSMETICS-CAPTURE-HANDOFF-PROMPT.md and add the missing catalogs."*
>
> Written 2026-09-09, end of the session that branded the app, shipped `v0.9.0`, took the first real capture apart, and split Teamfight Tactics out of the League counts. Facts marked ✓ were machine-verified — against this repo, against a real export from a real account, or against CommunityDragon. **Facts marked ⚠ are inferences that nobody has checked against a live client, and §3 is about how to stop guessing.**

---

## Binding decisions

1. **Do not break the privacy guarantee to add a feature.** Every route this tool touches is a read-only inventory GET. `AccountSnapshot` has no field a credential could live in, and that is structural, not a filter. Any new endpoint must clear the same bar, and the README, the in-app privacy panel and `release.yml`'s notes must all be updated together — they are the same promise written three times, and one going stale makes the other two worthless.
2. **Never fold two namespaces into one array.** This is the mistake that cost a version bump already: TFT content was counted as League because both arrive from `CHAMPION_SKIN`. Companion ids run `1..176001` ✓ and collide outright with skin ids and champion keys, so every new category gets its own field. No exceptions, however tempting the symmetry.
3. **A count the site cannot resolve is worse than no count.** `summoner_icons` on the website has **0 rows**, so the 524 profile icons in the real capture resolve to nothing. Adding a category the site has no catalog for repeats that. Say so in the format doc when you add one.
4. **`docs/export-format.md` is a contract with the website**, not documentation of the code. Update it in the same commit, and bump `ExportWriter.Version` when `accountData` changes shape. `Docs-Account-Export-Import.md` in the website repo is the other side.
5. **You cannot build or run this locally.** No `dotnet`, no Windows, no League client. GitHub Actions is the compiler; `gh run watch` is the feedback loop. A green build is not evidence the window renders — that is what `WindowSmokeTests` is for.

## First ten minutes

1. `git log --oneline -3` — expect `7cca4d5` (the TFT split) at tip ✓.
2. `cat docs/export-format.md` — the contract, currently at schema **version 3** ✓.
3. `src/UnrankedSmurfs.AccountExporter/Export/RiotIds.cs` — how League and TFT are told apart, and the precedent for any new namespace rule.
4. `src/UnrankedSmurfs.AccountExporter/Lcu/LcuRequestLog.cs` — **read this before anything else.** §3 depends on it.

---

## 1. What is captured today ✓

Seven read-only GETs. Two of them are catalogs:

| Endpoint | Gives |
| --- | --- |
| `/lol-summoner/v1/current-summoner` | summoner id (discarded), level |
| `/riotclient/region-locale` | region |
| `/lol-inventory/v1/wallet?currencyTypes=…` | BE, RP |
| `/lol-ranked/v1/current-ranked-stats` | solo-queue tier |
| `/lol-champions/v1/inventories/{id}/champions-minimal` | champions **+ TFT Chibis** |
| `/lol-catalog/v1/items/CHAMPION_SKIN` | skins, chromas, **+ TFT Chibi skins** |
| `/lol-catalog/v1/items/SUMMONER_ICON` | profile icons |

Catalog reads follow one shape: `GET /lol-catalog/v1/items/{TYPE}` returns an array of items with `itemId`, `owned` and `subInventoryType`. `CatalogParser.OwnedItemIds()` already handles the flat case and is total — a half-started client answers with an empty body or a bare error object, and neither costs the user a capture.

### The TFT content we have is accidental

Chibi champions arrive because Riot files them in `champion-summary.json` alongside real champions, so `champions-minimal` returns them. They are **not** Little Legends. Verified against CommunityDragon ✓:

- the rule is exactly `id = 60000 + championKey`, on all 63 in the real capture ✓
- their alias is the champion's with a `Jade_` prefix — `Jade_Annie` (60001), `Jade_Wukong` (60062) ✓
- every one had its League champion owned as well ✓

`RiotIds` draws the line at 1200, in the 59,000 keys of empty space between the largest real champion key (950) and the smallest Chibi (60001) ✓. That still holds; the `60000 +` rule is a tighter description of the same set, and if you ever need to name a Chibi rather than merely exclude it, that is the rule to use.

## 2. What is not captured ✓

Sizes are entry counts in CommunityDragon's catalogs — the whole game, not one account.

| Content | CDragon catalog | Entries | Id range | Notes |
| --- | --- | --- | --- | --- |
| **Little Legends** | `companions.json` | **2017** | 1–176001 | carries `TFTRarity` and **star level 1–3** ✓ |
| **Emotes** | `summoner-emotes.json` | **2347** | 0–10133 | League *and* TFT |
| **TFT damage skins** | `tftdamageskins.json` | 350 | 1–258003 | has `level` |
| **Ward skins** | `ward-skins.json` | 265 | 0–267 | `isLegacy`, `rarities` |
| **TFT arenas** | `tftmapskins.json` | 138 | 1–1034 | `TFTRarity` |

**Little Legends are the interesting one.** Unlike Chibis they carry rarity *and* star level, so a 3-star Mythic is a genuinely scarce object — the TFT content with resale value. 157 of the 2017 are TFT-exclusive ✓; the rest are shared.

**Ward skins and emotes are League**, and your buyers are League buyers. On value-per-hour they probably beat TFT arenas and damage skins.

Also not captured, and not in a catalog: **Eternals / statstones**, hextech chest and key counts, and event-pass state.

## 3. Stop guessing: record the payloads first ⚠

**Do this before writing any capture code.**

The LCU catalog type names below are inferences from Riot's naming conventions. **None has been observed on a live client**, because no session has ever seen a raw LCU body — the machine this is built on has no League install:

- ⚠ `COMPANION` — Little Legends
- ⚠ `WARD_SKIN` — ward skins
- ⚠ `EMOTE` — emotes
- ⚠ `TFT_MAP_SKIN` — arenas
- ⚠ `TFT_DAMAGE_SKIN` — damage skins

Guessing five endpoint names and shipping is how you get a release that silently exports five empty arrays. Every one of them fails *soft* — `CatalogParser` treats a 404 as "owns nothing" — so the failure looks exactly like an account that owns none of it.

**`LcuRequestLog` already records every request and response body** — 1000 entries, 200KB each — and is already wired into `LcuClient` ✓. It just has no way out. Give it one, and a single capture on a real account confirms all five names, their `subInventoryType` values, and whether ownership is `owned` or something else.

**The sharp edge.** `/lol-summoner/v1/current-summoner` returns `displayName`, `puuid` and `accountId`, so that log holds identity data in memory today. Harmless while it never leaves the process. A naive "save log" button writes credentials-adjacent data to disk and breaks the promise the tool is sold on. So:

- scrub on the way out — allowlist the fields a diagnostic needs, do not blocklist the ones it must not have;
- **write the scrubber's test first**, and assert against the same forbidden-field list `ExportTests` uses;
- put it behind a deliberate action with a plain warning, not a silent auto-dump;
- never write it next to the export by default.

`ExportTests.Export_never_contains_a_credential_or_identity_field` is the model to copy: it greps the serialized output, so it survives a rewrite of whatever produced it.

## 4. Recommended order

1. **The scrubbed diagnostic log** (§3). Nothing else is safe to build on inference.
2. **One real capture** with it. Confirms all five type names at once.
3. **Little Legends.** One GET, own field, the TFT content with value.
4. **Ward skins and emotes.** Two GETs, League content.
5. **TFT arenas and damage skins**, if anyone asks for them.

Steps 3–5 are each one endpoint, one array on `AccountSnapshot`, one key in `ExportWriter`, one row in the format doc, one column in the grid. `SUMMONER_ICON` is the worked example: `CatalogParser.OwnedItemIds()` already does the parsing.

**Schema version 4** should carry all of them at once rather than one bump per category. Additive this time — `champions`, `skins` and `chromas` keep their meaning from version 3 — so a version 3 reader stays correct.

**Ask the founder which categories sellers are actually asked about before building all five.** The exporter's value is that a listing is honest and complete, not that it is exhaustive; five arrays the site cannot resolve are five more numbers with nothing behind them (§3 of the website's `Docs-Account-Export-Import.md`).

## 5. Acceptance

- [ ] The diagnostic log exists, is scrubbed, and the scrubber has a test written against the forbidden-field list.
- [ ] No inferred endpoint name ships unconfirmed — either observed on a live client, or documented as ⚠ in `docs/export-format.md`.
- [ ] Each new category is its own field. Nothing is folded into `champions`, `skins` or `chromas`.
- [ ] `ExportWriter.Version` bumped, `docs/export-format.md` updated in the same commit, version history row added.
- [ ] README table, in-app privacy panel and `release.yml` release notes all list the same categories.
- [ ] CI green, 0 warnings 0 errors, and the test count went up.
- [ ] A real capture confirms the new arrays are non-empty on an account that owns the content.

## Out of scope

- **The website's ingestion.** `Docs-Account-Export-Import.md` and `ACCOUNT-IMPORT-HANDOFF-PROMPT.md` in `/home/lcurva/projects/website` cover it. Adding a field here does not oblige anyone there.
- **Populating the site's catalog tables.** `summoner_icons` is empty and `chromas` unpopulated; both are website work.
- **Code-signing.** Real, unrelated, founder's call.
- **`v0.10.0`.** The TFT split is merged to master but not tagged ✓. Cut it before starting here, so the fix that is already done reaches users rather than waiting behind this work.
