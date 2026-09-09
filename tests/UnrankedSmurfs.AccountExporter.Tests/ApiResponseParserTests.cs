using UnrankedSmurfs.AccountExporter.Lcu;

namespace UnrankedSmurfs.AccountExporter.Tests;

public class ApiResponseParserTests
{
    [Fact]
    public void A_ranked_account_reports_its_tier()
    {
        var stats = ApiResponseParser.ParseRankedStats(
            """{"queueMap":{"RANKED_SOLO_5x5":{"tier":"GOLD","division":"II","leaguePoints":42}}}""");

        Assert.Equal("GOLD", ApiResponseParser.ParseTier(stats));
    }

    [Fact]
    public void An_unranked_account_reports_UNRANKED_rather_than_an_empty_string()
    {
        var stats = ApiResponseParser.ParseRankedStats(
            """{"queueMap":{"RANKED_SOLO_5x5":{"tier":"","division":"NA","leaguePoints":0}}}""");

        Assert.Equal("UNRANKED", ApiResponseParser.ParseTier(stats));
    }

    [Fact]
    public void A_missing_queue_map_is_UNRANKED_not_a_crash()
    {
        Assert.Equal("UNRANKED", ApiResponseParser.ParseTier(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{\"unexpected\":true}")]
    public void A_half_started_client_yields_null_rather_than_throwing(string body)
    {
        // The client returns all of these while it is still coming up.
        Assert.Null(ApiResponseParser.ParseRankedStats(body));
        Assert.Null(ApiResponseParser.ParseSummoner(body));
    }

    [Fact]
    public void A_wallet_is_read_in_both_currencies()
    {
        var wallet = ApiResponseParser.ParseWallet("""{"lol_blue_essence":24500,"RP":1350}""");

        Assert.NotNull(wallet);
        Assert.Equal(24500, wallet.BlueEssence);
        Assert.Equal(1350, wallet.RiotPoints);
    }

    [Fact]
    public void A_wallet_missing_a_currency_reads_it_as_zero()
    {
        var wallet = ApiResponseParser.ParseWallet("""{"RP":1350}""");

        Assert.NotNull(wallet);
        Assert.Equal(0, wallet.BlueEssence);
        Assert.Equal(1350, wallet.RiotPoints);
    }
}
