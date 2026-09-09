using System.Diagnostics;
using System.Windows;
using UnrankedSmurfs.AccountExporter.Export;

namespace UnrankedSmurfs.AccountExporter.Tests;

/// <summary>
///     The regression guard for the one class of defect this project's CI could
///     not otherwise see.
///
///     XAML resource failures are a runtime concern, not a compile-time one: a
///     `{StaticResource AccentBrush}` pointing at a key somebody renamed builds
///     with zero warnings and throws the moment the window is constructed. A
///     green build was therefore never evidence that the app starts. These tests
///     construct the real window against the real application resources, so a
///     renamed or deleted brush fails the build instead of the user's launch.
/// </summary>
public class WindowSmokeTests
{
    [Fact]
    public void MainWindow_constructs_against_the_real_application_resources()
    {
        UiThread.Run(() =>
        {
            var window = new MainWindow();

            try
            {
                Assert.NotNull(window.Content);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    ///     Lays the window out with a populated grid and fails on any binding
    ///     error WPF reports.
    ///
    ///     Binding failures are silent by default — they surface as an empty
    ///     cell, never an exception. `ChampionKeys.Count` was exactly this bug
    ///     during the fork: the backing collection is an array, whose `Count` is
    ///     an explicit interface implementation and therefore invisible to the
    ///     binding engine. It was caught by reading, which does not scale.
    /// </summary>
    [Fact]
    public void MainWindow_lays_out_a_populated_grid_without_binding_errors()
    {
        var listener = new BindingErrorListener();
        var source = PresentationTraceSources.DataBindingSource;

        source.Listeners.Add(listener);
        source.Switch.Level = SourceLevels.Error | SourceLevels.Warning;

        try
        {
            UiThread.Run(() =>
            {
                var window = new MainWindow();

                try
                {
                    window.AccountsGrid.ItemsSource = new[] { Sample() };
                    window.AccountsGrid.Visibility = Visibility.Visible;

                    // Realises the row containers, which is when the column
                    // bindings are actually evaluated.
                    window.Measure(new Size(960, 720));
                    window.Arrange(new Rect(0, 0, 960, 720));
                    window.UpdateLayout();
                }
                finally
                {
                    window.Close();
                }
            });
        }
        finally
        {
            source.Listeners.Remove(listener);
        }

        Assert.True(
            listener.Errors.Count == 0,
            "WPF reported binding errors:" + Environment.NewLine + string.Join(Environment.NewLine, listener.Errors));
    }

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

    private sealed class BindingErrorListener : TraceListener
    {
        public List<string> Errors { get; } = [];

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message)) Errors.Add(message);
        }
    }
}
