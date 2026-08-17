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
            var validationError = ValidateMedia(
                session,
                request.Slot,
                payload,
                request.DisplayName ?? mediaPath,
                out var runtimePayload,
                out var effectiveSlot);
            if (!string.IsNullOrEmpty(validationError))
                return ValueTask.FromResult(new AttachMediaResponse(RpcStatus.InvalidArgument(validationError), null));

            var appliedToRuntime = TryApplyMediaToRuntime(session, effectiveSlot, runtimePayload, mediaPath, out var applyError);
            var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? Path.GetFileName(mediaPath)
                : request.DisplayName;
            var attachment = new MediaAttachmentDto(
                effectiveSlot,
                mediaPath,
                displayName,
                true,
                request.IsReadOnly,
                appliedToRuntime,
                applyError);
            session.MediaAttachments[effectiveSlot] = attachment;
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
            if (!session.MediaAttachments.TryGetValue(request.Slot, out var attachment))
                return ValueTask.FromResult(new DetachMediaResponse(RpcStatus.NotFound($"Media slot '{request.Slot}' is empty."), null));

            if (!TryDetachMediaFromRuntime(session, request.Slot, attachment, out var detachError))
            {
                var failed = attachment with { Error = detachError };
                return ValueTask.FromResult(new DetachMediaResponse(
                    RpcStatus.FailedPrecondition(detachError),
                    failed));
            }

            session.MediaAttachments.Remove(request.Slot);
            var detached = attachment with
            {
                IsAttached = false,
                AppliedToRuntime = true,
                Error = string.Empty,
            };
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
        string displayName,
        out byte[] runtimePayload,
        out MediaSlot effectiveSlot)
    {
        runtimePayload = payload;
        effectiveSlot = slot;

        // T64 is a PRG archive, not TAP pulse data. Extract the first file into a single-file D64
        // on Drive 8 so LOAD"*",8,1 / autostart work (same approach VICE uses with device traps).
        if (T64Image.TryOpen(payload, out T64Image? t64) && t64 is not null)
        {
            if (!t64.TryExtractFirstProgram(out byte[] prg) || prg.Length < 3)
            {
                return "T64 image has no loadable program entry.";
            }

            string baseName = Path.GetFileNameWithoutExtension(displayName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "PROGRAM";
            }

            runtimePayload = D64SingleFileBuilder.FromPrg(prg, baseName);
            effectiveSlot = MediaSlot.Drive8;
            return null;
        }

        return slot switch
        {
            MediaSlot.Drive8 => IecD64Attachment.TryAttach(8, payload, out _)
                ? null
                : "Drive 8 media must be a supported D64 image (or T64 archive).",
            MediaSlot.Drive9 => IecD64Attachment.TryAttach(9, payload, out _)
                ? null
                : "Drive 9 media must be a supported D64 image.",
            MediaSlot.Tape => TapImage.TryAttach(payload, out _)
                ? null
                : "Tape media must be a supported TAP image (T64 is attached via Drive 8).",
            MediaSlot.Cartridge => TryValidateCartridge(session, payload, out runtimePayload),
            _ => $"Media slot '{slot}' is not supported."
        };
    }

    private static string? TryValidateCartridge(
        EmulatorRuntimeSession session,
        byte[] payload,
        out byte[] runtimePayload)
    {
        // VIC-20 expansion carts (FE3 512K, Ultimem >=1MB, Mega-Cart >=64K) are accepted
        // when the machine exposes IVic20ExpansionCartPort — before C64 CRT validation.
        if (IsVic20ExpansionCartPayload(session, payload))
        {
            runtimePayload = payload;
            return null;
        }

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

            // Vic20 MVP BLK cart (raw up to 16K) when expansion port present
            var hasVic20Port = session.Machine.Devices
                .GetAll<ViceSharp.Core.Vic20.IVic20ExpansionCartPort>()
                .Any();
            if (hasVic20Port && payload.Length is > 0 and <= 0x4000)
            {
                runtimePayload = payload;
                return null;
            }

            runtimePayload = payload;
            return $"Cartridge media must be a supported generic CRT, raw 8K/16K, C64GS, or VIC-20 expansion (FE3/Ultimem/Mega-Cart) image. {ex.Message}";
        }
    }

    /// <summary>
    /// True when payload is a VIC-20 FE3/Ultimem/Mega-Cart sized image on a machine
    /// that has <see cref="ViceSharp.Core.Vic20.IVic20ExpansionCartPort"/>.
    /// </summary>
    private static bool IsVic20ExpansionCartPayload(EmulatorRuntimeSession session, byte[] payload)
    {
        var port = session.Machine.Devices
            .GetAll<ViceSharp.Core.Vic20.IVic20ExpansionCartPort>()
            .FirstOrDefault();
        if (port is null)
            return false;

        // FE3 flash 512K (+ optional NVRAM trailer), Ultimem >= 1MB, Mega-Cart >= 64K
        if (payload.Length >= 0x80000)
            return true;
        if (payload.Length >= 0x10000)
            return true; // Mega-Cart style multi-bank ROM
        return false;
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
        string mediaPath,
        out string error)
    {
        error = string.Empty;

        if (slot is MediaSlot.Drive8 or MediaSlot.Drive9)
            return TryApplyDiskToRuntime(session, slot, payload, out error);

        if (slot == MediaSlot.Tape)
            return TryApplyTapeToRuntime(session, payload, out error);

        if (slot != MediaSlot.Cartridge)
            return false;

        // VIC-20 expansion carts (FE3 / Ultimem / Mega-Cart) when size looks like FE3 flash.
        var vic20Port = session.Machine.Devices.GetAll<ViceSharp.Core.Vic20.IVic20ExpansionCartPort>().FirstOrDefault();
        if (vic20Port is not null && payload.Length >= 0x10000)
        {
            try
            {
                // Prefer FE3 for 512K flash images; larger → Ultimem; Mega-Cart when NVRAM trailing.
                if (payload.Length is >= 0x80000 and <= 0x80000 + 0x2000)
                {
                    vic20Port.AttachFinalExpansion3(payload.AsSpan(0, 0x80000));
                    session.Vic20ExpansionCartKind = "fe3";
                    return true;
                }

                if (payload.Length >= 0x100000)
                {
                    vic20Port.AttachUltimem(payload);
                    session.Vic20ExpansionCartKind = "ultimem";
                    return true;
                }

                // Mega-Cart: ROM banks (at least 64K) plus a durable NVRAM sidecar.
                vic20Port.AttachMegaCart(payload, LoadMegaCartNvram(mediaPath));
                session.Vic20ExpansionCartKind = "megacart";
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

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

    private static byte[] LoadMegaCartNvram(string mediaPath)
    {
        var nvramPath = GetMegaCartNvramPath(mediaPath);
        if (!File.Exists(nvramPath))
            return [];

        var nvram = File.ReadAllBytes(nvramPath);
        if (nvram.Length != ViceSharp.Core.Vic20.MegaCartCartridge.NvramSize)
        {
            throw new InvalidDataException(
                $"Mega-Cart NVRAM sidecar '{nvramPath}' must be exactly " +
                $"{ViceSharp.Core.Vic20.MegaCartCartridge.NvramSize} bytes.");
        }

        return nvram;
    }

    private static string GetMegaCartNvramPath(string mediaPath)
        => Path.GetFullPath(mediaPath) + ".nvram";

    private static void WriteAllBytesAtomically(string path, byte[] data)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Media directory for '{fullPath}' does not exist.");

        var tempPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                stream.Write(data);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (IOException)
            {
                // The original persistence result is more actionable.
            }
            catch (UnauthorizedAccessException)
            {
                // The original persistence result is more actionable.
            }
        }
    }

    private static bool TryDetachMediaFromRuntime(
        EmulatorRuntimeSession session,
        MediaSlot slot,
        MediaAttachmentDto attachment,
        out string error)
    {
        error = string.Empty;
        if (slot is MediaSlot.Drive8 or MediaSlot.Drive9)
        {
            if (TryDetachDiskFromRuntime(session, slot))
                return true;
            error = $"Runtime did not detach media slot '{slot}'.";
            return false;
        }

        if (slot == MediaSlot.Tape)
        {
            if (TryDetachTapeFromRuntime(session))
                return true;
            error = "Runtime did not detach tape media.";
            return false;
        }

        if (slot != MediaSlot.Cartridge)
        {
            error = $"Media slot '{slot}' is not supported.";
            return false;
        }

        // VIC-20 FE3 / Ultimem / Mega-Cart
        var vic20Port = session.Machine.Devices
            .GetAll<ViceSharp.Core.Vic20.IVic20ExpansionCartPort>()
            .FirstOrDefault();
        if (vic20Port is not null
            && vic20Port.AttachedKind != ViceSharp.Core.Vic20.Vic20ExpansionCartKind.None)
        {
            if (vic20Port.WriteBack && !attachment.IsReadOnly)
            {
                try
                {
                    var image = vic20Port.FlushImage();
                    if (image is not null)
                        WriteAllBytesAtomically(attachment.FilePath, image);

                    var nvram = vic20Port.FlushNvram();
                    if (nvram is not null)
                    {
                        WriteAllBytesAtomically(
                            GetMegaCartNvramPath(attachment.FilePath),
                            nvram);
                    }

                    vic20Port.AcknowledgeFlush();
                }
                catch (Exception ex) when (
                    ex is IOException
                    or UnauthorizedAccessException
                    or ArgumentException
                    or NotSupportedException)
                {
                    error = $"VIC-20 cartridge writeback failed: {ex.Message}";
                    return false;
                }
            }

            vic20Port.Eject();
            session.Vic20ExpansionCartKind = "none";
            return true;
        }

        var cartridgePort = session.Machine.Devices.GetAll<ICartridgePort>().SingleOrDefault();
        if (cartridgePort is null)
        {
            error = "Runtime has no attached cartridge port.";
            return false;
        }

        cartridgePort.EjectCartridge();
        return true;
    }

    private static bool TryApplyDiskToRuntime(
        EmulatorRuntimeSession session,
        MediaSlot slot,
        byte[] payload,
        out string error)
    {
        // True-drive rig: the emulated 1541 lives in a coordinator peripheral
        // machine (not the host's devices), so mount the D64 into its drive
        // mechanism directly.
        var mechanism = FindTrueDriveMechanism(session);
        if (mechanism is not null)
        {
            try
            {
                mechanism.Mount(new D64DiskImageDevice(new D64Image(payload)));
                error = string.Empty;
                return true;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
        }

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
        var mechanism = FindTrueDriveMechanism(session);
        if (mechanism is not null)
        {
            mechanism.Mount(null);
            return true;
        }

        var driveNumber = ToDriveNumber(slot);
        var drive = session.Machine.Devices.All
            .OfType<IFloppyDrive>()
            .FirstOrDefault(candidate => candidate.DriveNumber == driveNumber);
        if (drive is null)
            return false;

        drive.EjectDisk();
        return true;
    }

    /// <summary>
    /// The true-drive 1541 mechanism in a coordinator rig session, or null for a
    /// simulated-drive session. The rig currently hosts a single 1541.
    /// </summary>
    private static C1541DriveMechanismDevice? FindTrueDriveMechanism(EmulatorRuntimeSession session)
        => session.Machine is CoordinatorMachine coord
            ? coord.Coordinator.Systems
                .SelectMany(machine => machine.Devices.All)
                .OfType<C1541DriveMechanismDevice>()
                .FirstOrDefault()
            : null;

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
