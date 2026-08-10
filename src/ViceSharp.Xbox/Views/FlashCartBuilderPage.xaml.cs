// FR-FLASHCART-001: UWP flash cart builder code-behind.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ViceSharp.Core.FlashCarts;
using ViceSharp.Xbox.ViewModels;

/// <summary>10-foot UI for composing FE3 / Ultimem / Mega-Cart flash images.</summary>
public sealed partial class FlashCartBuilderPage : Page
{
    private readonly FlashCartImageBuilderViewModel _vm = new(new UwpFlashBuilderFileIo());

    public FlashCartBuilderPage()
    {
        InitializeComponent();
        ProfileBox.ItemsSource = _vm.Profiles.Select(p => p.DisplayName).ToList();
        ProfileBox.SelectedIndex = 0;
        PrgToggle.IsOn = _vm.TreatAsPrg;
        RefreshBanks();
        StatusText.Text = _vm.StatusText;
        SummaryText.Text = _vm.ImageSizeSummary;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(FlashCartImageBuilderViewModel.StatusText))
                StatusText.Text = _vm.StatusText;
            if (e.PropertyName is nameof(FlashCartImageBuilderViewModel.ImageSizeSummary))
                SummaryText.Text = _vm.ImageSizeSummary;
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshBanks();
    }

    private void RefreshBanks()
    {
        var labels = new List<string>();
        foreach (var b in _vm.Banks)
        {
            var src = string.IsNullOrEmpty(b.SourceName) ? b.ContentKind : b.SourceName;
            labels.Add($"{b.Label}  [{src}]");
        }

        var selected = BankList.SelectedIndex;
        BankList.ItemsSource = labels;
        if (selected >= 0 && selected < labels.Count)
            BankList.SelectedIndex = selected;
        else if (labels.Count > 0)
            BankList.SelectedIndex = _vm.SelectedBankIndex;
        SummaryText.Text = _vm.ImageSizeSummary;
    }

    private void OnProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileBox.SelectedIndex < 0 || ProfileBox.SelectedIndex >= _vm.Profiles.Count)
            return;
        _vm.SelectedProfileId = _vm.Profiles[ProfileBox.SelectedIndex].Id;
        RefreshBanks();
    }

    private void OnPrgToggled(object sender, RoutedEventArgs e)
        => _vm.TreatAsPrg = PrgToggle.IsOn;

    private void OnBankSelected(object sender, SelectionChangedEventArgs e)
    {
        if (BankList.SelectedIndex >= 0)
            _vm.SelectedBankIndex = BankList.SelectedIndex;
    }

    private async void OnImport(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Downloads };
            picker.FileTypeFilter.Add(".prg");
            picker.FileTypeFilter.Add(".bin");
            picker.FileTypeFilter.Add(".rom");
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            if (file is null)
                return;
            var buffer = await FileIO.ReadBufferAsync(file);
            _vm.ImportBank(buffer.ToArray(), file.Name);
            RefreshBanks();
            StatusText.Text = _vm.StatusText;
        }
        catch (Exception ex)
        {
            App.CreateLogger("FlashCart").LogError(ex, "import bank failed");
            StatusText.Text = $"Import failed: {ex.Message}";
        }
    }

    private void OnClearBank(object sender, RoutedEventArgs e)
    {
        _vm.ClearSelectedBank();
        RefreshBanks();
        StatusText.Text = _vm.StatusText;
    }

    private void OnClearAll(object sender, RoutedEventArgs e)
    {
        _vm.ClearAllBanks();
        RefreshBanks();
        StatusText.Text = _vm.StatusText;
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.Downloads };
            picker.FileTypeChoices.Add("Binary cart image", new List<string> { ".bin" });
            picker.SuggestedFileName = $"{_vm.SelectedProfileId}.bin";
            var file = await picker.PickSaveFileAsync();
            if (file is null)
                return;

            var bytes = _vm.Build();
            await FileIO.WriteBytesAsync(file, bytes);
            var sec = _vm.BuildSecondary();
            if (sec is { Length: > 0 })
            {
                var folder = await file.GetParentAsync();
                if (folder is not null)
                {
                    var nv = await folder.CreateFileAsync(file.Name + ".nvram", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(nv, sec);
                }
            }

            StatusText.Text = _vm.StatusText + $" Saved {file.Name}.";
        }
        catch (Exception ex)
        {
            App.CreateLogger("FlashCart").LogError(ex, "save cart image failed");
            StatusText.Text = $"Save failed: {ex.Message}";
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        App.Instance.Navigation.GoBack();
        if (Frame?.CanGoBack == true)
            Frame.GoBack();
    }
}

internal sealed class UwpFlashBuilderFileIo : IFlashBuilderFileIo
{
    public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        => System.IO.File.WriteAllBytesAsync(path, data, cancellationToken);
}
#endif
