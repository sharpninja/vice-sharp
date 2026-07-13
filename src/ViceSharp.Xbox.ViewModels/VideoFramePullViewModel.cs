namespace ViceSharp.Xbox.ViewModels;

using System;

/// <summary>
/// PLAN-XBOXUWP S23 (IMPL-XBOXUWP-023), area XVIDEO. The pure video frame-pull adapter
/// the UWP video surface drives at its render cadence (~50 Hz). Each <see cref="Tick"/>
/// pulls ONE latest committed frame through <see cref="ILocalVideoFramePull"/> into a
/// reused, geometry-sized buffer that the XAML surface then uploads.
/// </summary>
/// <remarks>
/// <para>
/// It is a PURE SINK (FR-XVIDEO-002): its only dependency is the read-only
/// <see cref="ILocalVideoFramePull"/> pull seam, which exposes only frame-copy and
/// geometry members, so the adapter STRUCTURALLY cannot advance, reset, or otherwise
/// mutate the emulator core. The render pull can therefore never perturb determinism or
/// stall the emulation worker.
/// </para>
/// <para>
/// The pull buffer is allocated exactly ONCE, from the first available
/// <see cref="FrameGeometry"/>, and reused for every subsequent tick, so the ~50 Hz pull
/// allocates nothing on its steady-state path (zero-allocation hot path). There is no
/// lock and no core-advancing call.
/// </para>
/// </remarks>
public sealed class VideoFramePullViewModel
{
    private readonly ILocalVideoFramePull _frames;
    private readonly string _sessionId;

    private byte[]? _buffer;
    private int _bufferLength;
    private int _width;
    private int _height;
    private long _cycle;
    private bool _hasFrame;

    /// <summary>
    /// Creates the adapter over the read-only pull seam and the session it renders.
    /// </summary>
    /// <param name="frames">The lock-free, read-only video-pull seam.</param>
    /// <param name="sessionId">The session whose latest frame is pulled each tick.</param>
    public VideoFramePullViewModel(ILocalVideoFramePull frames, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentException.ThrowIfNullOrEmpty(sessionId);

        _frames = frames;
        _sessionId = sessionId;
    }

    /// <summary>
    /// Creates the adapter over the host facade's shared video-pull surface and the
    /// session it renders.
    /// </summary>
    /// <param name="facade">The emulator-session host facade; its video-pull surface is used.</param>
    /// <param name="sessionId">The session whose latest frame is pulled each tick.</param>
    public VideoFramePullViewModel(IEmulatorSessionFacade facade, string sessionId)
        : this(VideoFramesOf(facade), sessionId)
    {
    }

    /// <summary>The width, in pixels, of the most recently pulled frame (0 before the first).</summary>
    public int Width => _width;

    /// <summary>The height, in pixels, of the most recently pulled frame (0 before the first).</summary>
    public int Height => _height;

    /// <summary>The emulated cycle stamp of the most recently pulled frame (0 before the first).</summary>
    public long Cycle => _cycle;

    /// <summary><c>true</c> once at least one frame has been pulled into the reused buffer.</summary>
    public bool HasFrame => _hasFrame;

    /// <summary>
    /// Read-only view of the reused buffer holding the most recently pulled frame, for
    /// the XAML surface to upload. Empty before the first successful <see cref="Tick"/>.
    /// </summary>
    public ReadOnlySpan<byte> CurrentFrame =>
        _hasFrame && _buffer is not null ? _buffer.AsSpan(0, _bufferLength) : ReadOnlySpan<byte>.Empty;

    /// <summary>
    /// Pulls ONE latest committed frame into the reused buffer. Allocates the buffer once
    /// (on the first available geometry) and never reallocates thereafter.
    /// </summary>
    /// <returns>
    /// <c>true</c> when a frame was copied; <c>false</c> before the first published frame
    /// (or before the session's geometry is available).
    /// </returns>
    public bool Tick()
    {
        var buffer = _buffer;
        if (buffer is null)
        {
            if (!_frames.TryGetFrameGeometry(_sessionId, out var geometry) || geometry.BufferLength <= 0)
                return false;

            buffer = new byte[geometry.BufferLength];
            _buffer = buffer;
            _bufferLength = geometry.BufferLength;
        }

        if (!_frames.TryCopyFrameInto(_sessionId, buffer, out var width, out var height, out var cycle))
            return false;

        _width = width;
        _height = height;
        _cycle = cycle;
        _hasFrame = true;
        return true;
    }

    private static ILocalVideoFramePull VideoFramesOf(IEmulatorSessionFacade facade)
    {
        ArgumentNullException.ThrowIfNull(facade);
        return facade.VideoFrames;
    }
}
