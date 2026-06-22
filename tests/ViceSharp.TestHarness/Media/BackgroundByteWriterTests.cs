namespace ViceSharp.TestHarness.Media;

using System;
using System.Collections.Generic;
using System.Threading;
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
