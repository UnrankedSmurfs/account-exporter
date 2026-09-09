using UnrankedSmurfs.AccountExporter.Export;

namespace UnrankedSmurfs.AccountExporter.Tests;

/// <summary>
///     The League/TFT split.
///
///     Every number here is a real id from the first real capture — an EUW
///     account with 236 "champions" and 767 "skins", of which 63 and 226 were
///     Teamfight Tactics. This is not a synthetic boundary being defended; it
///     is the boundary that account actually sat either side of.
/// </summary>
public class RiotIdsTests
{
    [Theory]
    // Real League champion keys, oldest to newest observed.
    [InlineData(1)]     // Annie
    [InlineData(103)]   // Ahri
    [InlineData(147)]   // Seraphine
    [InlineData(950)]   // the largest League key in the real capture
    public void A_league_champion_key_is_league(int key)
    {
        Assert.True(RiotIds.IsLeagueChampionKey(key));
    }

    [Theory]
    // Real TFT companion keys from the same capture.
    [InlineData(60001)]
    [InlineData(60009)]
    [InlineData(60117)]
    public void A_tft_companion_key_is_not(int key)
    {
        Assert.False(RiotIds.IsLeagueChampionKey(key));
    }

    /// <summary>Champion id 0 is the "None" placeholder the client returns.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_non_positive_key_is_not_a_champion(int key)
    {
        Assert.False(RiotIds.IsLeagueChampionKey(key));
    }

    /// <summary>
    ///     A skin id is its champion's key followed by three digits, so
    ///     classifying a skin is one question about its champion rather than a
    ///     second threshold that could drift out of step with the first.
    /// </summary>
    [Theory]
    [InlineData(1004, 1)]           // Annie
    [InlineData(103028, 103)]       // K/DA Ahri
    [InlineData(147001, 147)]       // K/DA ALL OUT Seraphine
    [InlineData(895021, 895)]       // the largest League skin in the capture
    [InlineData(60001004, 60001)]   // a TFT tactician skin
    [InlineData(60103302, 60103)]   // a TFT tactician chroma
    public void A_skin_id_carries_its_champions_key(int skinId, int expectedKey)
    {
        Assert.Equal(expectedKey, RiotIds.ChampionKeyOf(skinId));
    }

    [Theory]
    [InlineData(1004)]
    [InlineData(103028)]
    [InlineData(147003)]    // an ultimate skin's alternate form; still a skin
    [InlineData(1039)]      // a chroma — same numbering space as skins
    [InlineData(895021)]
    public void A_league_skin_id_is_league(int skinId)
    {
        Assert.True(RiotIds.IsLeagueSkinId(skinId));
    }

    [Theory]
    [InlineData(60001004)]
    [InlineData(60002003)]
    [InlineData(60117037)]  // the largest id in the real capture
    [InlineData(60001305)]  // a TFT chroma
    public void A_tft_skin_id_is_not(int skinId)
    {
        Assert.False(RiotIds.IsLeagueSkinId(skinId));
    }

    /// <summary>
    ///     The line is drawn in empty space, not next to either neighbour.
    ///     The real capture's largest League key was 950 and its smallest TFT
    ///     key 60001; if a future patch ever puts something in between, this
    ///     is the test that should be argued with rather than quietly moved.
    /// </summary>
    [Fact]
    public void The_ceiling_sits_between_the_two_ranges_with_room_to_spare()
    {
        const int largestObservedLeagueKey = 950;
        const int smallestObservedTftKey = 60_001;

        Assert.True(largestObservedLeagueKey < RiotIds.LeagueChampionKeyCeiling);
        Assert.True(RiotIds.LeagueChampionKeyCeiling < smallestObservedTftKey);

        // Roughly 250 champion keys of headroom before this needs revisiting.
        Assert.True(RiotIds.LeagueChampionKeyCeiling - largestObservedLeagueKey > 200);
    }
}
