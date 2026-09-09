using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using UnrankedSmurfs.AccountExporter.Export;

namespace UnrankedSmurfs.AccountExporter;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<AccountSnapshot> captured = [];
    private readonly DispatcherTimer clientPoll;
    private bool capturing;

    public MainWindow()
    {
        InitializeComponent();

        AccountsGrid.ItemsSource = captured;
        captured.CollectionChanged += (_, _) => RefreshCapturedState();
        AccountsGrid.SelectionChanged += (_, _) =>
            RemoveButton.IsEnabled = AccountsGrid.SelectedItem is AccountSnapshot;

        clientPoll = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        clientPoll.Tick += (_, _) => RefreshClientStatus();
        clientPoll.Start();

        RefreshClientStatus();
        RefreshCapturedState();
    }

    private static bool IsClientRunning() => Process.GetProcessesByName("LeagueClientUx").Length > 0;

    private void RefreshClientStatus()
    {
        // While a capture is in flight the status line belongs to it.
        if (capturing) return;

        var running = IsClientRunning();
        ClientStatusText.Text = running ? "League client detected." : "League client not running.";
        ClientHintText.Text = running
            ? "Sign in to the account you want to export, then press Capture."
            : "Start the League client and sign in to an account to begin.";
        CaptureButton.IsEnabled = running;
    }

    private void RefreshCapturedState()
    {
        CapturedHeader.Text = $"Captured accounts ({captured.Count})";
        ExportButton.IsEnabled = captured.Count > 0;
        AccountsGrid.Visibility = captured.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyStateText.Visibility = captured.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void OnCaptureClick(object sender, RoutedEventArgs e)
    {
        capturing = true;
        CaptureButton.IsEnabled = false;
        ClientStatusText.Text = "Reading account data from the League client...";
        ClientHintText.Text = "This takes a few seconds.";
        StatusText.Text = string.Empty;

        try
        {
            var result = await AccountCapture.CaptureAsync();

            if (!result.Succeeded || result.Snapshot is null)
            {
                StatusText.Text = result.Error ?? "Capture failed.";
                return;
            }

            var snapshot = result.Snapshot;

            // Re-capturing the same signed-in account should refresh it rather
            // than add a second row. Region plus rank plus inventory size is a
            // good enough identity here: no credentials are read, so there is
            // no account id to compare on.
            var existing = captured.FirstOrDefault(a =>
                a.Region == snapshot.Region
                && a.Level == snapshot.Level
                && a.ChampionCount == snapshot.ChampionCount
                && a.SkinCount == snapshot.SkinCount);

            if (existing != null)
            {
                captured[captured.IndexOf(existing)] = snapshot;
                StatusText.Text = $"Updated {snapshot.Region} account — {snapshot.ChampionCount} champions, {snapshot.SkinCount} skins.";
            }
            else
            {
                captured.Add(snapshot);
                StatusText.Text = $"Captured {snapshot.Region} account — {snapshot.ChampionCount} champions, {snapshot.SkinCount} skins.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Capture failed: {ex.Message}";
        }
        finally
        {
            capturing = false;
            RefreshClientStatus();
        }
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (AccountsGrid.SelectedItem is AccountSnapshot selected)
        {
            captured.Remove(selected);
            StatusText.Text = "Removed.";
        }
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export accounts for UnrankedSmurfs",
            Filter = "JSON file (*.json)|*.json",
            DefaultExt = ".json",
            FileName = ExportWriter.SuggestedFileName(captured.Count),
            AddExtension = true,
        };

        if (dialog.ShowDialog(this) != true) return;

        ExportButton.IsEnabled = false;
        try
        {
            await ExportWriter.WriteAsync(dialog.FileName, captured);
            StatusText.Text = $"Exported {captured.Count} account(s) to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Export failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            ExportButton.IsEnabled = captured.Count > 0;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        clientPoll.Stop();
        base.OnClosed(e);
    }
}
