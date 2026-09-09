using System.Net.Http;
using Newtonsoft.Json.Linq;
using NLog;
using UnrankedSmurfs.AccountExporter.Lcu;

namespace UnrankedSmurfs.AccountExporter.Export;

internal sealed record CaptureResult(AccountSnapshot? Snapshot, string? Error)
{
    public bool Succeeded => Snapshot != null;

    public static CaptureResult Fail(string error) => new(null, error);

    public static CaptureResult Ok(AccountSnapshot snapshot) => new(snapshot, null);
}

/// <summary>
///     Reads the cosmetic inventory of whichever account is currently signed
///     into the League client, over the local LCU API.
///
///     Seven read-only GETs, no writes, and no credential endpoints are
///     touched. Chromas cost no request of their own: the client files them in
///     the skin catalog, so they arrive on a call already being made. The
///     summoner id is fetched because the champion-inventory route is keyed by
///     it, and is discarded once that call returns.
/// </summary>
internal static class AccountCapture
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string SummonerEndpoint = "/lol-summoner/v1/current-summoner";
    private const string RegionEndpoint = "/riotclient/region-locale";
    private const string WalletEndpoint = "/lol-inventory/v1/wallet?currencyTypes=[%22RP%22,%22lol_blue_essence%22]";
    private const string RankedEndpoint = "/lol-ranked/v1/current-ranked-stats";
    private const string SkinCatalogEndpoint = "/lol-catalog/v1/items/CHAMPION_SKIN";
    private const string SummonerIconCatalogEndpoint = "/lol-catalog/v1/items/SUMMONER_ICON";

    public static async Task<CaptureResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        var summonerBody = await GetAsync(SummonerEndpoint, cancellationToken);
        var summoner = summonerBody == null ? null : ApiResponseParser.ParseSummoner(summonerBody);
        if (summoner == null)
            return CaptureResult.Fail(
                "Could not read the current summoner. Make sure the League client is running and fully signed in.");

        var summonerId = summoner["summonerId"]?.ToString();
        if (string.IsNullOrWhiteSpace(summonerId))
            return CaptureResult.Fail("The League client did not report a summoner id yet. Wait for it to finish loading.");

        var level = summoner["summonerLevel"]?.ToObject<int?>() ?? 0;

        var regionBody = await GetAsync(RegionEndpoint, cancellationToken);
        var region = RegionNormalizer.Normalize(TryParseToken(regionBody)?["region"]?.ToString());

        var walletBody = await GetAsync(WalletEndpoint, cancellationToken);
        var wallet = walletBody == null ? null : ApiResponseParser.ParseWallet(walletBody);

        var rankedBody = await GetAsync(RankedEndpoint, cancellationToken);
        var rank = ApiResponseParser.ParseTier(rankedBody == null ? null : ApiResponseParser.ParseRankedStats(rankedBody));

        var championKeys = await GetOwnedChampionKeysAsync(summonerId, cancellationToken);
        var skins = CatalogParser.OwnedSkinsAndChromas(await GetAsync(SkinCatalogEndpoint, cancellationToken));
        var summonerIconIds = CatalogParser.OwnedItemIds(
            await GetAsync(SummonerIconCatalogEndpoint, cancellationToken));

        return CaptureResult.Ok(new AccountSnapshot
        {
            Region = region,
            Rank = rank,
            Level = level,
            BlueEssence = wallet?.BlueEssence ?? 0,
            RiotPoints = wallet?.RiotPoints ?? 0,
            ChampionKeys = championKeys,
            SkinIds = skins.SkinIds,
            ChromaIds = skins.ChromaIds,
            SummonerIconIds = summonerIconIds,
        });
    }

    /// <summary>
    ///     Owned champions as Riot keys. `champions-minimal` lists every
    ///     champion in the game, so ownership has to be filtered here rather
    ///     than assumed from the response length.
    /// </summary>
    private static async Task<IReadOnlyList<int>> GetOwnedChampionKeysAsync(
        string summonerId, CancellationToken cancellationToken)
    {
        var body = await GetAsync($"/lol-champions/v1/inventories/{summonerId}/champions-minimal", cancellationToken);
        if (body == null) return [];

        // An error payload comes back as an object, a good response as an array.
        if (body.TrimStart().StartsWith('{')) return [];

        JArray parsed;
        try
        {
            parsed = JArray.Parse(body);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Could not parse the champion inventory");
            return [];
        }

        var keys = new SortedSet<int>();
        foreach (var champion in parsed)
        {
            if (champion["ownership"]?["owned"]?.ToObject<bool?>() != true) continue;

            var key = champion["id"]?.ToObject<int?>();
            // Champion id 0 is the "None" placeholder the client returns.
            if (key is > 0) keys.Add(key.Value);
        }

        return [.. keys];
    }

    private static async Task<string?> GetAsync(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            if (await LcuClient.Connector("league", "get", endpoint, "", cancellationToken) is not HttpResponseMessage response)
                return null;

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Debug("LCU {Endpoint} returned {Status}", endpoint, (int)response.StatusCode);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(body) ? null : body;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "LCU request to {Endpoint} failed", endpoint);
            return null;
        }
    }

    private static JToken? TryParseToken(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            return JToken.Parse(body);
        }
        catch
        {
            return null;
        }
    }
}
