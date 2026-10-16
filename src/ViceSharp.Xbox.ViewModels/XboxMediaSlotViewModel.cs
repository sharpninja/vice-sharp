namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ViceSharp.Abstractions;
using ViceSharp.Protocol;

/// <summary>
/// PLAN-XBOXUWP S27 (IMPL-XBOXUWP-027), area XDEV / XMVVM. FR-XDEV-001..003,
/// TR-XMVVM-001. One peripheral card on the 10-foot Device Setup page. It mirrors the
/// desktop <c>AttachSlotViewModel</c> state (title, media kind, file patterns, attach /
/// read-only / status), and for the two IEC drive slots (<see cref="MediaSlot.Drive8"/>
/// / <see cref="MediaSlot.Drive9"/>) it also carries the drive-model selector
/// (<see cref="AvailableDriveModels"/> / <see cref="SelectedDriveModel"/>) and the
/// true-drive flag (<see cref="IsTrueDrive"/>).
/// </summary>
/// <remarks>
/// Pure MVVM (TR-MVVM-001): it references only the portable contracts
/// (<c>ViceSharp.Abstractions</c>, <c>ViceSharp.Protocol</c>) and holds no engine, host,
/// or XAML reference. The card is a passive observable state holder: all host coordination
/// (attach / eject / true-drive rebuild / single-rig enforcement) lives in the owning
/// <see cref="XboxDeviceSetupViewModel"/>, which mutates this card through its
/// same-assembly (internal) setters. The 1541-II drive model is VICE drive type 1542; see
/// <see cref="DriveModel"/>.
/// </remarks>
public sealed class XboxMediaSlotViewModel : INotifyPropertyChanged
{
    private bool _isAttached;
    private bool _isReadOnly;
    private bool _isTrueDrive;
    private string _status = "Empty";
    private string _filePath = string.Empty;
    private DriveModel _selectedDriveModel;

    /// <summary>
    /// Creates a peripheral card for a media slot.
    /// </summary>
    /// <param name="slot">The host media slot this card drives.</param>
    /// <param name="title">The card's display title (e.g. "Drive 8").</param>
    /// <param name="mediaKind">A short media-kind label (e.g. "Disk", "Tape", "Cart").</param>
    /// <param name="filePatterns">The file-extension glob patterns the picker filters on.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="title"/>, <paramref name="mediaKind"/>, or
    /// <paramref name="filePatterns"/> is <c>null</c>.
    /// </exception>
    public XboxMediaSlotViewModel(MediaSlot slot, string title, string mediaKind, IReadOnlyList<string> filePatterns)
    {
        Slot = slot;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        MediaKind = mediaKind ?? throw new ArgumentNullException(nameof(mediaKind));
        FilePatterns = filePatterns ?? throw new ArgumentNullException(nameof(filePatterns));

        // The True Drive toggle applies only to the IEC drive slots, mirroring the desktop
        // AttachSlotViewModel.SupportsTrueDrive rule. Only those slots surface the
        // drive-model selector; the others expose an empty list.
        if (SupportsTrueDrive)
        {
            AvailableDriveModels = DriveModelCatalog.Implemented;
            _selectedDriveModel = DriveModel.C1541;
        }
        else
        {
            AvailableDriveModels = Array.Empty<DriveModel>();
            _selectedDriveModel = DriveModel.None;
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The host media slot this card drives.</summary>
    public MediaSlot Slot { get; }

    /// <summary>The card's display title (e.g. "Drive 8").</summary>
    public string Title { get; }

    /// <summary>A short media-kind label (e.g. "Disk", "Tape", "Cart").</summary>
    public string MediaKind { get; }

    /// <summary>The file-extension glob patterns the picker filters on (e.g. <c>*.d64</c>).</summary>
    public IReadOnlyList<string> FilePatterns { get; }

    /// <summary>
    /// The drive models offered by this card: <see cref="DriveModelCatalog.Implemented"/>
    /// for the IEC drive slots, otherwise empty.
    /// </summary>
    public IReadOnlyList<DriveModel> AvailableDriveModels { get; }

    /// <summary>
    /// True for the IEC drive slots (<see cref="MediaSlot.Drive8"/> /
    /// <see cref="MediaSlot.Drive9"/>) where the True Drive toggle and drive-model
    /// selector apply. Mirrors the desktop <c>AttachSlotViewModel.SupportsTrueDrive</c>.
    /// </summary>
    public bool SupportsTrueDrive => Slot is MediaSlot.Drive8 or MediaSlot.Drive9;

    /// <summary>Whether media is currently attached to this slot.</summary>
    public bool IsAttached
    {
        get => _isAttached;
        internal set => SetProperty(ref _isAttached, value);
    }

    /// <summary>Whether the attached (or to-be-attached) media is opened read-only.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    /// <summary>
    /// Whether this drive runs as a cycle-accurate emulated (true-drive) rig. Only ever
    /// true for a drive slot, and at most one drive slot at a time (the single-rig
    /// invariant enforced by <see cref="XboxDeviceSetupViewModel"/>).
    /// </summary>
    public bool IsTrueDrive
    {
        get => _isTrueDrive;
        internal set => SetProperty(ref _isTrueDrive, value);
    }

    /// <summary>
    /// The selected drive model for a drive slot (default <see cref="DriveModel.C1541"/>),
    /// or <see cref="DriveModel.None"/> for a non-drive slot.
    /// </summary>
    public DriveModel SelectedDriveModel
    {
        get => _selectedDriveModel;
        internal set => SetProperty(ref _selectedDriveModel, value);
    }

    /// <summary>A short human-readable status for the card ("Empty", the display name, or an error).</summary>
    public string Status
    {
        get => _status;
        internal set => SetProperty(ref _status, value);
    }

    /// <summary>The path of the attached media (empty when the slot is empty).</summary>
    public string FilePath
    {
        get => _filePath;
        internal set => SetProperty(ref _filePath, value);
    }

    internal void ApplyAttachment(MediaAttachmentDto attachment)
    {
        FilePath = attachment.FilePath;
        IsAttached = attachment.IsAttached;
        IsReadOnly = attachment.IsReadOnly;

        var displayName = string.IsNullOrWhiteSpace(attachment.DisplayName)
            ? Path.GetFileName(attachment.FilePath)
            : attachment.DisplayName;

        Status = attachment.IsAttached
            ? attachment.AppliedToRuntime ? displayName : $"{displayName} staged"
            : "Empty";
    }

    internal void MarkEmpty()
    {
        FilePath = string.Empty;
        IsAttached = false;
        Status = "Empty";
    }

    internal void MarkError(string message) =>
        Status = string.IsNullOrWhiteSpace(message) ? "Error" : message;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
