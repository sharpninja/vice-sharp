using System;

namespace ViceSharp.Host.Audio;

/// <summary>
/// Narrow seam over the native XAudio2 source voice so the ring / submit / pause /
/// silent-degrade LOGIC of <see cref="XAudio2SourceVoiceBackend"/> is testable
/// off-console without opening a real audio device (PLAN-XBOXUWP S18,
/// IMPL-XBOXUWP-018; FR-XAUDIO-003, TR-XAUDIO-002).
///
/// <para>Two implementations exist: <see cref="XAudio2SourceVoiceDevice"/> (the real
/// <c>[LibraryImport("xaudio2_9.dll")]</c> blittable vtable interop) and a test fake.
/// The backend takes a factory (defaulting to the real device) so behavioral tests
/// inject a fake that records submitted buffers or fails <see cref="Open"/> to exercise
/// the silent-degrade path, while the real interop still compiles into the assembly so
/// the AOT analyzer / publish check links it.</para>
///
/// <para>All members must be non-blocking: the deterministic emulation worker calls
/// <see cref="SubmitBuffer"/> on the hot path and must never be parked on the device.</para>
/// </summary>
internal interface ISourceVoiceDevice : IDisposable
{
    /// <summary>
    /// Opens the device for the given format and ring geometry. Returns
    /// <see langword="false"/> when no device could be opened (CI / headless /
    /// non-Windows / driver failure) so the backend degrades to a silent no-op.
    /// Must not throw.
    /// </summary>
    /// <param name="sampleRate">Output sample rate in Hz.</param>
    /// <param name="channels">Output channel count.</param>
    /// <param name="fragmentBytes">Bytes in one ring fragment (PCM16).</param>
    /// <param name="bufferFragmentCount">Number of fragments the ring holds.</param>
    /// <returns><see langword="true"/> when the device is open and ready.</returns>
    bool Open(int sampleRate, int channels, int fragmentBytes, int bufferFragmentCount);

    /// <summary>
    /// Submits one fragment of little-endian PCM16 for playback. Non-blocking: the
    /// device copies/queues the data and returns immediately.
    /// </summary>
    /// <param name="pcm">The fragment's little-endian 16-bit PCM bytes.</param>
    void SubmitBuffer(ReadOnlySpan<byte> pcm);

    /// <summary>
    /// The number of fragment buffers the device still holds (submitted but not yet
    /// fully played). The backend reconciles its queued count down against this so the
    /// reported queue falls as the device drains.
    /// </summary>
    int BuffersQueued { get; }

    /// <summary>Starts (or resumes) playback.</summary>
    void Start();

    /// <summary>Stops playback and flushes any queued buffers.</summary>
    void Stop();
}
