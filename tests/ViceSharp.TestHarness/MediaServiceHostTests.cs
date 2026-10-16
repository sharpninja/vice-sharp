namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Chips.IEC;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for the
/// <see cref="MediaServiceHost"/> RPC surface (all three public methods
/// on <see cref="IMediaService"/>: AttachMediaAsync /
/// DetachMediaAsync / ListMediaAsync) at the host-RPC boundary.
/// Complements the existing media coverage in
/// <see cref="ProtocolHostIntegrationTests"/> (real C64
/// disk/tape/cartridge attach success paths backed by
/// MachineTestFactory) by exercising the gaps that suite does not
/// cover: missing-session resolution for every method, pre-cancelled
/// token observation, constructor null guard, request-validation edge
/// cases (no FilePath AND no Payload, non-existent FilePath, empty
/// detach slot, unsupported MediaSlot), and the read-only ListMedia
/// success path on a freshly registered session. Uses
/// <see cref="MinimalHostArchitectureDescriptor"/> so the tests do not
/// require C64 ROM assets on disk; runtime-apply paths report
/// AppliedToRuntime=false in this configuration, which is itself part
/// of the host contract.
/// </summary>
public sealed class MediaServiceHostTests
{
    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls AttachMedia with a session id the host
    /// runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status (carrying the unknown session id) and a null attachment
    /// payload, without touching the filesystem.
    /// </summary>
    [Fact]
    public async Task AttachMediaAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new MediaServiceHost(registry);

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest("ghost-session", MediaSlot.Drive8, "ignored.d64"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.Attachment);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls DetachMedia with a session id the host
    /// runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and a null attachment payload, without consulting any
    /// session's attachment map.
    /// </summary>
    [Fact]
    public async Task DetachMediaAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new MediaServiceHost(registry);

        var response = await service.DetachMediaAsync(
            new DetachMediaRequest("ghost-session", MediaSlot.Drive8),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.Attachment);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls ListMedia with a session id the host
    /// runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and an empty (but non-null) attachments collection so
    /// callers can iterate without a null check.
    /// </summary>
    [Fact]
    public async Task ListMediaAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new MediaServiceHost(registry);

