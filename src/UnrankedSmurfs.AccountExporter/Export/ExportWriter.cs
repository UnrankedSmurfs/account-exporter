using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnrankedSmurfs.AccountExporter.Export;

/// <summary>
///     Serialises captured accounts into the JSON the UnrankedSmurfs listing
///     importer reads.
///
///     The `accountData` object mirrors the shape the site's own appraisal
///     wizard produces, field name for field name — including the singular
///     `riotPoint` — so an imported account and a hand-filled one are
///     indistinguishable downstream.
/// </summary>
internal static class ExportWriter
{
    public const string Schema = "unrankedsmurfs.account-export";
    /// <summary>
    ///     2 added `chromas` and `summonerIcons` to `accountData`. Both were
    ///     additive: a reader written against version 1 got every field it knew
    ///     about, in the same shape, and could ignore the two new arrays.
    ///
    ///     3 moved Teamfight Tactics content out of `champions`, `skins` and
    ///     `chromas` into `tftCompanions`, `tftSkins` and `tftChromas`. This
    ///     one is **not** additive, which is why it is a version rather than a
    ///     patch: the three original arrays keep their names and types but no
    ///     longer carry the same ids. Versions 1 and 2 counted a TFT tactician
    ///     as a champion and its skins as champion skins — on the first real
    ///     capture, 63 of 236 "champions" and 226 of 767 "skins". A reader that
    ///     assumes a version 2 file is League-only is reading numbers that are
    ///     roughly a quarter too high.
    /// </summary>
    public const int Version = 3;
    private const string Game = "league-of-legends";

    public static string Serialize(IEnumerable<AccountSnapshot> snapshots)
    {
        var accounts = new JArray();
        foreach (var snapshot in snapshots)
        {
            accounts.Add(new JObject
            {
                ["game"] = Game,
                ["accountData"] = new JObject
                {
                    ["region"] = snapshot.Region,
                    ["rank"] = snapshot.Rank,
                    ["level"] = snapshot.Level,
                    ["blueEssence"] = snapshot.BlueEssence,
                    ["riotPoint"] = snapshot.RiotPoints,
                    ["champions"] = new JArray(snapshot.ChampionKeys),
                    ["skins"] = new JArray(snapshot.SkinIds),
                    ["chromas"] = new JArray(snapshot.ChromaIds),
                    ["summonerIcons"] = new JArray(snapshot.SummonerIconIds),
                    // Teamfight Tactics, kept whole but kept apart: the site
                    // has no TFT catalog, and folding these into `champions`
                    // and `skins` is what made version 2's counts wrong.
                    ["tftCompanions"] = new JArray(snapshot.TftCompanionKeys),
                    ["tftSkins"] = new JArray(snapshot.TftSkinIds),
                    ["tftChromas"] = new JArray(snapshot.TftChromaIds),
                },
            });
        }

        var envelope = new JObject
        {
            ["schema"] = Schema,
            ["version"] = Version,
            ["generator"] = GeneratorName(),
            ["exportedAt"] = DateTimeOffset.UtcNow.ToString("o"),
            ["accounts"] = accounts,
        };

        return envelope.ToString(Formatting.Indented);
    }

    public static async Task WriteAsync(string path, IEnumerable<AccountSnapshot> snapshots,
        CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(path, Serialize(snapshots), cancellationToken).ConfigureAwait(false);
    }

    public static string SuggestedFileName(int accountCount)
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-dd");
        return $"unrankedsmurfs-accounts-{accountCount}-{stamp}.json";
    }

    private static string GeneratorName()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null
            ? "UnrankedSmurfs Account Exporter"
            : $"UnrankedSmurfs Account Exporter {version.Major}.{version.Minor}.{version.Build}";
    }
}
