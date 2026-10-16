namespace ViceSharp.Host.Audio;

/// <summary>
/// Pure, allocation-free ring/queue accounting for the future XAudio2
/// source-voice backend (PLAN-XBOXUWP S18; FR-XAUDIO-003, TR-XAUDIO-001/002).
///
/// The desktop <see cref="WinMmAudioBackend"/> applies device back-pressure: a
/// full <c>waveOut</c> queue parks the emulation worker in
/// <c>WaitAndSubmitBuffer</c> until Windows has played enough samples
/// (<see cref="WinMmAudioBackend.DropsSamplesWhenDeviceQueueFull"/> is
/// <see langword="false"/>). The XAudio2 backend must instead stay decoupled and
/// NON-blocking so the deterministic emulation worker never stalls on the audio
/// device. This type holds only the pure math for that decoupled ring; the
/// P/Invoke / XAudio2 interop is layered on top in S18.
///
/// It mirrors the SEMANTICS of
/// <see cref="WinMmAudioBackend.ComputeAvailableBytes(int,int,int)"/>
/// (available = ring - used) but expressed over a ring of
/// <paramref name="bufferFragmentCount"/> fixed-size fragments rather than raw
/// bytes, and with a non-blocking over-run policy:
/// <list type="bullet">
///   <item><description><b>Representation.</b> The ring is tracked as a
///   <c>head</c> (the index of the oldest queued fragment, in
///   <c>[0, bufferFragmentCount)</c>) plus a <c>queuedFragmentCount</c> (the
///   number of fragments queued and not yet consumed, in
///   <c>[0, bufferFragmentCount]</c>). The tail / next free write index is the
///   derived value <c>(head + queuedFragmentCount) % bufferFragmentCount</c>.
///   The explicit count disambiguates the full ring (count ==
///   <c>bufferFragmentCount</c>) from the empty ring (count == 0), which
///   head/tail alone cannot.</description></item>
///   <item><description><b>Over-run (drop-oldest).</b> When the ring is full and
///   the producer submits, the oldest queued fragment is dropped (the consumer
///   head advances one slot) and the new fragment overwrites that slot, so the
///   producer never blocks and the queued count stays at capacity. See
///   <see cref="Enqueue(int,int,int)"/>.</description></item>
///   <item><description><b>Under-run (brief silence).</b> When the ring is empty
///   and the device pulls, the result signals silence with a stable,
///   non-negative index rather than crashing or advancing past empty. See
///   <see cref="Dequeue(int,int,int)"/>.</description></item>
/// </list>
///
/// All members are pure: no static mutable state, no time or randomness;
/// identical inputs always produce identical outputs, and no call allocates.
/// </summary>
public static class XAudio2AudioMath
{
    /// <summary>
    /// Samples per ring fragment, mirroring
    /// <c>WinMmAudioBackend.FragmentSampleCount</c> so the two backends share the
    /// same fragment granularity.
    /// </summary>
    public const int FragmentSampleCount = 256;

    /// <summary>
    /// Number of fixed-size fragments in the ring, mirroring
    /// <c>WinMmAudioBackend.BufferFragmentCount</c>.
    /// </summary>
    public const int BufferFragmentCount = 8;

    /// <summary>
    /// The XAudio2 ring is non-blocking: a full ring drops the oldest queued
    /// fragment on submit rather than applying device back-pressure. This is the
    /// deliberate counterpart to
    /// <see cref="WinMmAudioBackend.DropsSamplesWhenDeviceQueueFull"/> being
    /// <see langword="false"/> on the desktop waveOut path.
    /// </summary>
    public const bool DropsOldestWhenRingFull = true;

    /// <summary>
    /// The number of SID samples currently queued in the ring and not yet
    /// consumed by the device.
    /// </summary>
    /// <param name="queuedFragmentCount">Fragments queued, in
    /// <c>[0, bufferFragmentCount]</c>.</param>
    /// <param name="fragmentSampleCount">Samples per fragment.</param>
    /// <returns><paramref name="queuedFragmentCount"/> *
    /// <paramref name="fragmentSampleCount"/>.</returns>
    public static int QueuedSampleCount(int queuedFragmentCount, int fragmentSampleCount)
        => queuedFragmentCount * fragmentSampleCount;

