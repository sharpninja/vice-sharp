namespace ViceSharp.Host.Audio;

/// <summary>
/// Converts SID float samples (nominally [-1, 1]) to clamped little-endian
/// 16-bit mono PCM, matching the established RecordingAudioBackend / cbmengine
/// convention (<c>clamp(sample * 32767)</c>). Platform-neutral and pure so it
/// can be unit tested without any audio device.
/// </summary>
public static class AudioSampleConverter
{
    public static int ConvertToPcm16(ReadOnlySpan<float> samples, Span<byte> destination)
    {
        var count = Math.Min(samples.Length, destination.Length / 2);
        for (var i = 0; i < count; i++)
        {
            var scaled = samples[i] * 32767f;
            var s = (short)Math.Clamp(scaled, -32768f, 32767f);
            destination[i * 2] = (byte)(s & 0xFF);
            destination[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        return count;
    }
}
