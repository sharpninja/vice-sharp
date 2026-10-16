using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ViceSharp.Core.FlashCarts;

/// <summary>
/// Portable flash cart image builder (TR-MVVM-001: no Avalonia/UWP types).
/// Heads bind this and supply <see cref="IFlashBuilderFileIo"/> / optional session hook.
/// </summary>
public sealed class FlashCartImageBuilderViewModel : INotifyPropertyChanged
{
    private readonly IFlashBuilderFileIo? _fileIo;
    private readonly IFlashBuilderSessionHook? _sessionHook;
    private FlashImageBuilder _builder;
    private string _selectedProfileId = FlashCartProfiles.Fe3.Id;
    private int _selectedBankIndex;
    private bool _treatAsPrg = true;
    private string _statusText = "Select a profile and import bank files.";
    private string? _lastError;

    public FlashCartImageBuilderViewModel(
        IFlashBuilderFileIo? fileIo = null,
        IFlashBuilderSessionHook? sessionHook = null)
    {
        _fileIo = fileIo;
        _sessionHook = sessionHook;
        _builder = new FlashImageBuilder(FlashCartProfiles.Fe3);
        RebuildBankItems();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<IFlashCartProfile> Profiles { get; } = FlashCartProfiles.All
        .Where(p => p.Id is "fe3" or "ultimem" or "megacart")
        .ToList();

    /// <summary>Includes EasyFlash for tooling; primary UI may hide it.</summary>
    public IReadOnlyList<IFlashCartProfile> AllProfiles => FlashCartProfiles.All;

    public string SelectedProfileId
    {
        get => _selectedProfileId;
        set
        {
            var id = string.IsNullOrWhiteSpace(value) ? FlashCartProfiles.Fe3.Id : value;
            if (string.Equals(_selectedProfileId, id, StringComparison.Ordinal))
                return;
            var profile = FlashCartProfiles.TryGet(id) ?? FlashCartProfiles.Fe3;
            _selectedProfileId = profile.Id;
            _builder = new FlashImageBuilder(profile);
            _selectedBankIndex = 0;
            RebuildBankItems();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedProfile));
            OnPropertyChanged(nameof(SelectedBankIndex));
            OnPropertyChanged(nameof(ImageSizeSummary));
            StatusText = $"Profile: {profile.DisplayName}";
        }
    }

    public IFlashCartProfile SelectedProfile => _builder.Profile;

    public ObservableCollection<FlashBankSlotViewItem> Banks { get; } = new();

    public int SelectedBankIndex
    {
        get => _selectedBankIndex;
        set
        {
            var next = Math.Clamp(value, 0, Math.Max(0, Banks.Count - 1));
            if (_selectedBankIndex == next)
                return;
            _selectedBankIndex = next;
            OnPropertyChanged();
        }
    }

    /// <summary>When true, imports strip a 2-byte CBM load address (PRG).</summary>
    public bool TreatAsPrg
    {
        get => _treatAsPrg;
        set
        {
            if (_treatAsPrg == value)
                return;
            _treatAsPrg = value;
            OnPropertyChanged();
        }
    }

    public string ImageSizeSummary
        => $"{SelectedProfile.DisplayName}: {SelectedProfile.ImageSizeBytes / 1024} KiB, {SelectedProfile.BankCount} x {SelectedProfile.BankSizeBytes / 1024} KiB banks";

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal))
                return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string? LastError
    {
        get => _lastError;
        private set
        {
            if (string.Equals(_lastError, value, StringComparison.Ordinal))
                return;
            _lastError = value;
            OnPropertyChanged();
        }
    }

    public void ImportBank(ReadOnlySpan<byte> data, string sourceName)
    {
        try
        {
            LastError = null;
            if (TreatAsPrg && data.Length >= 3)
                _builder.WritePrg(SelectedBankIndex, data, sourceName);
            else
                _builder.WriteRaw(SelectedBankIndex, data, sourceName: sourceName);
            SyncBankItem(SelectedBankIndex);
            StatusText = $"Imported {sourceName} into bank {SelectedBankIndex}.";
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            StatusText = $"Import failed: {ex.Message}";
        }
    }

    public void ClearSelectedBank()
    {
        _builder.ClearBank(SelectedBankIndex);
        SyncBankItem(SelectedBankIndex);
        StatusText = $"Cleared bank {SelectedBankIndex}.";
    }

    public void ClearAllBanks()
    {
        _builder.ClearAll();
        RebuildBankItems();
        StatusText = "All banks cleared.";
    }

    public byte[] Build()
    {
        LastError = null;
        var img = _builder.Build();
        StatusText = $"Built {img.Length} byte image ({SelectedProfile.Id}).";
        return img;
    }

    public byte[]? BuildSecondary() => _builder.BuildSecondary();

    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_fileIo is null)
            throw new InvalidOperationException("No file IO service configured.");
        var img = Build();
        await _fileIo.WriteAllBytesAsync(path, img, cancellationToken).ConfigureAwait(false);
        var sec = BuildSecondary();
        if (sec is { Length: > 0 })
        {
            var nvPath = path + ".nvram";
            await _fileIo.WriteAllBytesAsync(nvPath, sec, cancellationToken).ConfigureAwait(false);
            StatusText = $"Saved {path} and {nvPath}.";
        }
        else
        {
            StatusText = $"Saved {path}.";
        }
    }

    public async Task AttachToSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_sessionHook is null)
            throw new InvalidOperationException("No session attach hook configured.");
        var img = Build();
        await _sessionHook
            .AttachImageAsync(SelectedProfile.Id, img, $"{SelectedProfile.Id}.bin", cancellationToken)
            .ConfigureAwait(false);
        StatusText = $"Attached {SelectedProfile.Id} image to session.";
    }

    private void RebuildBankItems()
    {
        Banks.Clear();
        foreach (var bank in _builder.Banks)
            Banks.Add(new FlashBankSlotViewItem(bank));
    }

    private void SyncBankItem(int index)
    {
        if (index < 0 || index >= Banks.Count)
            return;
        Banks[index] = new FlashBankSlotViewItem(_builder.Banks[index]);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Bindable snapshot of a bank slot.</summary>
public sealed class FlashBankSlotViewItem
{
    public FlashBankSlotViewItem(FlashBankSlot slot)
    {
        Index = slot.Index;
        Offset = slot.Offset;
        Length = slot.Length;
        Label = slot.Label;
        SourceName = slot.SourceName ?? "";
        ContentKind = slot.ContentKind.ToString();
        IsFilled = slot.ContentKind != FlashBankContentKind.Empty;
    }

    public int Index { get; }
    public int Offset { get; }
    public int Length { get; }
    public string Label { get; }
    public string SourceName { get; }
    public string ContentKind { get; }
    public bool IsFilled { get; }
}
