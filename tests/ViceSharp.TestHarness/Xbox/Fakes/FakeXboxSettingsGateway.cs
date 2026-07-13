namespace ViceSharp.TestHarness.Xbox.Fakes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Protocol;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021). Off-console test double for
/// <see cref="IXboxSettingsGateway"/>, modeled on the harness's
/// <c>FakeHostProtocolClient</c> so DTO construction matches the real host contract.
/// Records the settings/media/device calls and returns canned
/// <see cref="ViceSharp.Protocol"/> DTOs so the Settings / Device-Setup ViewModels
/// can be exercised without a live host.
/// </summary>
public sealed class FakeXboxSettingsGateway : IXboxSettingsGateway
{
    private readonly Dictionary<MediaSlot, MediaAttachmentDto> _attachments = new();

    /// <summary>Number of <see cref="GetSettingsAsync"/> calls received.</summary>
    public int GetSettingsCount { get; private set; }

    /// <summary>The request passed to the most recent <see cref="UpdateSettingsAsync"/> call.</summary>
    public UpdateSettingsRequest? LastUpdateRequest { get; private set; }

    /// <summary>The request passed to the most recent <see cref="ValidateSettingsResourcesAsync"/> call.</summary>
    public ValidateSettingsResourcesRequest? LastValidateRequest { get; private set; }

    /// <summary>The slot passed to the most recent attach call.</summary>
    public MediaSlot? AttachedSlot { get; private set; }

    /// <summary>The file path passed to the most recent attach call.</summary>
    public string? AttachedPath { get; private set; }

    /// <summary>The read-only flag passed to the most recent attach call.</summary>
    public bool AttachedReadOnly { get; private set; }

    /// <summary>The payload passed to the most recent attach call (empty for the path-only overload).</summary>
    public byte[]? AttachedPayload { get; private set; }

    /// <summary>The display name passed to the most recent attach call.</summary>
    public string? AttachedDisplayName { get; private set; }

    /// <summary>The slot passed to the most recent <see cref="DetachMediaAsync"/> call.</summary>
    public MediaSlot? DetachedSlot { get; private set; }

    /// <summary>The keyboard-map id passed to the most recent <see cref="SetKeyboardMapAsync"/> call.</summary>
    public string? LastKeyboardMapId { get; private set; }

    /// <summary>The enabled flag passed to the most recent <see cref="SetTrueDriveAsync"/> call.</summary>
    public bool? TrueDrive { get; private set; }

    /// <summary>The drive device passed to the most recent <see cref="SetTrueDriveAsync"/> call.</summary>
    public int? TrueDriveDevice { get; private set; }

    /// <summary>The disk image path passed to the most recent <see cref="SetTrueDriveAsync"/> call.</summary>
    public string? TrueDriveDiskImagePath { get; private set; }

    /// <inheritdoc />
    public ValueTask<GetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetSettingsCount++;
        return ValueTask.FromResult(new GetSettingsResponse(
            RpcStatus.Ok(),
            new SessionSettingsDto(
                "c64",
                new LimiterSettingsDto(100, true),
                new DisplaySettingsDto("host", "vice", true, true, "2x", "visible-area", "vice-pixel-aspect"),
                new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick2, false, "keyboard-joystick"),
                new AudioSettingsDto("enabled"),
                new ResourceSettingsDto("auto-detect"))));
    }

    /// <inheritdoc />
    public ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastUpdateRequest = request;
        return ValueTask.FromResult(new UpdateSettingsResponse(
            RpcStatus.Ok(),
            new SessionSettingsDto(
                string.IsNullOrWhiteSpace(request.ProfileId) ? "c64" : request.ProfileId,
                request.Limiter ?? new LimiterSettingsDto(),
                request.Display ?? new DisplaySettingsDto(),
                request.Input ?? new InputSettingsDto(),
                request.Audio ?? new AudioSettingsDto(),
                request.Resources ?? new ResourceSettingsDto()),
            Array.Empty<SettingApplyDiagnosticDto>()));
    }

    /// <inheritdoc />
    public ValueTask<ValidateSettingsResourcesResponse> ValidateSettingsResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastValidateRequest = request;
        return ValueTask.FromResult(new ValidateSettingsResourcesResponse(
            RpcStatus.Ok(),
            Array.Empty<SettingsResourceValidationDto>()));
    }

    /// <inheritdoc />
    public ValueTask<ListMediaResponse> ListMediaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ListMediaResponse(RpcStatus.Ok(), _attachments.Values.ToArray()));
    }

    /// <inheritdoc />
    public ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        CancellationToken cancellationToken = default)
        => AttachMediaAsync(slot, filePath, isReadOnly, Array.Empty<byte>(), string.Empty, cancellationToken);

    /// <inheritdoc />
    public ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AttachedSlot = slot;
        AttachedPath = filePath;
        AttachedReadOnly = isReadOnly;
        AttachedPayload = payload;
        AttachedDisplayName = displayName;

        var attachment = new MediaAttachmentDto(
            slot,
            filePath,
            string.IsNullOrWhiteSpace(displayName) ? System.IO.Path.GetFileName(filePath) : displayName,
            true,
            isReadOnly,
            true);
        _attachments[slot] = attachment;

        return ValueTask.FromResult(new AttachMediaResponse(RpcStatus.Ok(), attachment));
    }

    /// <inheritdoc />
    public ValueTask<DetachMediaResponse> DetachMediaAsync(
        MediaSlot slot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DetachedSlot = slot;
        _attachments.Remove(slot, out var attachment);
        return ValueTask.FromResult(new DetachMediaResponse(RpcStatus.Ok(), attachment));
    }

    /// <inheritdoc />
    public ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ListKeyboardMapsResponse(RpcStatus.Ok(), Array.Empty<KeyboardMapDto>()));
    }

    /// <inheritdoc />
    public ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        string keyboardMapId,
        byte[]? payload = null,
        string displayName = "",
        string sourcePath = "",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastKeyboardMapId = keyboardMapId;
        return ValueTask.FromResult(new KeyboardMapResponse(RpcStatus.Ok(), null));
    }

    /// <inheritdoc />
    public ValueTask SetTrueDriveAsync(
        bool enabled,
        int driveDevice = 8,
        string? diskImagePath = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TrueDrive = enabled;
        TrueDriveDevice = driveDevice;
        TrueDriveDiskImagePath = diskImagePath;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<ListSettingsProfilesResponse> ListSettingsProfilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ListSettingsProfilesResponse(
            RpcStatus.Ok(),
            new[]
            {
                new SettingsProfileDto("c64", "C64 PAL", "x64sc", true, true, "test profile"),
            }));
    }
}
