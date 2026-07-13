namespace ViceSharp.Xbox.ViewModels;

using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Protocol;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021), area XSET/XDEV. The portable settings +
/// device (media/keyboard-map/true-drive) gateway seam OWNED by the 10-foot
/// ViewModels.
/// </summary>
/// <remarks>
/// Every member is an EXISTING <c>IHostProtocolClient</c> signature (using the same
/// <see cref="ViceSharp.Protocol"/> DTOs), narrowed to the settings/device subset the
/// Settings and Device-Setup ViewModels need: the head implements it over the
/// in-process direct client, and the off-console tests bind it to a fake. The Xbox
/// UI reuses the emulator settings/media/input contract rather than inventing a
/// parallel one.
/// </remarks>
public interface IXboxSettingsGateway
{
    /// <summary>Reads the host-canonical session settings.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current session settings.</returns>
    ValueTask<GetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies a settings change through the host pipeline.</summary>
    /// <param name="request">The settings mutation to apply.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The host-adopted settings plus any apply diagnostics.</returns>
    ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Validates the resources referenced by a prospective settings change.</summary>
    /// <param name="request">The prospective settings to validate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The per-resource validation results.</returns>
    ValueTask<ValidateSettingsResourcesResponse> ValidateSettingsResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the media currently attached to the session's slots.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current media attachments.</returns>
    ValueTask<ListMediaResponse> ListMediaAsync(CancellationToken cancellationToken = default);

    /// <summary>Attaches a media image (by path) to a slot.</summary>
    /// <param name="slot">The target media slot.</param>
    /// <param name="filePath">The path of the image to attach.</param>
    /// <param name="isReadOnly">Whether to attach the image read-only.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The resulting attachment.</returns>
    ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        CancellationToken cancellationToken = default);

    /// <summary>Attaches a media image (by in-memory payload) to a slot.</summary>
    /// <param name="slot">The target media slot.</param>
    /// <param name="filePath">The original path (for display / provenance).</param>
    /// <param name="isReadOnly">Whether to attach the image read-only.</param>
    /// <param name="payload">The image bytes to persist and attach.</param>
    /// <param name="displayName">A friendly display name for the attachment.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The resulting attachment.</returns>
    ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>Detaches whatever media occupies a slot.</summary>
    /// <param name="slot">The slot to detach.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The detached attachment, if any.</returns>
    ValueTask<DetachMediaResponse> DetachMediaAsync(
        MediaSlot slot,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the available keyboard maps.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The available keyboard maps.</returns>
    ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(CancellationToken cancellationToken = default);

    /// <summary>Selects (or imports) the active keyboard map.</summary>
    /// <param name="keyboardMapId">The id of the keyboard map to select.</param>
    /// <param name="payload">Optional keyboard-map bytes to import.</param>
    /// <param name="displayName">A friendly display name for an imported map.</param>
    /// <param name="sourcePath">The original path of an imported map.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The selected keyboard map.</returns>
    ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        string keyboardMapId,
        byte[]? payload = null,
        string displayName = "",
        string sourcePath = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects simulated vs emulated (true-drive) for an IEC drive. Because true-drive
    /// is a machine-config choice, the host recreates the session, so attached media
    /// must be re-attached afterwards.
    /// </summary>
    /// <param name="enabled">Whether to enable cycle-accurate true-drive emulation.</param>
    /// <param name="driveDevice">The IEC device number (default 8).</param>
    /// <param name="diskImagePath">An optional disk image to re-attach after the rebuild.</param>
    /// <param name="driveModel">
    /// The drive model to build the rig as, expressed as VICE's canonical drive-type
    /// number (see <see cref="DriveModel"/>): 1541, 1540, or 1542 for the 1541-II.
    /// Defaults to the 1541.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask SetTrueDriveAsync(
        bool enabled,
        int driveDevice = 8,
        string? diskImagePath = null,
        int driveModel = (int)DriveModel.C1541,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the available settings profiles.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The available settings profiles.</returns>
    ValueTask<ListSettingsProfilesResponse> ListSettingsProfilesAsync(CancellationToken cancellationToken = default);
}
