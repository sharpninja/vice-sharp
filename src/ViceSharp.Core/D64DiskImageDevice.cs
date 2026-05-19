using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core;

/// <summary>
/// A device wrapper exposing a mounted D64 disk image to a 1541-family
/// drive machine. Registered under <see cref="DeviceRole.DriveDisk"/> so
/// VIA2 wiring (Phase G2+) can resolve the disk surface when the drive's
/// read/write head circuitry asks for sector data.
///
/// The wrapper holds the parsed <see cref="D64Image"/> + the original path
/// for diagnostics. Eject by calling <see cref="Eject"/> or replacing the
/// device in the registry with an empty one.
/// </summary>
public sealed class D64DiskImageDevice : IDevice
{
    public D64DiskImageDevice(D64Image image, string? sourcePath = null)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
        SourcePath = sourcePath;
    }

    public DeviceId Id => new DeviceId(0x1400);

    public string Name => $"D64 Disk ({(SourcePath is null ? "in-memory" : Path.GetFileName(SourcePath))})";

    /// <summary>The parsed disk image. Stays non-null until the device is replaced.</summary>
    public D64Image Image { get; private set; }

    /// <summary>Original file path the image was loaded from; null when constructed in-memory.</summary>
    public string? SourcePath { get; }

    /// <summary>True after Eject; reads should fail until a new image is mounted.</summary>
    public bool IsEjected { get; private set; }

    /// <summary>
    /// True after at least one successful WriteSector. Persistence layers
    /// (Commit-to-stream, save-as-file) consult this flag to decide whether
    /// they need to flush. Reset by <see cref="ClearDirty"/>.
    /// </summary>
    public bool IsDirty { get; private set; }

    public void Reset() { /* the disk surface persists across reset */ }

    /// <summary>Eject the disk; the device stays in the registry as a stub.</summary>
    public void Eject() => IsEjected = true;

    /// <summary>
    /// Write a 256-byte sector into the in-memory image. Bounds-checks the
    /// track (1-35) and sector (per-track count: 21 on tracks 1-17, 19 on
    /// 18-24, 18 on 25-30, 17 on 31-35) before touching the buffer; out-of
    /// -range requests raise <see cref="ArgumentOutOfRangeException"/> so a
    /// runaway drive program cannot corrupt the rest of the image. The
    /// payload must be at least 256 bytes; only the first 256 are copied.
    /// Sets <see cref="IsDirty"/> on success. Disk-to-stream persistence is
    /// a follow-up slice.
    /// </summary>
    /// <param name="track">1-based track number, 1-35 inclusive.</param>
    /// <param name="sector">0-based sector index for the given track.</param>
    /// <param name="data">Source bytes (must contain at least 256 bytes).</param>
    /// <exception cref="ArgumentOutOfRangeException">Track or sector outside
    /// the valid D64 layout.</exception>
    /// <exception cref="ArgumentException">Payload is shorter than 256 bytes.</exception>
    public void WriteSector(int track, int sector, ReadOnlySpan<byte> data)
    {
        if (track is < 1 or > 35)
            throw new ArgumentOutOfRangeException(
                nameof(track),
                track,
                "Track must be between 1 and 35 inclusive for a 35-track D64.");

        var sectorCount = SectorsPerTrack(track);
        if (sector < 0 || sector >= sectorCount)
            throw new ArgumentOutOfRangeException(
                nameof(sector),
                sector,
                $"Sector must be between 0 and {sectorCount - 1} for track {track}.");

        if (data.Length < 256)
            throw new ArgumentException(
                $"Sector payload must be at least 256 bytes (was {data.Length}).",
                nameof(data));

        data[..256].CopyTo(Image.GetSector(track, sector));
        IsDirty = true;
    }

    /// <summary>
    /// Reset the dirty flag, e.g. after a persistence layer flushes the
    /// image. Does not touch the in-memory bytes.
    /// </summary>
    public void ClearDirty() => IsDirty = false;

    /// <summary>
    /// Write the current in-memory disk image (174,848 bytes for a 35-track
    /// D64) to the destination stream and clear the dirty flag. Closes the
    /// write-back persistence loop opened by <see cref="WriteSector"/>;
    /// callers can flush to a FileStream, MemoryStream, or any other
    /// writable stream. Does not seek or close the stream.
    /// </summary>
    /// <param name="destination">Writable stream that receives the image bytes.</param>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    public void CommitToStream(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Write(Image.RawData);
        IsDirty = false;
    }

    private static int SectorsPerTrack(int track) => track switch
    {
        >= 1 and <= 17 => 21,
        >= 18 and <= 24 => 19,
        >= 25 and <= 30 => 18,
        >= 31 and <= 35 => 17,
        _ => 0,
    };

    /// <summary>
    /// Load a D64 image from disk + wrap it. Throws if the file is missing or
    /// not 174,848 bytes (the canonical 35-track size).
    /// </summary>
    public static D64DiskImageDevice LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"D64 image not found: {path}", path);
        var bytes = File.ReadAllBytes(path);
        return new D64DiskImageDevice(new D64Image(bytes), path);
    }
}