    /// <summary>
    /// The number of free fragments the producer may still enqueue before the
    /// ring is full.
    /// </summary>
    /// <param name="queuedFragmentCount">Fragments queued, in
    /// <c>[0, bufferFragmentCount]</c>.</param>
    /// <param name="bufferFragmentCount">Total fragments in the ring.</param>
    /// <returns><paramref name="bufferFragmentCount"/> -
    /// <paramref name="queuedFragmentCount"/>.</returns>
    public static int AvailableFragmentCount(int queuedFragmentCount, int bufferFragmentCount)
        => bufferFragmentCount - queuedFragmentCount;

    /// <summary>
    /// The number of free SID samples the producer may still enqueue before the
    /// ring is full.
    /// </summary>
    /// <param name="queuedFragmentCount">Fragments queued, in
    /// <c>[0, bufferFragmentCount]</c>.</param>
    /// <param name="bufferFragmentCount">Total fragments in the ring.</param>
    /// <param name="fragmentSampleCount">Samples per fragment.</param>
    /// <returns>The free fragment count times
    /// <paramref name="fragmentSampleCount"/>.</returns>
    public static int AvailableSampleCount(int queuedFragmentCount, int bufferFragmentCount, int fragmentSampleCount)
        => (bufferFragmentCount - queuedFragmentCount) * fragmentSampleCount;

    /// <summary>
    /// Whether the producer can enqueue another fragment without dropping. A full
    /// ring returns <see langword="false"/> (the producer still never blocks; it
    /// drops-oldest instead - see <see cref="Enqueue(int,int,int)"/>).
    /// </summary>
    /// <param name="queuedFragmentCount">Fragments queued, in
    /// <c>[0, bufferFragmentCount]</c>.</param>
    /// <param name="bufferFragmentCount">Total fragments in the ring.</param>
    /// <returns><see langword="true"/> when
    /// <paramref name="queuedFragmentCount"/> &lt;
    /// <paramref name="bufferFragmentCount"/>.</returns>
    public static bool RoomAvailable(int queuedFragmentCount, int bufferFragmentCount)
        => queuedFragmentCount < bufferFragmentCount;

    /// <summary>
    /// The ring index the producer will write next (the tail), derived from the
    /// consumer head and the queued count and wrapped modulo the fragment count.
    /// </summary>
    /// <param name="head">Index of the oldest queued fragment, in
    /// <c>[0, bufferFragmentCount)</c>.</param>
    /// <param name="queuedFragmentCount">Fragments queued, in
    /// <c>[0, bufferFragmentCount]</c>.</param>
    /// <param name="bufferFragmentCount">Total fragments in the ring.</param>
    /// <returns><c>(head + queuedFragmentCount) % bufferFragmentCount</c>, always
    /// in <c>[0, bufferFragmentCount)</c>.</returns>
    public static int NextFreeBufferIndex(int head, int queuedFragmentCount, int bufferFragmentCount)
        => (head + queuedFragmentCount) % bufferFragmentCount;

