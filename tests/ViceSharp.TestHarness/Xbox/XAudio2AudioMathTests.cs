namespace ViceSharp.TestHarness.Xbox;

using System.Linq;
using System.Reflection;
using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S16 (IMPL-XBOXUWP-016), TEST-XAUDIO-002.
/// FR-XAUDIO-003 / TR-XAUDIO-001 / TR-XAUDIO-002: pure ring/queue accounting for
/// the future XAudio2 source-voice backend (S18). Unlike the desktop
/// <c>WinMmAudioBackend</c>, which applies device back-pressure (a full device
/// queue parks the emulation worker), the XAudio2 ring is decoupled and
/// NON-blocking: a full ring drops the oldest queued fragment on submit so the
/// producer (emulation worker) never blocks, and an empty ring answers a device
/// pull with a silence indicator rather than crashing or returning a negative
/// index. These are the pure boundary semantics that <c>XAudio2AudioMath</c>
/// mirrors from <c>WinMmAudioBackend.ComputeAvailableBytes</c>
/// (available = buffer - used) but decoupled from any P/Invoke or blocking wait.
///
/// The ring is <c>bufferFragmentCount</c> fixed-size fragments tracked as a
/// <c>head</c> (index of the oldest queued fragment) plus a
/// <c>queuedFragmentCount</c>; the next free write index / tail is the derived
/// value <c>(head + queuedFragmentCount) % bufferFragmentCount</c>.
///
/// Convention: plain xUnit <c>[Fact]</c>/<c>[Theory]</c> off-console (no
/// <c>[ViceFact]</c>, no <c>Assert.Skip</c>), Category=Xbox.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XAudio2AudioMathTests
{
    private const int N = 8; // bufferFragmentCount, mirrors WinMm BufferFragmentCount.
    private const int F = 256; // fragmentSampleCount, mirrors WinMm FragmentSampleCount.

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-001 (S16), TEST-XAUDIO-002.
    /// Use case: the head reports how many SID samples are still queued in the
    /// XAudio2 ring (queued fragments not yet consumed by the device).
    /// Acceptance: across the full range 0..bufferFragmentCount (empty, partial,
    /// full) QueuedSampleCount == queuedFragmentCount * fragmentSampleCount.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void QueuedSampleCount_AcrossFullRange_IsQueuedTimesFragment(int queued)
    {
        Assert.Equal(queued * F, XAudio2AudioMath.QueuedSampleCount(queued, F));
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-001 (S16), TEST-XAUDIO-002.
    /// Use case: a producer must know whether it can enqueue another fragment
    /// without blocking, and how much room (in samples/fragments) remains.
    /// Acceptance: over 0..bufferFragmentCount, AvailableFragmentCount ==
    /// capacity - queued; AvailableSampleCount + QueuedSampleCount conserves to
    /// capacity*fragment; RoomAvailable == (queued &lt; capacity) so an empty
    /// ring has room and a full ring does not.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Availability_AcrossFullRange_ConservesAndGatesRoom(int queued)
    {
        Assert.Equal(N - queued, XAudio2AudioMath.AvailableFragmentCount(queued, N));
        Assert.Equal((N - queued) * F, XAudio2AudioMath.AvailableSampleCount(queued, N, F));

        // Conservation: queued samples + available samples == whole ring.
        Assert.Equal(
            N * F,
            XAudio2AudioMath.QueuedSampleCount(queued, F) + XAudio2AudioMath.AvailableSampleCount(queued, N, F));

        Assert.Equal(queued < N, XAudio2AudioMath.RoomAvailable(queued, N));
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-001 (S16), TEST-XAUDIO-002.
    /// Use case: an empty ring has full room; a full ring has none.
    /// Acceptance: RoomAvailable(0,N) is true with AvailableSampleCount N*F;
    /// RoomAvailable(N,N) is false with AvailableSampleCount 0.
    /// </summary>
    [Fact]
    public void RoomAvailable_EmptyTrue_FullFalse()
    {
        Assert.True(XAudio2AudioMath.RoomAvailable(0, N));
        Assert.Equal(N * F, XAudio2AudioMath.AvailableSampleCount(0, N, F));

        Assert.False(XAudio2AudioMath.RoomAvailable(N, N));
        Assert.Equal(0, XAudio2AudioMath.AvailableSampleCount(N, N, F));
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-001 (S16), TEST-XAUDIO-002.
    /// Use case: the producer writes into the ring slot derived from head and the
    /// queued count and must wrap modulo the fragment count.
    /// Acceptance: NextFreeBufferIndex(head, queued, N) ==
    /// (head + queued) % N for representative head/queued pairs.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 8, 0)]
    [InlineData(0, 3, 8, 3)]
    [InlineData(0, 7, 8, 7)]
    [InlineData(0, 8, 8, 0)] // head + queued == capacity wraps to 0.
    [InlineData(7, 0, 8, 7)]
    [InlineData(7, 1, 8, 0)] // last slot advances to 0.
    [InlineData(5, 6, 8, 3)] // (5 + 6) % 8 == 3.
    [InlineData(3, 1, 4, 0)] // small ring wrap.
    public void NextFreeBufferIndex_WrapsModuloCapacity(int head, int queued, int capacity, int expected)
    {
        Assert.Equal(expected, XAudio2AudioMath.NextFreeBufferIndex(head, queued, capacity));
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-001 (S16), TEST-XAUDIO-002.
    /// Use case: the write cursor at the last physical slot must wrap to index 0.
    /// Acceptance: NextFreeBufferIndex resolving to capacity-1 stays capacity-1,
    /// and any (head+queued) landing on capacity wraps to 0 (never == capacity,
    /// never negative).
    /// </summary>
    [Fact]
    public void NextFreeBufferIndex_AtCapacity_WrapsToZero()
    {
        Assert.Equal(N - 1, XAudio2AudioMath.NextFreeBufferIndex(N - 1, 0, N));
        Assert.Equal(0, XAudio2AudioMath.NextFreeBufferIndex(N - 1, 1, N));
        Assert.Equal(0, XAudio2AudioMath.NextFreeBufferIndex(0, N, N));

        for (var head = 0; head < N; head++)
        {
            for (var queued = 0; queued <= N; queued++)
            {
                var index = XAudio2AudioMath.NextFreeBufferIndex(head, queued, N);
                Assert.InRange(index, 0, N - 1);
            }
        }
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-002 (S16), TEST-XAUDIO-002.
    /// Use case: from empty, the producer fills the ring fragment by fragment,
    /// writing sequential slots without dropping anything until the ring is full.
    /// Acceptance: successive Enqueue calls write indices 0..N-1, increment the
    /// queued count, leave head at 0, and report DroppedOldest == false.
    /// </summary>
    [Fact]
    public void Enqueue_FillsRingSequentially_WithoutDropping()
    {
        var head = 0;
        var queued = 0;

        for (var i = 0; i < N; i++)
        {
            var write = XAudio2AudioMath.Enqueue(head, queued, N);

            Assert.False(write.DroppedOldest);
            Assert.Equal(i, write.WriteIndex);
            Assert.Equal(0, write.Head);
            Assert.Equal(i + 1, write.QueuedFragmentCount);

            head = write.Head;
            queued = write.QueuedFragmentCount;
        }

        Assert.Equal(N, queued);
        Assert.False(XAudio2AudioMath.RoomAvailable(queued, N));
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-002 (S16), TEST-XAUDIO-002.
    /// Use case: over-run policy. When the ring is full and the emulation worker
    /// submits another fragment, the backend must NOT block; it drops the oldest
    /// queued fragment (drop-oldest) so the producer always makes progress.
    /// Acceptance: RoomAvailable was false; Enqueue reports DroppedOldest == true,
    /// the queued count stays at capacity, the write lands on the old head slot,
    /// and head advances by one (mod capacity) to the new oldest fragment.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void Enqueue_FullRing_DropsOldest_NonBlocking(int head)
    {
        Assert.False(XAudio2AudioMath.RoomAvailable(N, N));

        var write = XAudio2AudioMath.Enqueue(head, N, N);

        Assert.True(write.DroppedOldest);
        Assert.Equal(N, write.QueuedFragmentCount); // capacity unchanged: one dropped, one added.
        Assert.Equal(head, write.WriteIndex); // overwrites the oldest slot.
        Assert.Equal((head + 1) % N, write.Head); // consumer advanced past the dropped fragment.
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-002 (S16), TEST-XAUDIO-002.
    /// Use case: enqueue into a ring that still has room must not trigger the
    /// drop-oldest path.
    /// Acceptance: for a non-full ring, Enqueue reports DroppedOldest == false,
    /// writes (head+queued)%N, keeps head fixed, and increments the queued count.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 4)]
    [InlineData(2, 5)]
    [InlineData(7, 0)]
    [InlineData(6, 7)]
    public void Enqueue_PartialRing_AppendsWithoutDropping(int head, int queued)
    {
        var write = XAudio2AudioMath.Enqueue(head, queued, N);

        Assert.False(write.DroppedOldest);
        Assert.Equal((head + queued) % N, write.WriteIndex);
        Assert.Equal(head, write.Head);
        Assert.Equal(queued + 1, write.QueuedFragmentCount);
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-002 (S16), TEST-XAUDIO-002.
    /// Use case: under-run policy. When the device asks for a fragment but the
    /// ring is empty, the result must indicate silence and leave the read index
    /// stable and non-negative (brief silence, no crash, no negative index).
    /// Acceptance: Dequeue(head, 0, N) reports Silence == true, ReadIndex == head
    /// (in range), head unchanged, queued stays 0; repeated pulls stay stable.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(7)]
    public void Dequeue_EmptyRing_SignalsSilence_IndexStable(int head)
    {
        var read = XAudio2AudioMath.Dequeue(head, 0, N);

        Assert.True(read.Silence);
        Assert.Equal(head, read.ReadIndex);
        Assert.InRange(read.ReadIndex, 0, N - 1);
        Assert.Equal(head, read.Head);
        Assert.Equal(0, read.QueuedFragmentCount);

        // A second under-run pull is identical and stable (never advances, never negative).
        var again = XAudio2AudioMath.Dequeue(read.Head, read.QueuedFragmentCount, N);
        Assert.Equal(read, again);
        Assert.InRange(again.ReadIndex, 0, N - 1);
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-002 (S16), TEST-XAUDIO-002.
    /// Use case: a normal device pull from a non-empty ring consumes the oldest
    /// fragment and advances the consumer.
    /// Acceptance: Dequeue on a non-empty ring reports Silence == false, ReadIndex
    /// == head, head advances (head+1)%N, and the queued count decrements.
    /// </summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 8)]
    [InlineData(6, 3)]
    [InlineData(7, 1)]
    public void Dequeue_NonEmptyRing_AdvancesHead(int head, int queued)
    {
        var read = XAudio2AudioMath.Dequeue(head, queued, N);

        Assert.False(read.Silence);
        Assert.Equal(head, read.ReadIndex);
        Assert.Equal((head + 1) % N, read.Head);
        Assert.Equal(queued - 1, read.QueuedFragmentCount);
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-002 (S16), TEST-XAUDIO-002.
    /// Use case: a full producer/consumer round-trip - fill, over-run with drops,
    /// then drain to empty with under-runs - must maintain the ring invariants
    /// end to end (the non-blocking contract never yields an invalid state).
    /// Acceptance: at every step the queued count stays within [0, capacity], head
    /// and every write/read index stay within [0, capacity), Enqueue on a full
    /// ring always drops-oldest (never blocks) and Dequeue on an empty ring always
    /// signals silence with a stable, non-negative index.
    /// </summary>
    [Fact]
    public void ProducerConsumer_RoundTrip_HoldsRingInvariants()
    {
        var head = 0;
        var queued = 0;

        // Submit far more fragments than the ring holds: never blocks, drops oldest once full.
        for (var i = 0; i < N * 3; i++)
        {
            var write = XAudio2AudioMath.Enqueue(head, queued, N);

            Assert.InRange(write.WriteIndex, 0, N - 1);
            Assert.InRange(write.Head, 0, N - 1);
            Assert.InRange(write.QueuedFragmentCount, 0, N);
            Assert.Equal(i < N ? false : true, write.DroppedOldest); // no drops until full, then always.

            head = write.Head;
            queued = write.QueuedFragmentCount;
        }

        Assert.Equal(N, queued); // ring saturated, not overflowed.

        // Drain past empty: consumes to 0 then reports silence without going negative.
        for (var i = 0; i < N * 2; i++)
        {
            var read = XAudio2AudioMath.Dequeue(head, queued, N);

            Assert.InRange(read.ReadIndex, 0, N - 1);
            Assert.InRange(read.Head, 0, N - 1);
            Assert.InRange(read.QueuedFragmentCount, 0, N);
            Assert.Equal(queued == 0, read.Silence); // silence exactly when the ring was empty.

            head = read.Head;
            queued = read.QueuedFragmentCount;
        }

        Assert.Equal(0, queued);
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-001 (S16), TEST-XAUDIO-002.
    /// Use case: the math must be pure - identical inputs always yield identical
    /// outputs (determinism), a prerequisite for reproducible audio pacing.
    /// Acceptance: each function called twice with identical arguments returns an
    /// equal result.
    /// </summary>
    [Fact]
    public void Functions_ArePure_IdenticalArgsYieldIdenticalResults()
    {
        Assert.Equal(XAudio2AudioMath.QueuedSampleCount(5, F), XAudio2AudioMath.QueuedSampleCount(5, F));
        Assert.Equal(XAudio2AudioMath.AvailableFragmentCount(5, N), XAudio2AudioMath.AvailableFragmentCount(5, N));
        Assert.Equal(XAudio2AudioMath.AvailableSampleCount(5, N, F), XAudio2AudioMath.AvailableSampleCount(5, N, F));
        Assert.Equal(XAudio2AudioMath.RoomAvailable(5, N), XAudio2AudioMath.RoomAvailable(5, N));
        Assert.Equal(XAudio2AudioMath.NextFreeBufferIndex(6, 5, N), XAudio2AudioMath.NextFreeBufferIndex(6, 5, N));
        Assert.Equal(XAudio2AudioMath.Enqueue(7, N, N), XAudio2AudioMath.Enqueue(7, N, N));
        Assert.Equal(XAudio2AudioMath.Dequeue(3, 0, N), XAudio2AudioMath.Dequeue(3, 0, N));
        Assert.Equal(XAudio2AudioMath.Dequeue(3, 2, N), XAudio2AudioMath.Dequeue(3, 2, N));
    }

    /// <summary>
    /// FR-XAUDIO-003 / TR-XAUDIO-001 (S16), TEST-XAUDIO-002.
    /// Use case: purity guard - the math type must hold no static mutable state
    /// (no shared counters, no time/random), so it is thread-safe and
    /// allocation-free by construction.
    /// Acceptance: reflection over XAudio2AudioMath finds no mutable static field
    /// (every field is a compile-time constant or read-only).
    /// </summary>
    [Fact]
    public void Type_HasNoStaticMutableFields()
    {
        var mutable = typeof(XAudio2AudioMath)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.Name)
            .ToArray();

        Assert.Empty(mutable);
    }
}
