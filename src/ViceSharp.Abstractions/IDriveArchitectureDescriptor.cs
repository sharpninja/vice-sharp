namespace ViceSharp.Abstractions;

/// <summary>
/// Optional descriptor surface for 1541-family drive machines. Allows the
/// builder to resolve the DOS ROM filename without reflection (which is not
/// trim-safe under NativeAOT).
/// </summary>
public interface IDriveArchitectureDescriptor : IArchitectureDescriptor
{
    /// <summary>Filename of the drive DOS ROM image (16KB).</summary>
    string DosRomName { get; }

    /// <summary>Drive device number (8..11) as set on the rear DIP switches.</summary>
    int DeviceNumber { get; }

    /// <summary>Optional D64 disk image path; null = empty drive.</summary>
    string? DiskImagePath { get; }
}
