using System.ComponentModel;

namespace ViceSharp.Abstractions;

/// <summary>
/// View-facing flash cart builder and its observable bank list.
/// 2026-11-23, TR-MVVM-001 and FR-FLASHCART-001, D6/R18:
/// expose the builder to desktop views through the host composition boundary.
/// </summary>
public interface IFlashCartImageBuilder : INotifyPropertyChanged
{
    /// <summary>Profiles supported by the builder's desktop selector.</summary>
    IReadOnlyList<IFlashCartBuilderProfile> Profiles { get; }

    /// <summary>Stable ID of the selected profile.</summary>
    string SelectedProfileId { get; set; }

    /// <summary>Observable bank list for the selected profile.</summary>
    IReadOnlyList<IFlashCartBuilderBank> Banks { get; }

    /// <summary>Index of the bank used by import and clear commands.</summary>
    int SelectedBankIndex { get; set; }

    /// <summary>Whether to strip the load address when importing program files.</summary>
    bool TreatAsPrg { get; set; }

    /// <summary>Display text for the image dimensions of the selected profile.</summary>
    string ImageSizeSummary { get; }

    /// <summary>Most recent build or editing status.</summary>
    string StatusText { get; }

    /// <summary>Imports the source bytes into the selected bank.</summary>
    void ImportBank(ReadOnlySpan<byte> data, string sourceName);

    /// <summary>Clears the selected bank.</summary>
    void ClearSelectedBank();

    /// <summary>Clears every bank in the current image.</summary>
    void ClearAllBanks();

    /// <summary>Builds the primary image for delivery to a stream.</summary>
    byte[] Build();

    /// <summary>Saves the image and any companion data to a local path.</summary>
    Task SaveAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Profile information displayed by the desktop builder.
/// 2026-11-23, TR-MVVM-001 and FR-FLASHCART-001, D6/R18.
/// </summary>
public interface IFlashCartBuilderProfile
{
    /// <summary>Stable profile key.</summary>
    string Id { get; }

    /// <summary>Human-readable profile label.</summary>
    string DisplayName { get; }
}

/// <summary>
/// Bank information displayed by the desktop builder.
/// 2026-11-23, TR-MVVM-001 and FR-FLASHCART-001, D6/R18.
/// </summary>
public interface IFlashCartBuilderBank
{
    /// <summary>Display label for the bank.</summary>
    string Label { get; }

    /// <summary>Display label for the bank's current contents.</summary>
    string ContentKind { get; }

    /// <summary>Display name of the source file.</summary>
    string SourceName { get; }
}
