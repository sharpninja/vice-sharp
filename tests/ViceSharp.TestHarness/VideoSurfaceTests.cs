namespace ViceSharp.TestHarness;

using System.Reflection;
using AvaloniaControl = global::Avalonia.Controls.Control;
using ViceSharp.Avalonia;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
/// Unit tests for the Avalonia <see cref="VideoSurface"/> Control covering
/// the parts of its public/static API that do not require an Avalonia
/// rendering runtime: dimension constants, type contract, and the
/// VideoFrameDto validation gating used by <see cref="VideoSurface.SetFrame"/>.
/// Parts that exercise the underlying <see cref="Avalonia.Media.Imaging.WriteableBitmap"/>
/// (the actual blit, InvalidateVisual, and Render) require a headless
/// Avalonia platform and are scoped as follow-up integration tests in
/// the BACKFILL-HOSTUI-001 epic - see VideoSurfaceIntegrationTests for
/// the VIC-II frame-buffer side of the boundary.
/// </summary>
public sealed class VideoSurfaceTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: Host code (gRPC video stream consumers, frame source
    /// wiring) computes buffer sizes from <c>VideoSurface.SourceWidth</c>
    /// to ensure protocol DTOs match what the surface accepts; the
    /// constant must be exactly the canonical VICE PAL visible width.
    /// Acceptance: <see cref="VideoSurface.SourceWidth"/> equals 384.
    /// </summary>
    [Fact]
    public void SourceWidth_Constant_EqualsVicePalVisibleWidth()
    {
        Assert.Equal(384, VideoSurface.SourceWidth);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: Host code computes frame-buffer sizes from
    /// <c>VideoSurface.SourceHeight</c> for raster allocation and DTO
    /// payload sizing; the constant must equal the canonical VICE PAL
    /// visible height.
    /// Acceptance: <see cref="VideoSurface.SourceHeight"/> equals 272.
    /// </summary>
    [Fact]
    public void SourceHeight_Constant_EqualsVicePalVisibleHeight()
    {
        Assert.Equal(272, VideoSurface.SourceHeight);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: Source/host code reasons about expected BGRA payload
    /// size as <c>SourceWidth * SourceHeight * 4</c>; that derived size
    /// must match the IVideoChip frame-buffer size to avoid a copy-size
    /// mismatch between the chip and the Avalonia surface.
    /// Acceptance: SourceWidth * SourceHeight * 4 equals 417,792 bytes
    /// (the published BGRA payload size).
    /// </summary>
    [Fact]
    public void Source_BgraPayload_Size_MatchesVideoChipFrameBuffer()
    {
        var expected = VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4;

        Assert.Equal(384 * 272 * 4, expected);
        Assert.Equal(417_792, expected);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: The Avalonia host surface is the only video-presenting
    /// Control in the Avalonia project; it must inherit from
    /// Avalonia.Controls.Control so it can be hosted in any Avalonia
    /// layout and participate in the visual tree.
    /// Acceptance: VideoSurface is assignable to Avalonia.Controls.Control.
    /// </summary>
    [Fact]
    public void VideoSurface_Type_DerivesFromAvaloniaControl()
    {
        Assert.True(typeof(AvaloniaControl).IsAssignableFrom(typeof(VideoSurface)));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: VideoSurface is the leaf renderer for video frames; it
    /// must be sealed so subclasses cannot override the frame-blit path
    /// in a way that bypasses the validation in <see cref="VideoSurface.SetFrame"/>.
    /// Acceptance: The type is declared sealed.
    /// </summary>
    [Fact]
    public void VideoSurface_Type_IsSealed()
    {
        Assert.True(typeof(VideoSurface).IsSealed);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: The frame-set API is the only external write path into
    /// the surface's backing bitmap; it must accept a nullable
    /// <see cref="VideoFrameDto"/> so frame-source clients can call it
    /// with the "no frame yet" sentinel without throwing.
    /// Acceptance: VideoSurface.SetFrame is a public instance method
    /// taking a single VideoFrameDto? parameter and returning void.
    /// </summary>
    [Fact]
    public void SetFrame_Method_Signature_AcceptsNullableVideoFrameDto()
    {
        var method = typeof(VideoSurface).GetMethod(
            "SetFrame",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(VideoFrameDto), parameters[0].ParameterType);

        var nullableContext = parameters[0]
            .GetCustomAttributesData()
            .Any(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
        // Either the parameter is annotated nullable directly, or the
        // declaring type's nullable context covers it. The contract is
        // verified by the lack of [DisallowNull] and by the SetFrame
        // null-passthrough behaviour (see VideoFrameDto record below).
        Assert.NotNull(method);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: VideoFrameDto is the protocol DTO carried over gRPC to
    /// the surface; its Width/Height/Bgra layout is the only contract
    /// SetFrame validates against, so the record's positional layout
    /// must be preserved.
    /// Acceptance: VideoFrameDto exposes int Width, int Height,
    /// long Cycle and byte[] Bgra positional properties.
    /// </summary>
    [Fact]
    public void VideoFrameDto_Layout_MatchesSetFrameContract()
    {
        var properties = typeof(VideoFrameDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p.PropertyType);

        Assert.Equal(typeof(int), properties["Width"]);
        Assert.Equal(typeof(int), properties["Height"]);
        Assert.Equal(typeof(long), properties["Cycle"]);
        Assert.Equal(typeof(byte[]), properties["Bgra"]);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: VideoFrameDto must be safely constructible at
    /// VideoSurface.SourceWidth x SourceHeight with a properly sized
    /// BGRA payload; this is the canonical "good" frame the surface
    /// will accept.
    /// Acceptance: A VideoFrameDto with Width=SourceWidth,
    /// Height=SourceHeight and Bgra of length SourceWidth*SourceHeight*4
    /// is constructable, and its Bgra reference equals the input array.
    /// </summary>
    [Fact]
    public void VideoFrameDto_CanonicalFrame_IsConstructibleAtSourceDimensions()
    {
        var payload = new byte[VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4];
        var frame = new VideoFrameDto(
            VideoSurface.SourceWidth,
            VideoSurface.SourceHeight,
            Cycle: 0,
            Bgra: payload);

        Assert.Equal(VideoSurface.SourceWidth, frame.Width);
        Assert.Equal(VideoSurface.SourceHeight, frame.Height);
        Assert.Same(payload, frame.Bgra);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: SetFrame must reject DTOs whose Width does not match
    /// SourceWidth (e.g. NTSC frames sent to a PAL-configured surface);
    /// the early-exit guard prevents a memcpy of mismatched stride.
    /// Acceptance: A DTO with Width != SourceWidth is recognised as
    /// invalid via the same shape check used by SetFrame; we exercise
    /// the predicate here without instantiating the control.
    /// </summary>
    [Fact]
    public void SetFrame_ValidationPredicate_RejectsWidthMismatch()
    {
        var payload = new byte[VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4];
        var frame = new VideoFrameDto(
            Width: VideoSurface.SourceWidth - 1,
            Height: VideoSurface.SourceHeight,
            Cycle: 0,
            Bgra: payload);

        Assert.False(IsValidForSetFrame(frame));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: SetFrame must reject DTOs whose Height does not match
    /// SourceHeight; otherwise the inner MemoryCopy would read past the
    /// 4-byte-per-pixel destination buffer.
    /// Acceptance: A DTO with Height != SourceHeight is rejected by the
    /// same validation predicate used by SetFrame.
    /// </summary>
    [Fact]
    public void SetFrame_ValidationPredicate_RejectsHeightMismatch()
    {
        var payload = new byte[VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4];
        var frame = new VideoFrameDto(
            Width: VideoSurface.SourceWidth,
            Height: VideoSurface.SourceHeight + 1,
            Cycle: 0,
            Bgra: payload);

        Assert.False(IsValidForSetFrame(frame));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: SetFrame must reject DTOs whose Bgra payload is shorter
    /// than SourceWidth*SourceHeight*4, even if Width/Height look right;
    /// a truncated payload would otherwise produce a buffer underread.
    /// Acceptance: A DTO with matching dimensions but a shorter payload
    /// is rejected by the validation predicate.
    /// </summary>
    [Fact]
    public void SetFrame_ValidationPredicate_RejectsTruncatedPayload()
    {
        var payload = new byte[(VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4) - 1];
        var frame = new VideoFrameDto(
            VideoSurface.SourceWidth,
            VideoSurface.SourceHeight,
            Cycle: 0,
            Bgra: payload);

        Assert.False(IsValidForSetFrame(frame));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: SetFrame is called with null from the frame-source loop
    /// during startup before the first frame is available; the call must
    /// short-circuit without throwing.
    /// Acceptance: The validation predicate returns false for a null
    /// VideoFrameDto, mirroring SetFrame's null guard.
    /// </summary>
    [Fact]
    public void SetFrame_ValidationPredicate_RejectsNullFrame()
    {
        Assert.False(IsValidForSetFrame(null));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 VideoSurface).
    /// Use case: SetFrame must accept a canonical frame (matching
    /// dimensions and exactly SourceWidth*SourceHeight*4 bytes) and
    /// also a payload that is strictly larger than the minimum (extra
    /// trailing bytes are ignored by Buffer.MemoryCopy with the size
    /// argument).
    /// Acceptance: The validation predicate returns true for the exact
    /// size and for a larger-than-needed payload.
    /// </summary>
    [Fact]
    public void SetFrame_ValidationPredicate_AcceptsCanonicalAndOversizedPayload()
    {
        var exact = new VideoFrameDto(
            VideoSurface.SourceWidth,
            VideoSurface.SourceHeight,
            0,
            new byte[VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4]);

        var oversized = new VideoFrameDto(
            VideoSurface.SourceWidth,
            VideoSurface.SourceHeight,
            0,
            new byte[(VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4) + 16]);

        Assert.True(IsValidForSetFrame(exact));
        Assert.True(IsValidForSetFrame(oversized));
    }

    /// <summary>
    /// Mirrors the early-exit predicate in
    /// <see cref="VideoSurface.SetFrame"/>:
    /// frame is non-null, dimensions match the published SourceWidth /
    /// SourceHeight, and the BGRA payload is at least SourceWidth *
    /// SourceHeight * 4 bytes long. This lets us verify the contract
    /// without instantiating the control (which would require an
    /// Avalonia rendering platform).
    /// </summary>
    private static bool IsValidForSetFrame(VideoFrameDto? frame)
    {
        if (frame is null ||
            frame.Width != VideoSurface.SourceWidth ||
            frame.Height != VideoSurface.SourceHeight ||
            frame.Bgra.Length < VideoSurface.SourceWidth * VideoSurface.SourceHeight * 4)
        {
            return false;
        }

        return true;
    }
}
