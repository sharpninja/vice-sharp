// PLAN-ROMM-001 X3 (IMPL-ROMM-017): game-details page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using global::RomM.Client;
using global::RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using ViceSharp.RomM;
using ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-05). The game-details page: presents a game group, lists language/region
/// variants for selection, attaches the chosen variant, and can add it to a collection.
/// </summary>
public sealed partial class GameDetailsPage : Page
{
    private IReadOnlyList<RomTile> _variants = Array.Empty<RomTile>();
    private RomTile? _selectedVariant;
    private RomDetailViewModel? _detail;
    private CollectionsViewModel? _collections;
    private IRomMCollectionsGateway? _collectionsGateway;
    private IRomMLibraryGateway? _libraryGateway;
    private IGameLauncher? _launcher;
    private XboxCoverImageLoader? _coverLoader;
    private string _cacheDir = string.Empty;

    /// <summary>Creates the page.</summary>
    public GameDetailsPage()
    {
        InitializeComponent();
        AddHandler(
            UIElement.KeyDownEvent,
            new Windows.UI.Xaml.Input.KeyEventHandler(OnPageKeyDown),
            handledEventsToo: true);
    }

    /// <inheritdoc />
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not GameDetailsRequest request || request.Variants.Count == 0)
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

            _variants = request.Variants.ToList();
            _cacheDir = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "romm-cache");

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(request.Token))
            {
                options.Auth = RomMAuth.ClientApiToken(request.Token.Trim());
            }

            IRomMClient client = RomMClient.Create(options);
            _libraryGateway = new RomMLibraryGateway(client);
            _collectionsGateway = new RomMCollectionsGateway(client);
            _launcher = App.Instance.CreateRomMGameLauncher();
            _coverLoader = new XboxCoverImageLoader(uri, request.Token);

            TitleText.Text = request.GameName;
            SubtitleText.Text = _variants.Count == 1
                ? "1 variant"
                : $"{_variants.Count} variants";

            VariantsList.ItemsSource = _variants;
            VariantsList.SelectedIndex = 0;
            _selectedVariant = _variants[0];

            await LoadVariantDetailAsync(_selectedVariant);
            await LoadCoverAsync(_selectedVariant);

            _collections = new CollectionsViewModel(_collectionsGateway);
            await _collections.RefreshAsync();
            ListPicker.ItemsSource = _collections.Collections;

            StatusText.Text = _variants.Count == 1
                ? "Select Attach to load this game."
                : "Pick a variant, then Attach.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Load failed: {ex.Message}";
        }
    }

    private async void OnVariantSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VariantsList.SelectedItem is not RomTile tile)
        {
            return;
        }

        _selectedVariant = tile;
        try
        {
            await LoadVariantDetailAsync(tile);
            await LoadCoverAsync(tile);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Variant load failed: {ex.Message}";
        }
    }

    private async Task LoadVariantDetailAsync(RomTile tile)
    {
        if (_libraryGateway is null || _collectionsGateway is null)
        {
            return;
        }

        RomDetail detail = await _libraryGateway.GetRomAsync(tile.Id);
        _detail = new RomDetailViewModel(detail, _collectionsGateway);

        string platform = detail.PlatformSlug is { Length: > 0 } slug ? slug.ToUpperInvariant() : string.Empty;
        SubtitleText.Text = string.IsNullOrEmpty(platform)
            ? tile.VariantLabel
            : $"{platform} · {tile.VariantLabel}";
        SummaryText.Text = string.IsNullOrWhiteSpace(detail.Summary) ? "(no description)" : detail.Summary;
        FilesList.ItemsSource = detail.Files;
    }

    private async Task LoadCoverAsync(RomTile tile)
    {
        if (_coverLoader is null || tile.Cover is null)
        {
            CoverImage.Source = null;
            return;
        }

        try
        {
            ImageSource? source = await _coverLoader.LoadCoverAsync(tile.Cover, CancellationToken.None);
            if (_selectedVariant?.Id == tile.Id)
            {
                CoverImage.Source = source;
            }
        }
        catch
        {
            // Cover is decorative.
        }
    }

    private async void OnAttach(object sender, RoutedEventArgs e) => await AttachAsync(autostart: false);

    private async void OnAttachAutostart(object sender, RoutedEventArgs e) => await AttachAsync(autostart: true);

    private async void OnPageKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.GamepadA:
                e.Handled = true;
                await AttachAsync(autostart: false);
                break;
            case Windows.System.VirtualKey.GamepadY:
                e.Handled = true;
                await AttachAsync(autostart: true);
                break;
            case Windows.System.VirtualKey.GamepadB:
            case Windows.System.VirtualKey.Escape:
                e.Handled = true;
                OnBack(sender, e);
                break;
        }
    }

    private async Task AttachAsync(bool autostart)
    {
        if (_selectedVariant is null || _libraryGateway is null || _launcher is null)
        {
            StatusText.Text = "Pick a variant first.";
            return;
        }

        if (!_selectedVariant.Launchable)
        {
            StatusText.Text = "That variant is not launchable.";
            return;
        }

        RomTile tile = _selectedVariant;
        MediaSlot slot = MediaExtensionMap.Resolve(tile.FileName)?.Slot ?? MediaSlot.Drive8;

        try
        {
            StatusText.Text = "Downloading...";
            var progress = new Progress<double>(f => StatusText.Text = $"Downloading {f:P0}");
            AcquiredGame game = await _libraryGateway.DownloadAsync(
                tile.Id,
                tile.FileName,
                tile.SizeBytes ?? 0,
                _cacheDir,
                progress,
                CancellationToken.None);

            StatusText.Text = "Starting...";
            LaunchOutcome outcome = await _launcher.LaunchAsync(game, slot, autostart, CancellationToken.None);
            StatusText.Text = outcome.Message;

            if (outcome.Success)
            {
                try
                {
                    string recentsPath = System.IO.Path.Combine(
                        ApplicationData.Current.LocalFolder.Path, "romm-recents.json");
                    await new FileRecentsStore(recentsPath).RecordAsync(
                        RecentGame.FromTile(tile), RecentGame.DefaultCapacity, CancellationToken.None);
                }
                catch
                {
                    // Recents is best-effort.
                }

                // Autostart hands control to the running C64: leave details and resume the emulator.
                if (autostart)
                {
                    App.Instance.DismissMenu();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Attach failed: {ex.Message}";
        }
    }

    private async void OnAddToList(object sender, RoutedEventArgs e)
    {
        if (_selectedVariant is null || _libraryGateway is null || _collectionsGateway is null)
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
            if (_detail is null || _detail.Detail.Id != _selectedVariant.Id)
            {
                RomDetail detail = await _libraryGateway.GetRomAsync(_selectedVariant.Id);
                _detail = new RomDetailViewModel(detail, _collectionsGateway);
            }

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
