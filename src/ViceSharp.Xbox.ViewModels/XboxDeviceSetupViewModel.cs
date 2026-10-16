namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Protocol;

/// <summary>
/// PLAN-XBOXUWP S27 (IMPL-XBOXUWP-027), area XDEV / XMVVM. FR-XDEV-001..003,
/// TR-XMVVM-001. The portable 10-foot (couch) Device Setup ViewModel. It holds the four
/// fixed peripheral cards (Drive 8 / Drive 9 / Tape / Cartridge) and reproduces the
/// desktop <c>AttachPanelViewModel</c> device behaviors against the host-owned
/// <see cref="IXboxSettingsGateway"/> seam: typed attach + eject through the host media
/// boundary, the per-drive True Drive toggle, the drive-model selector, and the
/// single-true-drive-rig invariant.
/// </summary>
/// <remarks>
/// Pure MVVM (TR-MVVM-001): it references only the portable contracts
/// (<c>ViceSharp.Abstractions</c>, <c>ViceSharp.Protocol</c>) and holds no engine, host,
/// or XAML reference. It is a fresh class that MIRRORS the SEMANTICS of the desktop
/// device panel (it does not reference it):
/// <list type="bullet">
///   <item><description>The four cards use the exact slot kinds and file patterns the
///   desktop uses (<c>AttachPanelViewModel.cs:57-62</c>).</description></item>
///   <item><description>The two drive cards expose <see cref="DriveModelCatalog.Implemented"/>
///   and rebuild the true-drive rig when the model changes while the rig is active; the
///   1541-II flows as VICE drive type 1542 (<c>(int)<see cref="DriveModel.C1541II"/></c>).</description></item>
///   <item><description>Single true-drive rig: enabling one drive's true-drive disables it
///   on the other (<c>AttachPanelViewModel.cs:611-665</c>).</description></item>
/// </list>
/// Unlike the desktop's <c>PropertyChanged</c> async-void trigger, the host side effects are
/// exposed as awaitable intent methods (<see cref="AttachAsync"/>, <see cref="EjectAsync"/>,
/// <see cref="SetTrueDriveAsync"/>, <see cref="SelectDriveModelAsync"/>) so a controller
/// command drives them deterministically and off-console tests can await them.
/// </remarks>
public sealed class XboxDeviceSetupViewModel : INotifyPropertyChanged
{
    private readonly IXboxSettingsGateway _gateway;
    private string _statusText = string.Empty;

    /// <summary>
    /// Creates the Device Setup ViewModel over a host settings/media gateway, building the
    /// four fixed peripheral cards.
    /// </summary>
    /// <param name="gateway">The host-owned settings/media gateway seam.</param>
    /// <exception cref="ArgumentNullException"><paramref name="gateway"/> is <c>null</c>.</exception>
    public XboxDeviceSetupViewModel(IXboxSettingsGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

        Drive8 = new XboxMediaSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", new[] { "*.d64", "*.g64" });
        Drive9 = new XboxMediaSlotViewModel(MediaSlot.Drive9, "Drive 9", "Disk", new[] { "*.d64", "*.g64" });
        Tape = new XboxMediaSlotViewModel(MediaSlot.Tape, "Tape", "Tape", new[] { "*.tap" });
        Cartridge = new XboxMediaSlotViewModel(MediaSlot.Cartridge, "Cartridge", "Cart", new[] { "*.crt", "*.bin", "*.rom" });

        Slots = new[] { Drive8, Drive9, Tape, Cartridge };
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Drive 8 (IEC device 8): disk images, true-drive capable.</summary>
    public XboxMediaSlotViewModel Drive8 { get; }

    /// <summary>Drive 9 (IEC device 9): disk images, true-drive capable.</summary>
    public XboxMediaSlotViewModel Drive9 { get; }

    /// <summary>The datasette (tape) card.</summary>
    public XboxMediaSlotViewModel Tape { get; }

    /// <summary>The cartridge (expansion-port) card.</summary>
    public XboxMediaSlotViewModel Cartridge { get; }

    /// <summary>The four peripheral cards in presentation order (Drive 8, Drive 9, Tape, Cartridge).</summary>
    public IReadOnlyList<XboxMediaSlotViewModel> Slots { get; }

    /// <summary>A short human-readable status message describing the last operation.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Lists the media currently attached to the session and mirrors it onto the cards
    /// (clearing the cards first so a slot the host reports empty ends up empty).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var response = await _gateway.ListMediaAsync(cancellationToken).ConfigureAwait(false);
        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            return;
        }

        foreach (var slot in Slots)
        {
            slot.MarkEmpty();
        }

        foreach (var attachment in response.Attachments)
        {
            FindSlot(attachment.Slot)?.ApplyAttachment(attachment);
        }

