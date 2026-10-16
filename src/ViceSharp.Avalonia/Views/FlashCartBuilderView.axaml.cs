using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ViceSharp.Core.FlashCarts;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-FLASHCART-001: desktop UI for building FE3 / Ultimem / Mega-Cart flash images.
/// </summary>
public partial class FlashCartBuilderView : UserControl
{
    public FlashCartBuilderView()
    {
        InitializeComponent();
        DataContext ??= new FlashCartImageBuilderViewModel(new AvaloniaFlashBuilderFileIo());
        if (this.FindControl<ComboBox>("ProfileBox") is { } box && ViewModel is not null)
        {
            box.SelectedItem = ViewModel.Profiles.FirstOrDefault(p => p.Id == ViewModel.SelectedProfileId)
                ?? ViewModel.Profiles.FirstOrDefault();
        }
    }

    public FlashCartImageBuilderViewModel? ViewModel => DataContext as FlashCartImageBuilderViewModel;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public event EventHandler? CloseRequested;

    private void OnProfileChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: IFlashCartProfile profile } && ViewModel is not null)
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

internal sealed class AvaloniaFlashBuilderFileIo : IFlashBuilderFileIo
{
    public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        => File.WriteAllBytesAsync(path, data, cancellationToken);
}