    /// <summary>
    /// Computes the ring state after the producer submits one fragment, applying
    /// the non-blocking drop-oldest over-run policy.
    ///
    /// When the ring has room the fragment is appended at
    /// <see cref="NextFreeBufferIndex(int,int,int)"/>, head is unchanged and the
    /// queued count increments. When the ring is full the fragment overwrites the
    /// oldest slot (at <paramref name="head"/>), head advances one slot (mod
    /// capacity) to the next-oldest fragment, the queued count stays at capacity,
    /// and <see cref="WriteResult.DroppedOldest"/> is <see langword="true"/>. The
    /// producer therefore never blocks.
    /// </summary>
    /// <param name="head">Index of the oldest queued fragment, in
    /// <c>[0, bufferFragmentCount)</c>.</param>
    /// <param name="queuedFragmentCount">Fragments queued, in
    /// <c>[0, bufferFragmentCount]</c>.</param>
    /// <param name="bufferFragmentCount">Total fragments in the ring.</param>
    /// <returns>The write slot plus the post-submit head and queued count.</returns>
    public static WriteResult Enqueue(int head, int queuedFragmentCount, int bufferFragmentCount)
    {
        var writeIndex = (head + queuedFragmentCount) % bufferFragmentCount;

        if (queuedFragmentCount >= bufferFragmentCount)
        {
            // Ring full: drop the oldest fragment (advance the consumer) so the
            // producer never blocks; the queued count stays at capacity.
            var advancedHead = (head + 1) % bufferFragmentCount;
            return new WriteResult(writeIndex, advancedHead, bufferFragmentCount, DroppedOldest: true);
        }

        return new WriteResult(writeIndex, head, queuedFragmentCount + 1, DroppedOldest: false);
    }

    /// <summary>
    /// Computes the ring state after the device pulls one fragment, applying the
    /// under-run brief-silence policy.
    ///
    /// When the ring is non-empty the oldest fragment (at
    /// <paramref name="head"/>) is consumed, head advances one slot (mod capacity)
    /// and the queued count decrements. When the ring is empty the result signals
    /// silence with <see cref="ReadResult.ReadIndex"/> left at
    /// <paramref name="head"/> (stable and non-negative), head unchanged and the
    /// queued count still zero - a brief gap of silence, never a crash or a
    /// negative index.
    /// </summary>
    /// <param name="head">Index of the oldest queued fragment, in
    /// <c>[0, bufferFragmentCount)</c>.</param>
    /// <param name="queuedFragmentCount">Fragments queued, in
    /// <c>[0, bufferFragmentCount]</c>.</param>
    /// <param name="bufferFragmentCount">Total fragments in the ring.</param>
    /// <returns>The read slot plus the post-pull head and queued count, with the
    /// silence flag set when the ring was empty.</returns>
    public static ReadResult Dequeue(int head, int queuedFragmentCount, int bufferFragmentCount)
    {
        if (queuedFragmentCount <= 0)
        {
            // Ring empty: under-run. Emit silence, keep the index stable and
            // non-negative, do not advance past empty.
            return new ReadResult(head, head, 0, Silence: true);
        }

        var advancedHead = (head + 1) % bufferFragmentCount;
        return new ReadResult(head, advancedHead, queuedFragmentCount - 1, Silence: false);
    }

    /// <summary>
    /// The outcome of an <see cref="Enqueue(int,int,int)"/>: the ring slot the
    /// fragment was written to plus the resulting head and queued count.
    /// </summary>
    /// <param name="WriteIndex">The ring index the submitted fragment occupies.</param>
    /// <param name="Head">The consumer head after the submit (advanced only when
    /// the oldest fragment was dropped).</param>
    /// <param name="QueuedFragmentCount">The queued fragment count after the
    /// submit.</param>
    /// <param name="DroppedOldest"><see langword="true"/> when the ring was full
    /// and the oldest queued fragment was dropped to make room.</param>
    public readonly record struct WriteResult(
        int WriteIndex,
        int Head,
        int QueuedFragmentCount,
        bool DroppedOldest);

    /// <summary>
    /// The outcome of a <see cref="Dequeue(int,int,int)"/>: the ring slot the
    /// device should read plus the resulting head and queued count.
    /// </summary>
    /// <param name="ReadIndex">The ring index to read (kept stable and
    /// non-negative on under-run).</param>
    /// <param name="Head">The consumer head after the pull (unchanged on
    /// under-run).</param>
    /// <param name="QueuedFragmentCount">The queued fragment count after the pull
    /// (never below zero).</param>
    /// <param name="Silence"><see langword="true"/> when the ring was empty and
    /// the device should emit silence for this fragment.</param>
    public readonly record struct ReadResult(
        int ReadIndex,
        int Head,
        int QueuedFragmentCount,
        bool Silence);
}
