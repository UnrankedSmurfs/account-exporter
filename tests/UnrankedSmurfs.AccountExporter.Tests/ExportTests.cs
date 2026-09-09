using Newtonsoft.Json.Linq;
using UnrankedSmurfs.AccountExporter.Export;

namespace UnrankedSmurfs.AccountExporter.Tests;

public class ExportTests
{
    private static AccountSnapshot Sample() => new()
    {
        Region = "EUW",
        Rank = "GOLD",
        Level = 142,
        BlueEssence = 24500,
        RiotPoints = 1350,
        ChampionKeys = [1, 103, 84],
        SkinIds = [1000, 1001, 103000],
        ChromaIds = [103029, 103030],
        SummonerIconIds = [7, 4090],
    };

    [Fact]
    public void Export_matches_the_shape_the_website_importer_reads()
    {
        var json = JObject.Parse(ExportWriter.Serialize([Sample()]));

        Assert.Equal("unrankedsmurfs.account-export", json["schema"]!.Value<string>());
        Assert.Equal(2, json["version"]!.Value<int>());

        var account = json["accounts"]!.Single();
        Assert.Equal("league-of-legends", account["game"]!.Value<string>());

        var data = account["accountData"]!;
        Assert.Equal("EUW", data["region"]!.Value<string>());
        Assert.Equal("GOLD", data["rank"]!.Value<string>());
        Assert.Equal(142, data["level"]!.Value<int>());
        Assert.Equal(24500, data["blueEssence"]!.Value<int>());

        // Singular, matching the appraisal wizard's own field name. A rename
        // here silently zeroes the RP on every imported listing.
        Assert.Equal(1350, data["riotPoint"]!.Value<int>());

        Assert.Equal(new[] { 1, 103, 84 }, data["champions"]!.Values<int>().ToArray());
        Assert.Equal(new[] { 1000, 1001, 103000 }, data["skins"]!.Values<int>().ToArray());
        Assert.Equal(new[] { 103029, 103030 }, data["chromas"]!.Values<int>().ToArray());
        Assert.Equal(new[] { 7, 4090 }, data["summonerIcons"]!.Values<int>().ToArray());
    }

    /// <summary>
    ///     Version 2 added two arrays and changed nothing else. A reader written
    ///     against version 1 must still find every field it knew, in the same
    ///     place and the same shape, or the bump was breaking after all and the
    ///     importer needs a migration rather than a default.
    /// </summary>
    [Fact]
    public void Version_2_only_adds_to_what_version_1_promised()
    {
        var data = (JObject) JObject.Parse(ExportWriter.Serialize([Sample()]))["accounts"]!
            .Single()["accountData"]!;

        Assert.Equal(
            new[] { "region", "rank", "level", "blueEssence", "riotPoint", "champions", "skins", "chromas", "summonerIcons" },
            data.Properties().Select(p => p.Name).ToArray());
    }

    /// <summary>
    ///     Chroma ids sit in the same numeric space as skin ids — a chroma
    ///     takes the next free `championKey * 1000 + n` slot — so folding the
    ///     two together would put ids into `skins` that match no row in the
    ///     website's `skins` table.
    /// </summary>
    [Fact]
    public void Chromas_are_not_mixed_into_the_skin_list()
    {
        var data = JObject.Parse(ExportWriter.Serialize([Sample()]))["accounts"]!
            .Single()["accountData"]!;

        var skins = data["skins"]!.Values<int>().ToArray();

        Assert.DoesNotContain(103029, skins);
        Assert.DoesNotContain(103030, skins);
    }

    /// <summary>
    ///     The promise the whole tool is sold on. If this ever fails, the export
    ///     is leaking something a seller was told it would never touch.
    /// </summary>
    [Theory]
    [InlineData("username")]
    [InlineData("password")]
    [InlineData("email")]
    [InlineData("puuid")]
    [InlineData("summonerName")]
    [InlineData("summonerId")]
    [InlineData("riotId")]
    public void Export_never_contains_a_credential_or_identity_field(string forbidden)
    {
        var json = ExportWriter.Serialize([Sample(), Sample()]);

        Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_of_nothing_is_still_a_valid_envelope()
    {
        var json = JObject.Parse(ExportWriter.Serialize([]));

        Assert.Equal("unrankedsmurfs.account-export", json["schema"]!.Value<string>());
        Assert.Empty(json["accounts"]!);
    }

    [Theory]
    [InlineData("EUW", "EUW")]
    [InlineData("euw1", "EUW")]
    [InlineData("EUN1", "EUNE")]
    [InlineData("OC1", "OCE")]
    [InlineData("LA1", "LAN")]
    [InlineData("LA2", "LAS")]
    [InlineData("NA1", "NA")]
    public void Client_region_codes_map_onto_website_region_names(string reported, string expected)
    {
        Assert.Equal(expected, RegionNormalizer.Normalize(reported));
    }

    [Fact]
    public void An_unknown_shard_survives_upper_cased_rather_than_being_lost()
    {
        Assert.Equal("SG2", RegionNormalizer.Normalize("sg2"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_region_is_explicit_rather_than_empty(string? reported)
    {
        Assert.Equal("UNKNOWN", RegionNormalizer.Normalize(reported));
    }
}