        var response = await service.ListMediaAsync(
            new SessionRequest("ghost-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.NotNull(response.Attachments);
        Assert.Empty(response.Attachments);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls AttachMedia against a valid session but
    /// supplies neither a FilePath nor an inline Payload, so the host
    /// has no media bytes to attach.
    /// Acceptance: The host returns InvalidArgument (not Ok), no
    /// attachment payload, and the message identifies that FilePath or
    /// Payload is required.
    /// </summary>
    [Fact]
    public async Task AttachMediaAsync_NoFilePathOrPayload_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, "   "),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("FilePath", response.Status.Message);
        Assert.Contains("Payload", response.Status.Message);
        Assert.Null(response.Attachment);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls AttachMedia against a valid session
    /// with a FilePath that does not exist on the host filesystem.
    /// Acceptance: The host returns NotFound (not InvalidArgument), the
    /// returned attachment is null, and the message includes the
    /// missing path so the caller can echo it in diagnostics.
    /// </summary>
    [Fact]
    public async Task AttachMediaAsync_MissingFile_ReturnsNotFound()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);
        var missingPath = Path.Combine(Path.GetTempPath(), $"ViceSharp-missing-{Guid.NewGuid():N}.d64");

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, missingPath),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains(missingPath, response.Status.Message);
        Assert.Null(response.Attachment);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls AttachMedia against a valid session
    /// with an inline payload that is not a recognised disk image for
    /// the Drive8 slot.
    /// Acceptance: The host returns InvalidArgument (not Ok), no
    /// attachment payload, and the message identifies that the slot
    /// requires a supported D64 image.
    /// </summary>
    [Fact]
    public async Task AttachMediaAsync_InvalidDiskPayload_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest(
                session.SessionId,
                MediaSlot.Drive8,
                string.Empty,
                "garbage.bin",
                Payload: new byte[] { 0x00, 0x01, 0x02, 0x03 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("D64", response.Status.Message);
        Assert.Null(response.Attachment);
    }

    /// <summary>
    /// FR-DRVTRUE-001 / FR-IECLOAD-001: attaching a T64 converts its first program to D64.
    /// The attachment DTO must retain a readable D64 path after the old session is removed,
    /// because True Drive recreation reopens that path rather than the in-memory payload.
    /// Preserve the source archive, display name, read-only flag, and extracted program bytes.
    /// </summary>
    [Theory]
    [InlineData(MediaSlot.Drive8, false)]
    [InlineData(MediaSlot.Tape, false)]
    [InlineData(MediaSlot.Drive8, true)]
    public async Task AttachMediaAsync_T64_ReturnsD64PathForTrueDriveRecreation(
        MediaSlot requestedSlot,
        bool inlinePayload)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ViceSharp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var sourcePath = Path.Combine(directory, "original.t64");
        byte[] original = BuildMinimalT64();
        File.WriteAllBytes(sourcePath, original);
        string? effectivePath = null;

        try
        {
            var registry = new EmulatorRuntimeRegistry();
            var session = CreateMinimalSession();
            registry.Add(session);
            var service = new MediaServiceHost(registry);
            const string displayName = "Synthetic tape.t64";

            var response = await service.AttachMediaAsync(
                new AttachMediaRequest(
                    session.SessionId,
                    requestedSlot,
                    inlinePayload ? string.Empty : sourcePath,
                    displayName,
                    IsReadOnly: true,
                    Payload: inlinePayload ? original : null),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
            Assert.NotNull(response.Attachment);
            effectivePath = response.Attachment.FilePath;
            Assert.Equal(MediaSlot.Drive8, response.Attachment.Slot);
            Assert.Equal(displayName, response.Attachment.DisplayName);
            Assert.True(response.Attachment.IsReadOnly);
            Assert.Equal(original, File.ReadAllBytes(sourcePath));

            var listed = await new MediaServiceHost(registry).ListMediaAsync(
                new SessionRequest(session.SessionId), TestContext.Current.CancellationToken);
            Assert.Equal(response.Attachment, Assert.Single(listed.Attachments));
            Assert.True(registry.Remove(session.SessionId));

            // This is the same D64 loader used when the True Drive rig reopens the DTO path.
            var reopened = new D64Image(File.ReadAllBytes(effectivePath));
            _ = new D64DiskImageDevice(reopened);
            Assert.Equal(".d64", Path.GetExtension(effectivePath));
            Assert.NotEqual(sourcePath, effectivePath);
            var fileSystem = new D64FileSystem(reopened);
            Assert.True(fileSystem.TryFindFile("*"u8, out D64DirectoryEntry entry));
            Assert.Equal(
                new byte[] { 0x01, 0x08, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 },
                fileSystem.ReadFileStream(entry.StartTrack, entry.StartSector));

            var replacement = CreateMinimalSession();
            registry.Add(replacement);
            var reattached = await new MediaServiceHost(registry).AttachMediaAsync(
                new AttachMediaRequest(replacement.SessionId, MediaSlot.Drive8, effectivePath, displayName),
                TestContext.Current.CancellationToken);
            Assert.Equal(RpcStatusCode.Ok, reattached.Status.Code);
            Assert.Equal(effectivePath, reattached.Attachment!.FilePath);
            Assert.Equal(original, File.ReadAllBytes(sourcePath));
        }
        finally
        {
            if (effectivePath is not null && effectivePath != sourcePath)
                File.Delete(effectivePath);
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// FR-DRVTRUE-001 / FR-IECLOAD-001: a normal D64 keeps its original path and bytes,
    /// so adding durable T64 conversion does not redirect ordinary disk attachments.
    /// </summary>
    [Fact]
    public async Task AttachMediaAsync_D64_PreservesOriginalPathAndBytes()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"ViceSharp-disk-{Guid.NewGuid():N}.d64");
        byte[] original = D64SingleFileBuilder.FromPrg(new byte[] { 0x01, 0x08, 0x60 }, "PROGRAM");
        File.WriteAllBytes(sourcePath, original);
        try
        {
            var registry = new EmulatorRuntimeRegistry();
            var session = CreateMinimalSession();
            registry.Add(session);

            var response = await new MediaServiceHost(registry).AttachMediaAsync(
                new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, sourcePath, "Existing disk.d64"),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
            Assert.NotNull(response.Attachment);
            Assert.Equal(sourcePath, response.Attachment.FilePath);
            Assert.Equal("Existing disk.d64", response.Attachment.DisplayName);
            Assert.Equal(original, File.ReadAllBytes(sourcePath));
            _ = new D64DiskImageDevice(new D64Image(File.ReadAllBytes(response.Attachment.FilePath)));
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    /// <summary>
    /// FR-DRVTRUE-001 / FR-IECLOAD-001: malformed T64 data is rejected without creating
    /// an attachment or modifying the source file, whether its directory or data offset is invalid.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AttachMediaAsync_MalformedT64_RejectsWithoutChangingSource(bool truncatedDirectory)
    {
        byte[] original = BuildMinimalT64();
        if (truncatedDirectory)
            original = original[..80];
        else
            original[0x48] = 0xFF; // The sole program points beyond the end of this archive.
        var sourcePath = Path.Combine(Path.GetTempPath(), $"ViceSharp-invalid-{Guid.NewGuid():N}.t64");
        File.WriteAllBytes(sourcePath, original);
        try
        {
            var registry = new EmulatorRuntimeRegistry();
            var session = CreateMinimalSession();
            registry.Add(session);
            var service = new MediaServiceHost(registry);

            var response = await service.AttachMediaAsync(
                new AttachMediaRequest(session.SessionId, MediaSlot.Tape, sourcePath),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
            Assert.Null(response.Attachment);
            Assert.Equal(original, File.ReadAllBytes(sourcePath));
            var listed = await service.ListMediaAsync(
                new SessionRequest(session.SessionId), TestContext.Current.CancellationToken);
            Assert.Empty(listed.Attachments);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls AttachMedia targeting a MediaSlot value
    /// that is not defined in the enum (e.g. a future-proofing or
    /// downcasting bug at the boundary).
    /// Acceptance: The host returns InvalidArgument and identifies the
    /// unsupported slot in the message so the client can fix the call.
    /// </summary>
    [Fact]
    public async Task AttachMediaAsync_UnsupportedSlot_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest(
                session.SessionId,
                (MediaSlot)0x7F,
                string.Empty,
                "bogus.bin",
                Payload: new byte[] { 0x00 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("slot", response.Status.Message, System.StringComparison.OrdinalIgnoreCase);
        Assert.Null(response.Attachment);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls DetachMedia for a slot on a valid
    /// session, but no media is currently attached to that slot.
    /// Acceptance: The host returns NotFound (not Ok) and the message
    /// identifies which slot was empty so the caller can echo it.
    /// </summary>
    [Fact]
    public async Task DetachMediaAsync_EmptySlot_ReturnsNotFound()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        var response = await service.DetachMediaAsync(
            new DetachMediaRequest(session.SessionId, MediaSlot.Tape),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("Tape", response.Status.Message);
        Assert.Null(response.Attachment);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client calls ListMedia against a freshly registered
    /// minimal-architecture session that has never attached any media.
    /// Acceptance: Status is Ok and the returned attachments collection
    /// is non-null and empty (rather than null, so callers can iterate
    /// without a null check).
    /// </summary>
    [Fact]
    public async Task ListMediaAsync_FreshSession_ReturnsOkWithEmptyCollection()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        var response = await service.ListMediaAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Attachments);
        Assert.Empty(response.Attachments);
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client cancels an AttachMedia RPC before the host
    /// has a chance to service it.
    /// Acceptance: Invoking AttachMediaAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> before any registry
    /// lookup or filesystem access occurs.
    /// </summary>
    [Fact]
    public async Task AttachMediaAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.AttachMediaAsync(
                new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, "ignored.d64"),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client cancels a DetachMedia RPC before the host
    /// has a chance to service it.
    /// Acceptance: DetachMediaAsync with an already-cancelled token
    /// throws <see cref="OperationCanceledException"/> before any
    /// attachment-map mutation occurs.
    /// </summary>
    [Fact]
    public async Task DetachMediaAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.DetachMediaAsync(
                new DetachMediaRequest(session.SessionId, MediaSlot.Drive8),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client cancels a ListMedia RPC before the host has
    /// a chance to service it.
    /// Acceptance: ListMediaAsync with an already-cancelled token
    /// throws <see cref="OperationCanceledException"/> before any
    /// attachment-map projection occurs.
    /// </summary>
    [Fact]
    public async Task ListMediaAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new MediaServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.ListMediaAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-MED (BACKFILL-HOSTUI-001 Media RPC).
    /// Use case: A client constructs a <see cref="MediaServiceHost"/>
    /// without supplying a runtime registry (e.g. through a
    /// misconfigured DI container).
    /// Acceptance: The constructor throws
    /// <see cref="ArgumentNullException"/> immediately, surfacing the
    /// misconfiguration at host startup rather than at first RPC.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MediaServiceHost(null!));
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }

    private static byte[] BuildMinimalT64()
    {
        var image = new byte[104]; // 64-byte header, one 32-byte entry, eight program bytes.
        "C64S tape image file"u8.CopyTo(image);
        image[0x20] = 1;
        image[0x21] = 1;
        image[0x22] = 1;
        image[0x24] = 1;
        image[0x40] = 1;
        image[0x41] = 0x82;
        image[0x42] = 0x01;
        image[0x43] = 0x08;
        image[0x44] = 0x09;
        image[0x45] = 0x08;
        image[0x48] = 96;
        "PROGRAM"u8.CopyTo(image.AsSpan(0x50));
        for (var index = 0; index < 8; index++)
            image[96 + index] = (byte)(0x10 + index);
        return image;
    }
}
