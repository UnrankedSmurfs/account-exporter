namespace UnrankedSmurfs.AccountExporter.Export;

/// <summary>
///     One captured account, holding only what an UnrankedSmurfs listing
///     displays: where it plays, what it ranked, and what cosmetics it owns.
///
///     There is deliberately no username, password, email, PUUID or summoner
///     name on this type. The capture pipeline never reads those fields, so
///     they cannot reach the export file even by accident — the guarantee is
///     structural rather than a filter someone can forget to apply.
/// </summary>
internal sealed class AccountSnapshot
{
    /// <summary>Website region code: EUW, EUNE, NA, OCE, BR, LAN, LAS, RU, TR, JP, PBE.</summary>
    public required string Region { get; init; }

    /// <summary>Solo-queue tier as a bare word, e.g. "GOLD" or "UNRANKED".</summary>
    public required string Rank { get; init; }

    public required int Level { get; init; }

    public required int BlueEssence { get; init; }

    public required int RiotPoints { get; init; }

    /// <summary>Riot champion keys (Annie = 1), matching `champions.key` on the website.</summary>
    public required IReadOnlyList<int> ChampionKeys { get; init; }

    /// <summary>Riot skin ids (Annie's base skin = 1000), matching `skins.id` on the website.</summary>
    public required IReadOnlyList<int> SkinIds { get; init; }

    /// <summary>
    ///     Bindable counts. The backing collections are arrays, which expose
    ///     `Length` rather than a public `Count`, so binding straight to
    ///     `ChampionKeys.Count` would silently resolve to nothing.
    /// </summary>
    public int ChampionCount => ChampionKeys.Count;

    public int SkinCount => SkinIds.Count;

    /// <summary>"GOLD" reads as shouting in a table; "Gold" does not.</summary>
    public string RankDisplay => TitleCase(Rank);

    private static string TitleCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
