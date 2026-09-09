# Changelog

All notable changes to the UnrankedSmurfs Account Exporter.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versioning is [semantic](https://semver.org/spec/v2.0.0.html).

The **export schema version is separate from the app version** and lives in
every exported file as `"version"`. It changes only when `accountData` changes
shape — see [docs/export-format.md](docs/export-format.md).

## [Unreleased]

### Known gaps

- The application icon is still the upstream project's artwork, and the in-app
  accent is still the fork's blue rather than UnrankedSmurfs gold. Tracked in
  [BRANDING-HANDOFF-PROMPT.md](BRANDING-HANDOFF-PROMPT.md).
- Nothing on unrankedsmurfs.com ingests the exported file yet; the import
  endpoint is a separate piece of work.

## [1.0.0] — unreleased

First release of the exporter, forked from
[League Account Manager](https://github.com/Ja-Sa-La/League-Account-Manager)
by [Ja-Sa-La](https://github.com/Ja-Sa-La) (MIT) and reduced to a single job.

### Added

- Capture the cosmetic inventory of the account signed into the League client:
  region, solo-queue tier, summoner level, Blue Essence, RP, owned champion
  keys and owned skin ids.
- Export those accounts as JSON in the shape the UnrankedSmurfs listing
  importer reads — schema `unrankedsmurfs.account-export`, version 1. Champion
  keys and skin ids are Riot's own numbers, which are already what the site's
  `champions.key` and `skins.id` are keyed on, so no name matching is involved
  anywhere.
- Re-capturing an account already in the list updates that row instead of
  adding a duplicate.
- Region normalisation from the client's inconsistent shard codes (`OC1`,
  `LA1`, `EUN1`) onto the site's region names. An unrecognised shard is passed
  through upper-cased rather than dropped.
- Window smoke tests that construct the real main window against the real
  application resources, and fail on any binding error WPF reports. XAML
  resource failures are a runtime concern, so a green compile was never
  evidence the app starts; now it is.
- A test asserting no credential or identity field can appear in an export.

### Security and privacy

- **The exporter never reads a username, password or email.** `AccountSnapshot`
  has no field to hold one, so credentials cannot reach the export file even by
  accident — the guarantee is structural, not a filter that can be forgotten.
- Six read-only LCU GETs, no writes. Nothing is uploaded; the export is a local
  file.

### Removed from upstream

- Credential storage (the CSV/AES-GCM account file) and login automation.
- Stealth login, which downloaded a certificate from the upstream author's own
  domain and wrote an entry into the Windows hosts file.
- Login-token redirect, report tool, disenchanter, champion buyer, friend
  manager, profile editor, Valorant support, and the LCU traffic proxies.
- The self-updating binary. Updates are a download from the Releases page, so
  the app never fetches and executes code on a user's behalf.
- A committed 17MB prebuilt upstream `.exe`.

### Changed

- Retargeted `net10.0-windows7.0` → `net8.0-windows`: LTS, and what upstream's
  own release already asked users to install.
- Assembly version now comes from the release tag rather than a T4 template
  that only incremented under Visual Studio.

[Unreleased]: https://github.com/UnrankedSmurfs/account-exporter/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/UnrankedSmurfs/account-exporter/releases/tag/v1.0.0
