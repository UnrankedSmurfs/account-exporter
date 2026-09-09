namespace UnrankedSmurfs.AccountExporter.Export;

/// <summary>
///     Maps the region code the Riot client reports onto the codes the
///     UnrankedSmurfs `regions` table uses.
///
///     Riot is not consistent here: the client reports platform ids for some
///     shards ("OC1", "LA1") and plain region names for others ("EUW"), and
///     the two Latin-American shards collapse onto different website regions.
///     Unknown codes are passed through upper-cased so a new shard shows up
///     verbatim in the export instead of silently becoming null.
/// </summary>
internal static class RegionNormalizer
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NA"] = "NA",
        ["NA1"] = "NA",
        ["EUW"] = "EUW",
        ["EUW1"] = "EUW",
        ["EUNE"] = "EUNE",
        ["EUN1"] = "EUNE",
        ["OCE"] = "OCE",
        ["OC1"] = "OCE",
        ["BR"] = "BR",
        ["BR1"] = "BR",
        ["LAN"] = "LAN",
        ["LA1"] = "LAN",
        ["LAS"] = "LAS",
        ["LA2"] = "LAS",
        ["RU"] = "RU",
        ["RU1"] = "RU",
        ["TR"] = "TR",
        ["TR1"] = "TR",
        ["JP"] = "JP",
        ["JP1"] = "JP",
        ["PBE"] = "PBE",
        ["PBE1"] = "PBE",
    };

    public static string Normalize(string? clientRegion)
    {
        if (string.IsNullOrWhiteSpace(clientRegion))
            return "UNKNOWN";

        var trimmed = clientRegion.Trim();
        return Map.TryGetValue(trimmed, out var mapped) ? mapped : trimmed.ToUpperInvariant();
    }
}
