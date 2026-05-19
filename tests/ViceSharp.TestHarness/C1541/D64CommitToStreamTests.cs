namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-1541 (RUNTIME-1541-002 D64 commit-to-stream).
/// Use case: Closes the write-back persistence loop opened by
/// D64DiskImageDevice.WriteSector. CommitToStream writes the current
/// in-memory disk image to a destination stream and clears the dirty
/// flag so callers know the on-disk artifact matches the in-memory
/// view.
/// </summary>
public sealed class D64CommitToStreamTests
{
    private const int Track = 1;
    private const int Sector = 0;
    private const int DiskSize = 174_848;

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002).
    /// Use case: WriteSector + CommitToStream + restore from stream produces
    /// a D64 with the same sector contents.
    /// Acceptance: After round-trip via MemoryStream, the restored image's
    /// sector 1/0 equals the written pattern.
    /// </summary>
    [Fact]
    public void CommitToStream_RoundTripsWrittenSector()
    {
        var device = new D64DiskImageDevice(new D64Image());
        var pattern = new byte[256];
        for (int i = 0; i < 256; i++) pattern[i] = (byte)(i ^ 0x5A);
        device.WriteSector(Track, Sector, pattern);

        using var ms = new MemoryStream();
        device.CommitToStream(ms);

        var restored = new D64DiskImageDevice(new D64Image(ms.ToArray()));
        var actual = new byte[256];
        restored.Image.GetSector(Track, Sector).CopyTo(actual);
        actual.Should().Equal(pattern);
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002).
    /// Use case: CommitToStream clears the dirty flag so persistence
    /// layers know the in-memory + stream views are now in sync.
    /// Acceptance: IsDirty true after WriteSector; false after Commit.
    /// </summary>
    [Fact]
    public void CommitToStream_ClearsDirty()
    {
        var device = new D64DiskImageDevice(new D64Image());
        device.WriteSector(Track, Sector, new byte[256]);
        device.IsDirty.Should().BeTrue();

        using var ms = new MemoryStream();
        device.CommitToStream(ms);

        device.IsDirty.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002).
    /// Use case: CommitToStream on a fresh, untouched device still
    /// writes the full 174,848-byte image (and IsDirty stays false).
    /// Acceptance: Stream length = DiskSize; IsDirty false before + after.
    /// </summary>
    [Fact]
    public void CommitToStream_NoWrites_WritesFullImage_LeavesDirtyFalse()
    {
        var device = new D64DiskImageDevice(new D64Image());
        device.IsDirty.Should().BeFalse();

        using var ms = new MemoryStream();
        device.CommitToStream(ms);

        ms.Length.Should().Be(DiskSize);
        device.IsDirty.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002).
    /// Use case: Round-trip preserves non-written sectors byte-exactly.
    /// Write only one sector; restore from stream; verify other sectors
    /// match the original.
    /// Acceptance: Sector 2/0 (untouched) reads identical bytes after
    /// the WriteSector + Commit + Restore cycle.
    /// </summary>
    [Fact]
    public void CommitToStream_OtherSectorsUnchanged()
    {
        var device = new D64DiskImageDevice(new D64Image());
        var preWriteSector2 = new byte[256];
        device.Image.GetSector(2, 0).CopyTo(preWriteSector2);

        var pattern = new byte[256];
        for (int i = 0; i < 256; i++) pattern[i] = 0xAA;
        device.WriteSector(Track, Sector, pattern);

        using var ms = new MemoryStream();
        device.CommitToStream(ms);

        var restored = new D64DiskImageDevice(new D64Image(ms.ToArray()));
        var actualSector2 = new byte[256];
        restored.Image.GetSector(2, 0).CopyTo(actualSector2);
        actualSector2.Should().Equal(preWriteSector2);
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002).
    /// Use case: CommitToStream always emits exactly 174,848 bytes
    /// (35-track D64 canonical size), regardless of write activity.
    /// Acceptance: Stream length = DiskSize after Commit.
    /// </summary>
    [Fact]
    public void CommitToStream_WritesCanonicalDiskSize()
    {
        var device = new D64DiskImageDevice(new D64Image());
        device.WriteSector(Track, Sector, new byte[256]);

        using var ms = new MemoryStream();
        device.CommitToStream(ms);

        ms.Length.Should().Be(DiskSize);
    }
}
