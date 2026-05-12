using ViceSharp.Abstractions;

namespace ViceSharp.Core.Capture;

public sealed class RecordingFrameSink : IFrameSink
{
    private byte[] _lastFrame = [];

    public bool IsReady => true;

    public long FrameCount { get; private set; }

    public ReadOnlyMemory<byte> LastFrame => _lastFrame;

    public void PresentFrame(ReadOnlySpan<byte> pixelData)
    {
        _lastFrame = pixelData.ToArray();
        FrameCount++;
    }

    public Task SaveLastFrameAsync(
        int width,
        int height,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (_lastFrame.Length == 0)
            throw new InvalidOperationException("No frame has been presented.");

        return BmpFrameArtifactWriter.WriteBgraAsync(_lastFrame, width, height, filePath, cancellationToken);
    }
}
