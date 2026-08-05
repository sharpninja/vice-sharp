// PLAN-ROMM-001 X3 (IMPL-ROMM-014): RomM list-management page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using global::RomM.Client;
using global::RomM.Client.Auth;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-XUI-06). The list-management page: connect, list the user's RomM collections, and
/// create / rename / delete them, inspecting membership. Builds a <see cref="CollectionsViewModel"/> over
/// a <see cref="RomMCollectionsGateway"/> on Connect. Read-only (smart/virtual) collections cannot be
/// renamed or deleted.
/// </summary>
public sealed partial class ListsPage : Page
{
    private CollectionsViewModel? _collections;

    /// <summary>Creates the page.</summary>
    public ListsPage() => InitializeComponent();

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Uri.TryCreate(UrlBox.Text, UriKind.Absolute, out Uri? uri))
            {
                StatusText.Text = "Invalid server URL.";
                return;
            }

            var options = new RomMClientOptions { BaseAddress = uri };
            if (!string.IsNullOrWhiteSpace(TokenBox.Password))
            {
                options.Auth = RomMAuth.ClientApiToken(TokenBox.Password.Trim());
            }

            IRomMClient client = RomMClient.Create(options);
            _collections = new CollectionsViewModel(new RomMCollectionsGateway(client));
            await _collections.RefreshAsync();

            CollectionsList.ItemsSource = _collections.Collections;
            StatusText.Text = $"{_collections.Collections.Count} lists.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connect failed: {ex.Message}";
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_collections is null)
        {
            return;
        }

        var selected = CollectionsList.SelectedItem as LibraryCollection;
        _collections.SelectedCollection = selected;
        MembersList.ItemsSource = selected?.RomIds.Select(id => $"Rom #{id}").ToList();
        if (selected is not null)
        {
            NameBox.Text = selected.Name;
            StatusText.Text = selected.ReadOnly
                ? $"{selected.Name} is server-managed (read-only)."
                : $"{selected.Name}: {selected.Count} games.";
        }
    }

    private async void OnNewList(object sender, RoutedEventArgs e)
    {
        if (_collections is null)
        {
            StatusText.Text = "Connect first.";
            return;
        }

        string name = (NameBox.Text ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            StatusText.Text = "Enter a list name.";
            return;
        }

        try
        {
            await _collections.CreateAsync(name);
            StatusText.Text = $"Created '{name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Create failed: {ex.Message}";
        }
    }

    private async void OnRename(object sender, RoutedEventArgs e)
    {
        if (_collections?.SelectedCollection is not { } selected)
        {
            StatusText.Text = "Select a list.";
            return;
        }

        if (selected.ReadOnly)
        {
            StatusText.Text = "That list is server-managed and cannot be renamed.";
            return;
        }

        string name = (NameBox.Text ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            StatusText.Text = "Enter a new name.";
            return;
        }

        try
        {
            await _collections.RenameAsync(selected.Id, name);
            StatusText.Text = $"Renamed to '{name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Rename failed: {ex.Message}";
        }
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (_collections?.SelectedCollection is not { } selected)
        {
            StatusText.Text = "Select a list.";
            return;
        }

        if (selected.ReadOnly)
        {
            StatusText.Text = "That list is server-managed and cannot be deleted.";
            return;
        }

        try
        {
            await _collections.DeleteAsync(selected.Id);
            MembersList.ItemsSource = null;
            StatusText.Text = $"Deleted '{selected.Name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Delete failed: {ex.Message}";
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