        StatusText = "Connected";
    }

    /// <summary>
    /// Attaches a media image to a card's slot through the host media boundary and mirrors
    /// the resulting attachment onto the card. When a non-empty <paramref name="payload"/>
    /// is supplied (the sandboxed-console path, where the head reads the picked file's
    /// bytes), the payload overload is used; otherwise the path-only overload is used.
    /// </summary>
    /// <param name="slot">The card to attach to.</param>
    /// <param name="filePath">The path (or provenance path) of the image to attach.</param>
    /// <param name="isReadOnly">Whether to attach the image read-only.</param>
    /// <param name="payload">Optional image bytes (used on sandboxed consoles).</param>
    /// <param name="displayName">Optional friendly display name for the attachment.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slot"/> is <c>null</c>.</exception>
    public async Task AttachAsync(
        XboxMediaSlotViewModel slot,
        string filePath,
        bool isReadOnly,
        byte[]? payload = null,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            slot.MarkError("No file selected.");
            return;
        }

        var response = payload is { Length: > 0 }
            ? await _gateway
                .AttachMediaAsync(
                    slot.Slot,
                    filePath,
                    isReadOnly,
                    payload,
                    string.IsNullOrWhiteSpace(displayName) ? Path.GetFileName(filePath) : displayName,
                    cancellationToken)
                .ConfigureAwait(false)
            : await _gateway
                .AttachMediaAsync(slot.Slot, filePath, isReadOnly, cancellationToken)
                .ConfigureAwait(false);

        if (!response.Status.IsSuccess)
        {
            slot.MarkError(response.Status.Message);
            StatusText = response.Status.Message;
            return;
        }

        if (response.Attachment is not null)
        {
            slot.ApplyAttachment(response.Attachment);
        }

        StatusText = "Attached";
    }

    /// <summary>
    /// Detaches whatever media occupies a card's slot through the host media boundary and
    /// returns the card to empty.
    /// </summary>
    /// <param name="slot">The card to eject.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slot"/> is <c>null</c>.</exception>
    public async Task EjectAsync(XboxMediaSlotViewModel slot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var response = await _gateway.DetachMediaAsync(slot.Slot, cancellationToken).ConfigureAwait(false);
        if (!response.Status.IsSuccess)
        {
            slot.MarkError(response.Status.Message);
            StatusText = response.Status.Message;
            return;
        }

        slot.MarkEmpty();
        StatusText = "Ejected";
    }

    /// <summary>
    /// Toggles a drive card's true-drive rig. Enabling honors the single-rig invariant by
    /// disabling true-drive on the other drive first, then rebuilds the rig through the
    /// host; disabling turns the rig off. No-op for a non-drive card.
    /// </summary>
    /// <param name="slot">The drive card whose true-drive to toggle.</param>
    /// <param name="enabled">Whether to enable true-drive on this card.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slot"/> is <c>null</c>.</exception>
    public async Task SetTrueDriveAsync(
        XboxMediaSlotViewModel slot,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (!slot.SupportsTrueDrive)
        {
            return;
        }

        if (enabled)
        {
            // Single true-drive rig: enabling one drive disables the other.
            foreach (var other in Slots)
            {
                if (other.SupportsTrueDrive && !ReferenceEquals(other, slot) && other.IsTrueDrive)
                {
                    other.IsTrueDrive = false;
                }
            }

            slot.IsTrueDrive = true;
        }
        else
        {
            slot.IsTrueDrive = false;
        }

        await ApplyTrueDriveSelectionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Selects a drive card's model. When the card's true-drive rig is ACTIVE the rig is
    /// rebuilt with the new model (the 1541-II flowing as VICE drive type 1542); when the
    /// rig is INACTIVE only the pending selection is recorded, with no host call. No-op for
    /// a non-drive card.
    /// </summary>
    /// <param name="slot">The drive card whose model to change.</param>
    /// <param name="model">The drive model to select.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slot"/> is <c>null</c>.</exception>
    public async Task SelectDriveModelAsync(
        XboxMediaSlotViewModel slot,
        DriveModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (!slot.SupportsTrueDrive)
        {
            return;
        }

        slot.SelectedDriveModel = model;

        // Only an ACTIVE true-drive rig rebuilds on a model change; an inactive slot just
        // holds the pending selection until its true-drive is enabled.
        if (!slot.IsTrueDrive)
        {
            return;
        }

        await _gateway
            .SetTrueDriveAsync(
                enabled: true,
                driveDevice: DeviceNumberFor(slot),
                diskImagePath: slot.IsAttached ? slot.FilePath : null,
                driveModel: (int)model,
                cancellationToken)
            .ConfigureAwait(false);

        StatusText = $"Drive model set to {model} for {slot.Title}.";
    }

    private async Task ApplyTrueDriveSelectionAsync(CancellationToken cancellationToken)
    {
        var active = Slots.FirstOrDefault(slot => slot.SupportsTrueDrive && slot.IsTrueDrive);
        var device = active is null ? 8 : DeviceNumberFor(active);
        var diskPath = active is { IsAttached: true } ? active.FilePath : null;
        var model = active is null ? (int)DriveModel.C1541 : (int)active.SelectedDriveModel;

        await _gateway
            .SetTrueDriveAsync(active is not null, device, diskPath, model, cancellationToken)
            .ConfigureAwait(false);

        StatusText = active is not null
            ? $"True Drive enabled for {active.Title}."
            : "True Drive disabled.";
    }

    private static int DeviceNumberFor(XboxMediaSlotViewModel slot) =>
        slot.Slot == MediaSlot.Drive9 ? 9 : 8;

    private XboxMediaSlotViewModel? FindSlot(MediaSlot slot) =>
        Slots.FirstOrDefault(candidate => candidate.Slot == slot);

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
