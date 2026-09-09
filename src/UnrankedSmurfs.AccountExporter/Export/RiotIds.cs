namespace UnrankedSmurfs.AccountExporter.Export;

/// <summary>
///     Tells League content apart from Teamfight Tactics content by id.
///
///     The League client files TFT tacticians (the little companions you play
///     TFT as) under the same <c>CHAMPION</c> and <c>CHAMPION_SKIN</c>
///     inventory types it uses for League champions and League skins. Read
///     those routes naively — as this tool did through v0.9.0 — and a TFT
///     tactician counts as a champion, and its skins count as champion skins.
///
///     On the first real capture that was not a rounding error: 63 of 236
///     "champions" and 226 of 767 "skins" were TFT. A listing built from that
///     export would have advertised 767 skins when 539 were League skins.
///
///     There is no flag on the payload that separates them — or at least none
///     that has been observed, and the capture that proved the problem did not
///     record the raw bodies. What the ids themselves say is unambiguous, so
///     that is what this reads.
/// </summary>
internal static class RiotIds
{
    /// <summary>
    ///     One above the largest champion key that could plausibly be League's.
    ///
    ///     Measured on a real account: the largest League champion key in the
    ///     inventory was 950, and the smallest TFT companion key was 60001.
    ///     Nothing at all occupies the 59,000 keys between them, so this line
    ///     is drawn in empty space rather than near either neighbour. Riot
    ///     would have to allocate roughly 250 more champion keys before it
    ///     needed revisiting, and TFT would have to move down by two orders of
    ///     magnitude.
    /// </summary>
    public const int LeagueChampionKeyCeiling = 1_200;

    /// <summary>
    ///     A skin id is its champion's key followed by three digits —
    ///     Annie (1) owns 1000–1099, Ahri (103) owns 103000–103999. Chromas
    ///     share that space rather than having one of their own.
    ///
    ///     So a skin is League's exactly when the champion it belongs to is,
    ///     and the whole classification reduces to one question about one
    ///     number. That is deliberately not a second threshold to keep in step
    ///     with the first: 60001004 is TFT because 60001 is.
    /// </summary>
    public static int ChampionKeyOf(int skinId) => skinId / 1000;

    /// <summary>Champion key 0 is the "None" placeholder the client returns.</summary>
    public static bool IsLeagueChampionKey(int key) => key is > 0 and < LeagueChampionKeyCeiling;

    public static bool IsLeagueSkinId(int skinId) => IsLeagueChampionKey(ChampionKeyOf(skinId));
}
