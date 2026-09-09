using System.Windows;
using System.Windows.Media;

namespace UnrankedSmurfs.AccountExporter.Tests;

/// <summary>
///     Guards the brand palette against the two ways it silently rots.
///
///     The first is a renamed or deleted resource key. XAML resource lookup is
///     a runtime concern: a `{StaticResource AccentBrush}` pointing at a key
///     somebody renamed compiles with zero warnings and throws on the user's
///     launch. <see cref="WindowSmokeTests" /> catches that for the keys the
///     window happens to use; the roster below catches it for the rest.
///
///     The second is drift away from the design system at
///     `website/.claude/UnrankedSmurfs Design System/colors_and_type.css`.
///     These are its values, written out so a future edit that reaches for a
///     convenient grey has to change a test that says where the colour came
///     from.
/// </summary>
public class ThemeTests
{
    /// <summary>key → hex, and the design-system token it is taken from.</summary>
    public static TheoryData<string, string, string> Palette => new()
    {
        { "AppBackgroundBrush", "#FF131313", "--us-bg" },
        { "SurfaceBrush", "#FF181818", "--us-bg-bar" },
        { "SurfaceRaisedBrush", "#FF2B2B2B", "--us-bg-raised" },
        { "StrokeBrush", "#FF3B3B3D", "--us-grey-400" },
        { "TextSecondaryBrush", "#FFBDC3C7", "--us-grey-200" },
        { "AccentBrush", "#FFF7B733", "--us-gold-500" },
        { "SuccessBrush", "#FF6AC259", "--us-green" },
        { "DangerBrush", "#FFC0392B", "--us-red-200" },
    };

    [Theory]
    [MemberData(nameof(Palette))]
    public void The_palette_is_the_design_systems(string key, string expected, string token)
    {
        UiThread.Run(() =>
        {
            var brush = Application.Current.TryFindResource(key) as SolidColorBrush;

            Assert.True(brush != null, $"Resource '{key}' is missing or is not a SolidColorBrush.");
            Assert.Equal(expected, brush!.Color.ToString(), ignoreCase: true);
        });

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    /// <summary>
    ///     Nothing in the app may still be the fork's blue. `#4DA3FF` was the
    ///     old accent and `#354A5F` the old row-selection fill; the second was
    ///     the easy one to miss, because it is declared as a hard-coded literal
    ///     on `SystemColors.HighlightBrushKey` rather than as a reference to
    ///     the accent, so recolouring the accent did not touch it.
    /// </summary>
    [Fact]
    public void No_brush_is_still_the_forks_blue()
    {
        UiThread.Run(() =>
        {
            var blues = new[] { Color.FromRgb(0x4D, 0xA3, 0xFF), Color.FromRgb(0x35, 0x4A, 0x5F) };

            // Only our own dictionary. WPF-UI's dark theme ships blues of its
            // own that this app never surfaces, and walking a library's
            // palette would make this test about somebody else's colours.
            var theme = Application.Current.Resources.MergedDictionaries
                .Single(d => d.Source?.OriginalString.EndsWith("Theme.xaml", StringComparison.Ordinal) == true);

            var offenders = theme.Keys.Cast<object>()
                .Select(k => (Key: k, Value: theme[k]))
                .Where(e => e.Value is SolidColorBrush b && blues.Contains(b.Color))
                .Select(e => $"{e.Key} = {((SolidColorBrush) e.Value).Color}")
                .ToArray();

            Assert.True(offenders.Length == 0,
                "Still the fork's blue: " + string.Join(", ", offenders));
        });
    }

    /// <summary>
    ///     The row-selection fill specifically. Miss it and selecting a
    ///     captured account flashes blue in an otherwise gold app.
    /// </summary>
    [Fact]
    public void Selecting_a_row_uses_the_muted_gold()
    {
        UiThread.Run(() =>
        {
            var highlight = Application.Current.TryFindResource(SystemColors.HighlightBrushKey) as SolidColorBrush;
            var muted = Application.Current.TryFindResource("AccentMutedBrush") as SolidColorBrush;

            Assert.NotNull(highlight);
            Assert.NotNull(muted);
            Assert.Equal(muted!.Color, highlight!.Color);
        });
    }

    /// <summary>
    ///     Text on gold is `#131313`, never white — 1.3:1 against gold, which
    ///     is not a contrast ratio so much as a rumour.
    /// </summary>
    [Fact]
    public void The_primary_button_is_gold_with_dark_text()
    {
        UiThread.Run(() =>
        {
            var style = Application.Current.TryFindResource("PrimaryButtonStyle") as Style;
            Assert.True(style != null, "PrimaryButtonStyle is missing; Capture and Export would render as neutral buttons.");

            var setters = style!.Setters.OfType<Setter>().ToDictionary(s => s.Property.Name, s => s.Value);

            Assert.Equal(Color.FromRgb(0xF7, 0xB7, 0x33), ((SolidColorBrush) setters["Background"]).Color);
            Assert.Equal(Color.FromRgb(0x13, 0x13, 0x13), ((SolidColorBrush) setters["Foreground"]).Color);
        });
    }
}
