using Newtonsoft.Json.Linq;
using NLog;

namespace UnrankedSmurfs.AccountExporter.Export;

/// <summary>
///     What one <c>/lol-catalog/v1/items/CHAMPION_SKIN</c> response says the
///     account owns, split into the two things the website counts separately.
/// </summary>
internal sealed record OwnedSkinCatalog(IReadOnlyList<int> SkinIds, IReadOnlyList<int> ChromaIds);

/// <summary>
///     Reads the LCU catalog responses (<c>/lol-catalog/v1/items/{type}</c>).
///
///     Kept out of <c>Lcu/</c> and free of any transport concern so it can be
///     unit-tested against recorded payloads: there is no League client on the
///     machine this project is built on, so a parser that could only be
///     exercised through a live socket could not be exercised at all.
///
///     Every method is total. A half-started client answers these routes with
///     an empty body, a bare error object, or nothing at all, and none of
///     those should cost the user a capture.
/// </summary>
internal static class CatalogParser
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Riot files chromas in the skin catalog rather than a catalog of
    ///     their own, tagged by sub-inventory type. `RECOLOR` is the value the
    ///     client uses; `CHROMA` is accepted alongside it because the two have
    ///     been used interchangeably across LCU versions and neither costs
    ///     anything to allow.
    /// </summary>
    private static readonly string[] ChromaSubInventoryTypes = ["RECOLOR", "CHROMA"];

    /// <summary>Owned item ids from a flat catalog, ascending and de-duplicated.</summary>
    public static IReadOnlyList<int> OwnedItemIds(string? body)
    {
        var items = Parse(body);
        if (items == null) return [];

        var ids = new SortedSet<int>();
        foreach (var item in items)
        {
            if (!IsOwned(item)) continue;

            var id = ItemId(item);
            if (id is > 0) ids.Add(id.Value);
        }

        return [.. ids];
    }

    /// <summary>
    ///     Owned skins and owned chromas from the single skin-catalog response.
    ///
    ///     Chroma ids share Riot's skin numbering — a chroma of a champion's
    ///     skin simply takes the next free `championKey * 1000 + n` slot — so
    ///     an unsplit list puts ids into `skins` that match no row in the
    ///     website's `skins` table. Splitting them is therefore a correctness
    ///     fix as much as a feature.
    ///
    ///     The split has not been observed against a live client (see the class
    ///     remarks), so it is written to degrade into the previous behaviour
    ///     rather than to guess: an entry is treated as a chroma only when the
    ///     client explicitly says so. A missing, empty or unrecognised
    ///     `subInventoryType` counts as a skin, which is exactly what every
    ///     owned entry counted as before this method existed.
    /// </summary>
    public static OwnedSkinCatalog OwnedSkinsAndChromas(string? body)
    {
        var items = Parse(body);
        if (items == null) return new OwnedSkinCatalog([], []);

        var skins = new SortedSet<int>();
        var chromas = new SortedSet<int>();

        foreach (var item in items)
        {
            if (!IsOwned(item)) continue;

            var id = ItemId(item);
            if (id is not > 0) continue;

            var subType = item["subInventoryType"]?.ToString();
            var isChroma = subType != null
                && ChromaSubInventoryTypes.Contains(subType.Trim(), StringComparer.OrdinalIgnoreCase);

            (isChroma ? chromas : skins).Add(id.Value);
        }

        return new OwnedSkinCatalog([.. skins], [.. chromas]);
    }

    /// <summary>
    ///     A catalog response, or null for every way the client declines to
    ///     give one. A good response is a JSON array; an error payload comes
    ///     back as an object, which is why the shape is checked before the
    ///     parse rather than after it.
    /// </summary>
    private static JArray? Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        if (body.TrimStart().StartsWith('{')) return null;

        try
        {
            return JArray.Parse(body);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Could not parse a catalog response");
            return null;
        }
    }

    private static bool IsOwned(JToken item) => item["owned"]?.ToObject<bool?>() == true;

    private static int? ItemId(JToken item) => item["itemId"]?.ToObject<int?>();
}
