// PLAN-ROMM-001 X4 (IMPL-ROMM-013): CSDb discovery page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using global::RomM.Client;
using global::RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;

/// <summary>
/// PLAN-ROMM-001 (FR-CSDB-001, AC-CSDB-01/02/04/05). The CSDb discovery page: search scene releases
/// through the csdb-bridge and ingest a capped selection into RomM. Builds a
/// <see cref="CsdbDiscoveryViewModel"/> over a <see cref="BridgeCsdbGateway"/> (bridge HTTP + the RomM
/// client's Tasks client for the post-ingest scan). Xbox is sandboxed, so it always uses the bridge,
/// never the local <c>LocalCsdbGateway</c>.
/// </summary>
public sealed partial class CsdbPage : Page
{
    private CsdbDiscoveryViewModel? _vm;

    /// <summary>Creates the page.</summary>
    public CsdbPage() => InitializeComponent();

    private CsdbDiscoveryViewModel? BuildViewModel()
    {
        if (!Uri.TryCreate(ServerBox.Text, UriKind.Absolute, out Uri? serverUri))
        {
            StatusText.Text = "Invalid RomM server URL.";
            return null;
        }

        if (!Uri.TryCreate(BridgeBox.Text, UriKind.Absolute, out Uri? bridgeUri))
        {
            StatusText.Text = "Invalid bridge URL.";
            return null;
        }

        var options = new RomMClientOptions { BaseAddress = serverUri };
        if (!string.IsNullOrWhiteSpace(TokenBox.Password))
        {
            options.Auth = RomMAuth.ClientApiToken(TokenBox.Password.Trim());
        }

        IRomMClient client = RomMClient.Create(options);
        var bridgeHttp = new HttpClient { BaseAddress = bridgeUri };
        var gateway = new BridgeCsdbGateway(bridgeHttp, client.Tasks);
        return new CsdbDiscoveryViewModel(gateway);
    }

    private async void OnSearch(object sender, RoutedEventArgs e)
    {
        try
        {
            _vm ??= BuildViewModel();
            if (_vm is null)
            {
                return;
            }

            _vm.Query = SearchBox.Text ?? string.Empty;
            _vm.Kinds.Clear();
            if (DemoToggle.IsChecked == true)
            {
                _vm.Kinds.Add(CsdbKind.Demo);
            }

            if (CrackToggle.IsChecked == true)
            {
                _vm.Kinds.Add(CsdbKind.Crack);
            }

            if (SidToggle.IsChecked == true)
            {
                _vm.Kinds.Add(CsdbKind.Sid);
            }

            await _vm.SearchAsync();
            ResultsList.ItemsSource = _vm.Results;
            StatusText.Text = $"{_vm.Results.Count} hits. Select up to {CsdbDiscoveryViewModel.MaxIngestSelection}, then Ingest.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Search failed: {ex.Message}";
        }
    }

    private async void OnIngest(object sender, RoutedEventArgs e)
    {
        if (_vm is null)
        {
            StatusText.Text = "Search first.";
            return;
        }

        List<CsdbSelection> selections = ResultsList.SelectedItems
            .OfType<CsdbHit>()
            .Select(h => new CsdbSelection(h.CsdbId, h.Kind))
            .ToList();

        if (selections.Count == 0)
        {
            StatusText.Text = $"Select up to {CsdbDiscoveryViewModel.MaxIngestSelection} entries.";
            return;
        }

        try
        {
            int count = Math.Min(selections.Count, CsdbDiscoveryViewModel.MaxIngestSelection);
            StatusText.Text = $"Ingesting {count}...";
            await _vm.IngestAsync(selections);

            CsdbIngestResult? r = _vm.LastResult;
            StatusText.Text = r is null
                ? "Ingest complete."
                : $"Ingested {r.Ingested}, skipped {r.Skipped}, failed {r.Failed}; scan {(r.Scanned ? "queued" : "skipped")}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ingest failed: {ex.Message}";
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
        }
    }
}
#endif
