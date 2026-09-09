namespace UnrankedSmurfs.AccountExporter.Export;

/// <summary>
///     One captured account, holding only what an UnrankedSmurfs listing
///     displays: where it plays, what it ranked, and what cosmetics it owns
///     — champions, skins, chromas and profile icons, with Teamfight Tactics
///     content counted separately from League's.
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
    ///     Riot chroma ids. Chromas share the skin numbering space rather than
    ///     having one of their own, so these are kept apart from
    ///     <see cref="SkinIds" /> instead of being folded in: the website's
    ///     `skins` table has no row for a chroma id.
    /// </summary>
    public required IReadOnlyList<int> ChromaIds { get; init; }

    /// <summary>
    ///     Riot summoner-icon ids, matching `summoner_icons.provider_id` on the
    ///     website. Called "profile icons" everywhere a player sees them.
    /// </summary>
    public required IReadOnlyList<int> SummonerIconIds { get; init; }

    /// <summary>
    ///     Teamfight Tactics companions ("tacticians"), and their skins and
    ///     chromas.
    ///
    ///     The client returns these from the same two routes it returns League
    ///     champions and skins from, so they have to be separated somewhere.
    ///     They are kept rather than dropped because an account that owns 63 of
    ///     them owns something, and throwing it away would be a decision this
    ///     tool has no standing to make — but they are kept apart, because
    ///     counting a tactician as a champion overstates what is for sale.
    /// </summary>
    public required IReadOnlyList<int> TftCompanionKeys { get; init; }

    public required IReadOnlyList<int> TftSkinIds { get; init; }

    public required IReadOnlyList<int> TftChromaIds { get; init; }

    /// <summary>
    ///     Bindable counts. The backing collections are arrays, which expose
    ///     `Length` rather than a public `Count`, so binding straight to
    ///     `ChampionKeys.Count` would silently resolve to nothing.
    /// </summary>
    public int ChampionCount => ChampionKeys.Count;

    public int SkinCount => SkinIds.Count;

    public int ChromaCount => ChromaIds.Count;

    public int SummonerIconCount => SummonerIconIds.Count;

    public int TftCompanionCount => TftCompanionKeys.Count;

    public int TftSkinCount => TftSkinIds.Count;

    /// <summary>"GOLD" reads as shouting in a table; "Gold" does not.</summary>
    public string RankDisplay => TitleCase(Rank);

    private static string TitleCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
