using Newtonsoft.Json.Linq;

namespace UnrankedSmurfs.AccountExporter.Lcu;

/// <summary>
///     Wallet balances, in the only two currencies a listing cares about.
/// </summary>
internal sealed record Wallet(int BlueEssence, int RiotPoints);

/// <summary>
///     Parsers for the handful of LCU responses this tool reads.
///     Every parser is total: a malformed or empty body yields null rather
///     than throwing, because a half-finished client start-up routinely
///     returns both.
/// </summary>
internal static class ApiResponseParser
{
    public static JObject? ParseSummoner(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var summoner = JObject.Parse(content);
            return summoner["summonerId"] != null ? summoner : null;
        }
        catch
        {
            return null;
        }
    }

    public static Wallet? ParseWallet(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var wallet = JObject.Parse(content);
            return new Wallet(
                wallet["lol_blue_essence"]?.ToObject<int>() ?? 0,
                wallet["RP"]?.ToObject<int>() ?? 0);
        }
        catch
        {
            return null;
        }
    }

    public static JToken? ParseRankedStats(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var rankedStats = JToken.Parse(content);
            return rankedStats["queueMap"] != null ? rankedStats : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     The solo-queue tier as a bare word ("GOLD", "UNRANKED").
    ///     Deliberately not the LP/W/L display string LAM used: the website
    ///     stores a tier and ranks listings on it, so divisions and LP would
    ///     only have to be stripped again on the other side.
    /// </summary>
    public static string ParseTier(JToken? rankedStats, string queueName = "RANKED_SOLO_5x5")
    {
        var tier = rankedStats?["queueMap"]?[queueName]?["tier"]?.ToString();
        return string.IsNullOrWhiteSpace(tier) ? "UNRANKED" : tier.Trim().ToUpperInvariant();
    }
}
