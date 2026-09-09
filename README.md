# UnrankedSmurfs Account Exporter

Export the cosmetic inventory of your League of Legends accounts to a `.json`
file you can upload to an [UnrankedSmurfs](https://unrankedsmurfs.com) listing —
no screenshots, no typing out champion lists by hand.

Open source, MIT licensed, and maintained by UnrankedSmurfs.

---

## What it reads

Exactly seven things, per account:

| Field | Example |
| --- | --- |
| Region | `EUW` |
| Solo-queue rank | `GOLD` |
| Summoner level | `142` |
| Blue Essence | `24500` |
| Riot Points | `1350` |
| Owned champions | Riot champion keys |
| Owned skins | Riot skin ids |

## What it does not read

**Your username, password and email are never read.** They are not collected,
not stored, and not written to the export file. This is structural, not a
setting: the app only ever calls six read-only inventory endpoints on the
local League client, and the type that holds a captured account has no field
to put a credential in.

It also does not:

- upload anything anywhere — the export is a file, and you choose who to send it to
- log you in, or automate logging in
- store a list of your accounts between runs
- modify your League client, your account, or your `hosts` file

Nothing leaves your PC unless you upload the file yourself.

## How to use it

1. Download the latest `UnrankedSmurfs.AccountExporter.exe` from
   [Releases](https://github.com/UnrankedSmurfs/account-exporter/releases).
2. Make sure the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
   is installed.
3. Start the League client and sign in to the account you want to export.
4. Run the exporter and press **Capture current account**.
5. To add more accounts, sign in to the next one in the League client and press
   Capture again.
6. Press **Export .json** and upload the file to your UnrankedSmurfs listing.

## Export format

See [docs/export-format.md](docs/export-format.md) for the full schema.

```json
{
  "schema": "unrankedsmurfs.account-export",
  "version": 1,
  "generator": "UnrankedSmurfs Account Exporter 1.0.0",
  "exportedAt": "2026-09-09T10:24:31.0000000+00:00",
  "accounts": [
    {
      "game": "league-of-legends",
      "accountData": {
        "region": "EUW",
        "rank": "GOLD",
        "level": 142,
        "blueEssence": 24500,
        "riotPoint": 1350,
        "champions": [1, 2, 3],
        "skins": [1000, 1001]
      }
    }
  ]
}
```

## Build from source

Requires Windows and the .NET 8 SDK (WPF does not build on Linux or macOS).

```powershell
git clone https://github.com/UnrankedSmurfs/account-exporter.git
cd account-exporter
dotnet build src/UnrankedSmurfs.AccountExporter/UnrankedSmurfs.AccountExporter.csproj -c Release
dotnet run --project src/UnrankedSmurfs.AccountExporter/UnrankedSmurfs.AccountExporter.csproj
```

## Is this allowed?

The exporter reads your own account's inventory through the League Client
Update (LCU) API that the client itself uses locally. It does not automate
gameplay, does not touch the game process, and performs no writes of any kind.

Selling or buying League accounts is against Riot's Terms of Service. This tool
does not sell accounts and takes no position on that — it reads an inventory and
writes a file. What you do with the file is your decision.

## Credits

Forked from [League Account Manager](https://github.com/Ja-Sa-La/League-Account-Manager)
by [Ja-Sa-La](https://github.com/Ja-Sa-La), which is MIT licensed. The LCU
connection layer (`src/UnrankedSmurfs.AccountExporter/Lcu/`) is their work,
and the inventory endpoints this tool reads were mapped by that project first.
Thank you.

This fork strips the upstream account manager down to a single job — read
cosmetics, write JSON — and removes credential storage, login automation,
stealth login, the report tool, the disenchanter, and all client-modifying
features. If you want those, use the excellent upstream project instead.

## License

[MIT](LICENSE.md).
