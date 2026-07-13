namespace ViceSharp.TestHarness.Xbox.Fakes;

using System;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021). Off-console test double for
/// <see cref="ILocalVideoFramePull"/>. Mirrors the semantics of the real
/// <c>LocalVideoFrameSource.TryCopyFrameInto</c>: returns <c>false</c> before the
/// first frame is published (or when the caller's destination is too small), then
/// <c>true</c> with the configured width/height/cycle, filling the destination with
/// a canned byte so the pull ViewModel has deterministic bytes to observe.
/// </summary>
public sealed class FakeVideoFramePull : ILocalVideoFramePull
{
    private bool _hasFrame;

    /// <summary>Configured frame width returned once a frame has been published.</summary>
    public int Width { get; set; } = 384;

    /// <summary>Configured frame height returned once a frame has been published.</summary>
    public int Height { get; set; } = 272;

    /// <summary>Configured cycle stamp returned once a frame has been published.</summary>
    public long Cycle { get; set; } = 0;

    /// <summary>Byte value written to every pixel byte of the destination on a successful copy.</summary>
    public byte FillByte { get; set; } = 0xAB;

    /// <summary>The session id passed to the most recent <see cref="TryCopyFrameInto"/> call.</summary>
    public string? LastRequestedSessionId { get; private set; }

    /// <summary>Number of <see cref="TryCopyFrameInto"/> calls received.</summary>
    public int PullCount { get; private set; }

    /// <summary>
    /// Publishes a canned frame with the current <see cref="Width"/>/<see cref="Height"/>/<see cref="Cycle"/>,
    /// so subsequent pulls succeed (the "first frame committed" transition).
    /// </summary>
    public void PublishFrame() => _hasFrame = true;

    /// <summary>
    /// Publishes a canned frame with an explicit geometry and cycle, so subsequent pulls succeed.
    /// </summary>
    /// <param name="width">The frame width to report.</param>
    /// <param name="height">The frame height to report.</param>
    /// <param name="cycle">The cycle stamp to report.</param>
    public void PublishFrame(int width, int height, long cycle)
    {
        Width = width;
        Height = height;
        Cycle = cycle;
        _hasFrame = true;
    }

    /// <summary>Clears the published frame so pulls return <c>false</c> again (e.g. a stopped session).</summary>
    public void ClearFrame() => _hasFrame = false;

    /// <inheritdoc />
    public bool TryCopyFrameInto(string sessionId, Span<byte> destination, out int width, out int height, out long cycle)
    {
        LastRequestedSessionId = sessionId;
        PullCount++;

        width = 0;
        height = 0;
        cycle = 0;

        // Mirror LocalVideoFrameSource: no frame published yet -> false, UI skips the tick.
        if (!_hasFrame)
        {
            return false;
        }

        // BGRA8888 => 4 bytes per pixel. Too-small destination -> false (UI sizes to BufferLength).
        var required = checked(Width * Height * 4);
        if (destination.Length < required)
        {
            return false;
        }

        destination[..required].Fill(FillByte);
        width = Width;
        height = Height;
        cycle = Cycle;
        return true;
    }
}
