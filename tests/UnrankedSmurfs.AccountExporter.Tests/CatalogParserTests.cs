using UnrankedSmurfs.AccountExporter.Export;

namespace UnrankedSmurfs.AccountExporter.Tests;

/// <summary>
///     The chroma split is the one piece of this tool that reads a field
///     nobody here has seen a live client emit — there is no League client on
///     the machine this is built on. These tests pin the behaviour against
///     recorded-shape payloads, and in particular pin what happens when the
///     field is missing, so the unverified case is the safe one.
/// </summary>
public class CatalogParserTests
{
    [Fact]
    public void Owned_skins_and_chromas_come_back_in_separate_lists()
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(
            """
            [
              {"itemId":1000,"owned":true,"subInventoryType":""},
              {"itemId":103000,"owned":true,"subInventoryType":"BASE"},
              {"itemId":103029,"owned":true,"subInventoryType":"RECOLOR"},
              {"itemId":103030,"owned":true,"subInventoryType":"RECOLOR"}
            ]
            """);

        Assert.Equal(new[] { 1000, 103000 }, catalog.SkinIds);
        Assert.Equal(new[] { 103029, 103030 }, catalog.ChromaIds);
    }

    /// <summary>
    ///     Both axes at once, which is the case the client actually presents:
    ///     skin-versus-chroma comes from `subInventoryType`, League-versus-TFT
    ///     from the id. Ids are real ones from the first real capture.
    /// </summary>
    [Fact]
    public void The_catalog_splits_on_both_axes_at_once()
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(
            """
            [
              {"itemId":103028,"owned":true,"subInventoryType":""},
              {"itemId":103029,"owned":true,"subInventoryType":"RECOLOR"},
              {"itemId":60001004,"owned":true,"subInventoryType":""},
              {"itemId":60001305,"owned":true,"subInventoryType":"RECOLOR"}
            ]
            """);

        Assert.Equal(new[] { 103028 }, catalog.SkinIds);
        Assert.Equal(new[] { 103029 }, catalog.ChromaIds);
        Assert.Equal(new[] { 60001004 }, catalog.TftSkinIds);
        Assert.Equal(new[] { 60001305 }, catalog.TftChromaIds);
    }

    /// <summary>
    ///     The regression this whole split exists for. Through v0.9.0 a TFT
    ///     tactician skin landed in `skins`, so a real account advertised 767
    ///     skins when 539 were League skins.
    /// </summary>
    [Fact]
    public void A_tactician_skin_never_lands_in_the_league_skin_list()
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(
            """[{"itemId":60117037,"owned":true},{"itemId":1004,"owned":true}]""");

        Assert.Equal(new[] { 1004 }, catalog.SkinIds);
        Assert.Equal(new[] { 60117037 }, catalog.TftSkinIds);
    }

    [Fact]
    public void Unowned_items_are_in_neither_list()
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(
            """
            [
              {"itemId":1000,"owned":true},
              {"itemId":1001,"owned":false},
              {"itemId":103029,"owned":false,"subInventoryType":"RECOLOR"}
            ]
            """);

        Assert.Equal(new[] { 1000 }, catalog.SkinIds);
        Assert.Empty(catalog.ChromaIds);
    }

    /// <summary>
    ///     The load-bearing one. If a client does not send `subInventoryType`,
    ///     or sends a value nobody here anticipated, every owned entry must
    ///     still land in `skins` — which is exactly the list this tool exported
    ///     before chromas existed. The unverified path degrades to the shipped
    ///     behaviour rather than to an empty inventory.
    /// </summary>
    [Theory]
    [InlineData("""[{"itemId":1000,"owned":true}]""")]
    [InlineData("""[{"itemId":1000,"owned":true,"subInventoryType":null}]""")]
    [InlineData("""[{"itemId":1000,"owned":true,"subInventoryType":""}]""")]
    [InlineData("""[{"itemId":1000,"owned":true,"subInventoryType":"SOMETHING_NEW"}]""")]
    public void An_unrecognised_sub_inventory_type_counts_as_a_skin(string body)
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(body);

        Assert.Equal(new[] { 1000 }, catalog.SkinIds);
        Assert.Empty(catalog.ChromaIds);
    }

    [Theory]
    [InlineData("recolor")]
    [InlineData("Recolor")]
    [InlineData("RECOLOR")]
    [InlineData("CHROMA")]
    [InlineData(" RECOLOR ")]
    public void Chroma_tagging_is_not_case_or_whitespace_sensitive(string subType)
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(
            $$"""[{"itemId":103029,"owned":true,"subInventoryType":"{{subType}}"}]""");

        Assert.Equal(new[] { 103029 }, catalog.ChromaIds);
        Assert.Empty(catalog.SkinIds);
    }

    [Fact]
    public void Ids_are_sorted_and_de_duplicated()
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(
            """
            [
              {"itemId":103000,"owned":true},
              {"itemId":1000,"owned":true},
              {"itemId":1000,"owned":true}
            ]
            """);

        Assert.Equal(new[] { 1000, 103000 }, catalog.SkinIds);
    }

    [Fact]
    public void Item_id_zero_is_the_clients_placeholder_and_is_dropped()
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(
            """[{"itemId":0,"owned":true},{"itemId":1000,"owned":true}]""");

        Assert.Equal(new[] { 1000 }, catalog.SkinIds);
    }

    [Fact]
    public void A_flat_catalog_yields_the_owned_ids()
    {
        var ids = CatalogParser.OwnedItemIds(
            """
            [
              {"itemId":4090,"owned":true},
              {"itemId":7,"owned":true},
              {"itemId":29,"owned":false}
            ]
            """);

        Assert.Equal(new[] { 7, 4090 }, ids);
    }

    /// <summary>
    ///     A client that is still coming up answers these routes with an empty
    ///     body, a bare error object, or nothing. None of those should cost the
    ///     user a capture, so all of them read as "owns nothing" rather than
    ///     throwing.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("""{"errorCode":"RPC_ERROR","httpStatus":404}""")]
    [InlineData("[")]
    public void A_half_started_client_reads_as_an_empty_inventory(string? body)
    {
        var catalog = CatalogParser.OwnedSkinsAndChromas(body);

        Assert.Empty(catalog.SkinIds);
        Assert.Empty(catalog.ChromaIds);
        Assert.Empty(catalog.TftSkinIds);
        Assert.Empty(catalog.TftChromaIds);
        Assert.Empty(CatalogParser.OwnedItemIds(body));
    }
}
