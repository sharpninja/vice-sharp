namespace ViceSharp.TestHarness.Media;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ViceSharp.Core.Media;
using Xunit;

/// <summary>
/// FR-MED (review finding: blocking I/O on the emulation worker). The background
/// writer decouples the producer (emulation worker enqueuing copies) from the
/// slow consumer (socket/file writes on a dedicated thread), with ArrayPool-backed
/// payloads so there is no per-write GC churn.
/// </summary>
public sealed class BackgroundByteWriterTests
{
    [Fact]
    public void Enqueue_ThenComplete_WritesAllPayloadsInOrder()
    {
        var written = new List<byte[]>();
        var gate = new object();
        using var writer = new BackgroundByteWriter(
            (buf, len) => { lock (gate) written.Add(buf[..len]); }, capacity: 8, name: "test");

        for (var i = 0; i < 50; i++)
            writer.Enqueue(new byte[] { (byte)i, (byte)(i + 1) });

        writer.CompleteAndJoin(TimeSpan.FromSeconds(5));

        written.Should().HaveCount(50);
        for (var i = 0; i < 50; i++)
        {
            written[i].Should().Equal((byte)i, (byte)(i + 1));
        }
        writer.Faulted.Should().BeFalse();
    }

    [Fact]
    public void Enqueue_AfterComplete_IsIgnored()
    {
        var count = 0;
        var writer = new BackgroundByteWriter((_, _) => Interlocked.Increment(ref count), capacity: 4, name: "test");
        writer.Enqueue(new byte[] { 1 });
        writer.CompleteAndJoin(TimeSpan.FromSeconds(5));

        writer.Enqueue(new byte[] { 2 }); // after complete - dropped

        Volatile.Read(ref count).Should().Be(1);
        writer.Dispose();
    }

    /// <summary>
    /// Bug repro (emulator freezes when video recording starts): when the consumer
    /// (a stalled ffmpeg socket) stops draining, a blocking bounded queue applies
    /// back-pressure onto the producer - which is the emulation worker - freezing the
    /// emulator. A drop-on-full writer must NEVER block the producer; it drops the
    /// overflow and counts it instead.
    /// </summary>
    // xUnit1051: deliberate consumer stall + wall-clock bound to prove non-blocking;
    // TestContext-driven cancellation does not apply to the simulated stall.
#pragma warning disable xUnit1051
    [Fact]
    public async Task Enqueue_WithDropPolicy_WhenConsumerStalls_NeverBlocksProducer()
    {
        using var release = new ManualResetEventSlim(false);
        using var writer = new BackgroundByteWriter(
            (_, _) => release.Wait(), capacity: 2, name: "test", dropWhenFull: true);

        // The single consumer write blocks forever, so the bounded queue saturates at
        // capacity. With drop-on-full the producer must complete the whole burst well
        // inside the timeout rather than blocking on a full queue.
        var producer = Task.Run(() =>
        {
            for (var i = 0; i < 5000; i++)
                writer.Enqueue(new byte[] { (byte)i });
        });

        var finished = await Task.WhenAny(producer, Task.Delay(TimeSpan.FromSeconds(2)));
        finished.Should().BeSameAs(producer,
            "a drop-on-full writer must not apply back-pressure to the emulation worker");
        writer.DroppedCount.Should().BeGreaterThan(0);

        release.Set(); // let the consumer drain so teardown joins promptly
        await producer; // observe completion / surface any producer exception
    }
#pragma warning restore xUnit1051

    /// <summary>
    /// The default (blocking) policy is unchanged: with a consumer that keeps up,
    /// every payload is written and nothing is dropped.
    /// </summary>
    [Fact]
    public void DefaultPolicy_WritesEverything_AndDropsNothing()
    {
        var count = 0;
        using var writer = new BackgroundByteWriter(
            (_, _) => Interlocked.Increment(ref count), capacity: 4, name: "test");

        for (var i = 0; i < 100; i++)
            writer.Enqueue(new byte[] { (byte)i });
        writer.CompleteAndJoin(TimeSpan.FromSeconds(5));

        Volatile.Read(ref count).Should().Be(100);
        writer.DroppedCount.Should().Be(0);
    }

    [Fact]
    public void WriteCallbackThrows_FaultsWriter_WithoutPropagatingToProducer()
    {
        var writer = new BackgroundByteWriter((_, _) => throw new InvalidOperationException("boom"), capacity: 4, name: "test");

        // Producer must never see the consumer's exception.
        var ex = Record.Exception(() =>
        {
            for (var i = 0; i < 20; i++) writer.Enqueue(new byte[] { (byte)i });
            writer.CompleteAndJoin(TimeSpan.FromSeconds(5));
        });

        ex.Should().BeNull();
        writer.Faulted.Should().BeTrue();
        writer.Dispose();
    }
}
