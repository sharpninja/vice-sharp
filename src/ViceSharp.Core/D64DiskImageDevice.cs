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

    public void Reset() { /* the disk surface persists across reset */ }

    /// <summary>Eject the disk; the device stays in the registry as a stub.</summary>
    public void Eject() => IsEjected = true;

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
