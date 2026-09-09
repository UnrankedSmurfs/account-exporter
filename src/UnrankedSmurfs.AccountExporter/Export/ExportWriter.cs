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
    public const int Version = 1;
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
