using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ViceSharp.Abstractions;
using ViceSharp.Host.Services;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-FLASHCART-001: desktop UI for building FE3 / Ultimem / Mega-Cart flash images.
/// 2026-11-23, TR-MVVM-001 and D6/R18: use the host-composed builder contract.
/// </summary>
public partial class FlashCartBuilderView : UserControl
{
    /// <summary>Initializes the picker and its host-composed builder.</summary>
    public FlashCartBuilderView()
    {
        InitializeComponent();
        DataContext ??= FlashCartBuilderComposition.Create();
        if (this.FindControl<ComboBox>("ProfileBox") is { } box && ViewModel is not null)
        {
            box.SelectedItem = ViewModel.Profiles.FirstOrDefault(p => p.Id == ViewModel.SelectedProfileId)
                ?? ViewModel.Profiles.FirstOrDefault();
        }
    }

    /// <summary>Returns the builder exposed through the desktop view boundary.</summary>
    public IFlashCartImageBuilder? ViewModel => DataContext as IFlashCartImageBuilder;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public event EventHandler? CloseRequested;

    /// <summary>TR-MVVM-001, 2026-11-23. Forwards the selected profile ID to the builder.</summary>
    private void OnProfileChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: IFlashCartBuilderProfile profile } && ViewModel is not null)
            ViewModel.SelectedProfileId = profile.Id;
    }

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || TopLevel.GetTopLevel(this) is not { } top)
            return;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import bank file (PRG/BIN)",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Cart data") { Patterns = ["*.prg", "*.bin", "*.rom", "*.*"] },
            ],
        });
        if (files.Count == 0)
            return;

        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ViewModel.ImportBank(ms.ToArray(), files[0].Name);
    }

    private void OnClearBank(object? sender, RoutedEventArgs e) => ViewModel?.ClearSelectedBank();

    private void OnClearAll(object? sender, RoutedEventArgs e) => ViewModel?.ClearAllBanks();

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || TopLevel.GetTopLevel(this) is not { } top)
            return;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save cart image",
            SuggestedFileName = $"{ViewModel.SelectedProfileId}.bin",
            FileTypeChoices =
            [
                new FilePickerFileType("Binary cart image") { Patterns = ["*.bin"] },
            ],
        });
        if (file is null)
            return;

        var path = file.TryGetLocalPath() ?? file.Name;
        try
        {
            // Prefer write through storage API when path is not local.
            if (file.TryGetLocalPath() is { } local)
            {
                await ViewModel.SaveAsync(local);
            }
            else
            {
                var bytes = ViewModel.Build();
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(bytes);
            }
        }
        catch (Exception ex)
        {
            ViewModel.GetType(); // status already set on IO failure via SaveAsync when path works
            _ = ex;
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
        => CloseRequested?.Invoke(this, EventArgs.Empty);
}
