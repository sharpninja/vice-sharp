using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Tape;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class MediaServiceHost : IMediaService
{
    private readonly EmulatorRuntimeRegistry _registry;

    public MediaServiceHost(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ValueTask<AttachMediaResponse> AttachMediaAsync(
        AttachMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new AttachMediaResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        var mediaPath = request.FilePath;
        if (string.IsNullOrWhiteSpace(mediaPath) && request.Payload is { Length: > 0 })
            mediaPath = WritePayloadToHostCache(request);

        if (string.IsNullOrWhiteSpace(mediaPath))
            return ValueTask.FromResult(new AttachMediaResponse(RpcStatus.InvalidArgument("FilePath or Payload is required."), null));

        if (!File.Exists(mediaPath))
            return ValueTask.FromResult(new AttachMediaResponse(RpcStatus.NotFound($"Media file '{mediaPath}' was not found."), null));

        var payload = File.ReadAllBytes(mediaPath);
        lock (session.SyncRoot)
        {
            var validationError = ValidateMedia(session, request.Slot, payload, out var runtimePayload);
            if (!string.IsNullOrEmpty(validationError))
                return ValueTask.FromResult(new AttachMediaResponse(RpcStatus.InvalidArgument(validationError), null));

            var appliedToRuntime = TryApplyMediaToRuntime(session, request.Slot, runtimePayload, out var applyError);
            var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? Path.GetFileName(mediaPath)
                : request.DisplayName;
            var attachment = new MediaAttachmentDto(
                request.Slot,
                mediaPath,
                displayName,
                true,
                request.IsReadOnly,
                appliedToRuntime,
                applyError);
            session.MediaAttachments[request.Slot] = attachment;
            return ValueTask.FromResult(new AttachMediaResponse(RpcStatus.Ok(), attachment));
        }
    }

    public ValueTask<DetachMediaResponse> DetachMediaAsync(
        DetachMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new DetachMediaResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null));

        lock (session.SyncRoot)
        {
            if (!session.MediaAttachments.Remove(request.Slot, out var attachment))
                return ValueTask.FromResult(new DetachMediaResponse(RpcStatus.NotFound($"Media slot '{request.Slot}' is empty."), null));

            var appliedToRuntime = TryDetachMediaFromRuntime(session, request.Slot);
            var detached = attachment with { IsAttached = false, AppliedToRuntime = appliedToRuntime };
            return ValueTask.FromResult(new DetachMediaResponse(RpcStatus.Ok(), detached));
        }
    }

    public ValueTask<ListMediaResponse> ListMediaAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new ListMediaResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), Array.Empty<MediaAttachmentDto>()));

        lock (session.SyncRoot)
        {
            var attachments = session.MediaAttachments
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value)
                .ToArray();
            return ValueTask.FromResult(new ListMediaResponse(RpcStatus.Ok(), attachments));
        }
    }

    private static string WritePayloadToHostCache(AttachMediaRequest request)
    {
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? $"{request.Slot.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}.bin"
            : Path.GetFileName(request.DisplayName);
        var directory = Path.Combine(Path.GetTempPath(), "ViceSharp", "media");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"{Guid.NewGuid():N}-{displayName}");
        File.WriteAllBytes(filePath, request.Payload!);
        return filePath;
    }

    private static string? ValidateMedia(
        EmulatorRuntimeSession session,
        MediaSlot slot,
        byte[] payload,
        out byte[] runtimePayload)
    {
        runtimePayload = payload;
        return slot switch
        {
            MediaSlot.Drive8 => IecD64Attachment.TryAttach(8, payload, out _)
                ? null
                : "Drive 8 media must be a supported D64 image.",
            MediaSlot.Drive9 => IecD64Attachment.TryAttach(9, payload, out _)
                ? null
                : "Drive 9 media must be a supported D64 image.",
            MediaSlot.Tape => TapImage.TryAttach(payload, out _)
                ? null
                : "Tape media must be a supported TAP image.",
            MediaSlot.Cartridge => TryValidateCartridge(session, payload, out runtimePayload),
            _ => $"Media slot '{slot}' is not supported."
        };
    }

    private static string? TryValidateCartridge(
        EmulatorRuntimeSession session,
        byte[] payload,
        out byte[] runtimePayload)
    {
        try
        {
            runtimePayload = StandardCartridgeImage.FromBytes(payload).ToArray();
            return null;
        }
        catch (ArgumentException ex)
        {
            if (IsGameSystemCartridgePayload(session, payload))
            {
                runtimePayload = payload;
                return null;
            }

            runtimePayload = payload;
            return $"Cartridge media must be a supported generic CRT, raw 8K, raw 16K, or profile-compatible C64GS image. {ex.Message}";
        }
    }

    private static bool IsGameSystemCartridgePayload(EmulatorRuntimeSession session, byte[] payload)
    {
        if (payload.Length != StandardCartridgeImage.GameSystemRomSize)
            return false;

        var cartridgePort = session.Machine.Devices.GetAll<ICartridgePort>().SingleOrDefault();
        return cartridgePort?.DefaultMappingMode == CartridgeMappingMode.GameSystem;
    }

    private static bool TryApplyMediaToRuntime(
        EmulatorRuntimeSession session,
        MediaSlot slot,
        byte[] payload,
        out string error)
    {
        error = string.Empty;

        if (slot is MediaSlot.Drive8 or MediaSlot.Drive9)
            return TryApplyDiskToRuntime(session, slot, payload, out error);

        if (slot == MediaSlot.Tape)
            return TryApplyTapeToRuntime(session, payload, out error);

        if (slot != MediaSlot.Cartridge)
            return false;

        var cartridgePort = session.Machine.Devices.GetAll<ICartridgePort>().SingleOrDefault();
        if (cartridgePort is null)
        {
            error = "Runtime has no cartridge port.";
            return false;
        }

        try
        {
            cartridgePort.AttachCartridge(payload, CartridgeMappingMode.Auto);
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryDetachMediaFromRuntime(EmulatorRuntimeSession session, MediaSlot slot)
    {
        if (slot is MediaSlot.Drive8 or MediaSlot.Drive9)
            return TryDetachDiskFromRuntime(session, slot);

        if (slot == MediaSlot.Tape)
            return TryDetachTapeFromRuntime(session);

        if (slot != MediaSlot.Cartridge)
            return false;

        var cartridgePort = session.Machine.Devices.GetAll<ICartridgePort>().SingleOrDefault();
        if (cartridgePort is null)
            return false;

        cartridgePort.EjectCartridge();
        return true;
    }

    private static bool TryApplyDiskToRuntime(
        EmulatorRuntimeSession session,
        MediaSlot slot,
        byte[] payload,
        out string error)
    {
        var driveNumber = ToDriveNumber(slot);
        var drive = session.Machine.Devices.All
            .OfType<IFloppyDrive>()
            .FirstOrDefault(candidate => candidate.DriveNumber == driveNumber);

        if (drive is null)
        {
            error = $"Runtime has no IEC drive {driveNumber}.";
            return false;
        }

        try
        {
            drive.InsertDisk(payload);
            error = string.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryDetachDiskFromRuntime(EmulatorRuntimeSession session, MediaSlot slot)
    {
        var driveNumber = ToDriveNumber(slot);
        var drive = session.Machine.Devices.All
            .OfType<IFloppyDrive>()
            .FirstOrDefault(candidate => candidate.DriveNumber == driveNumber);
        if (drive is null)
            return false;

        drive.EjectDisk();
        return true;
    }

    private static bool TryApplyTapeToRuntime(
        EmulatorRuntimeSession session,
        byte[] payload,
        out string error)
    {
        var tape = session.Machine.Devices.All
            .OfType<ITapeDevice>()
            .SingleOrDefault();

        if (tape is null)
        {
            error = "Runtime has no tape device.";
            return false;
        }

        try
        {
            tape.InsertTape(payload);
            error = string.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryDetachTapeFromRuntime(EmulatorRuntimeSession session)
    {
        var tape = session.Machine.Devices.All
            .OfType<ITapeDevice>()
            .SingleOrDefault();
        if (tape is null)
            return false;

        tape.EjectTape();
        return true;
    }

    private static byte ToDriveNumber(MediaSlot slot)
        => slot == MediaSlot.Drive9 ? (byte)9 : (byte)8;
}
