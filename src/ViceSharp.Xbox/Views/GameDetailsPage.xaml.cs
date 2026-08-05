// PLAN-ROMM-001 X3 (IMPL-ROMM-017): game-details page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using global::RomM.Client;
using global::RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-05). The game-details page: fetches the selected ROM's detail, presents its
/// metadata + files, and adds it to a chosen list. Built from a <see cref="GameDetailsRequest"/> nav
/// parameter; binds a <see cref="RomDetailViewModel"/> over a <see cref="RomMCollectionsGateway"/>.
/// </summary>
public sealed partial class GameDetailsPage : Page
{
    private RomDetailViewModel? _detail;
    private CollectionsViewModel? _collections;

    /// <summary>Creates the page.</summary>
    public GameDetailsPage() => InitializeComponent();

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not GameDetailsRequest request)
        {
            StatusText.Text = "No game selected.";
            return;
        }

        try
        {
            if (!Uri.TryCreate(request.ServerUrl, UriKind.Absolute, out Uri? uri))
            {
                StatusText.Text = "Invalid server URL.";
                return;
            }

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(request.Token))
            {
                options.Auth = RomMAuth.ClientApiToken(request.Token.Trim());
            }

            IRomMClient client = RomMClient.Create(options);
            var libraryGateway = new RomMLibraryGateway(client);
            var collectionsGateway = new RomMCollectionsGateway(client);

            RomDetail detail = await libraryGateway.GetRomAsync(request.RomId);
            _detail = new RomDetailViewModel(detail, collectionsGateway);

            TitleText.Text = detail.Name;
            SubtitleText.Text = detail.PlatformSlug is { Length: > 0 } slug ? slug.ToUpperInvariant() : string.Empty;
            SummaryText.Text = string.IsNullOrWhiteSpace(detail.Summary) ? "(no description)" : detail.Summary;
            FilesList.ItemsSource = detail.Files;

            _collections = new CollectionsViewModel(collectionsGateway);
            await _collections.RefreshAsync();
            ListPicker.ItemsSource = _collections.Collections;

            StatusText.Text = $"{detail.Files.Count} file(s). In {detail.CollectionIds.Count} list(s).";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Load failed: {ex.Message}";
        }
    }

    private async void OnAddToList(object sender, RoutedEventArgs e)
    {
        if (_detail is null)
        {
            StatusText.Text = "Nothing loaded.";
            return;
        }

        if (ListPicker.SelectedItem is not LibraryCollection target)
        {
            StatusText.Text = "Pick a list first.";
            return;
        }

        if (target.ReadOnly)
        {
            StatusText.Text = "That list is server-managed and cannot be edited.";
            return;
        }

        try
        {
            await _detail.AddToCollectionAsync(target.Id);
            StatusText.Text = $"Added to '{target.Name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Add failed: {ex.Message}";
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
