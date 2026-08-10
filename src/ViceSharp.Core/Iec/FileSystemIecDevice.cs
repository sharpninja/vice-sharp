using System.Text;
using ViceSharp.Abstractions;

namespace ViceSharp.Core.Iec;

/// <summary>
/// Host-backed IEC filesystem device (VICE fsdevice / uIEC-style directory attach).
/// Available to any machine with an IEC bus; unit number is operator-selected.
/// </summary>
/// <remarks>
/// MVP: directory listing + sequential LOAD of a named file from a host path.
/// Not a full CBM DOS secondary-channel implementation.
/// </remarks>
public sealed class FileSystemIecDevice : IDevice
{
    private string _rootPath = string.Empty;
    private int _unitNumber;

    public FileSystemIecDevice(int unitNumber = 9)
    {
        SetUnitNumber(unitNumber);
        Id = new DeviceId(0x0E09); // stable device id; unit is logical
    }

    public DeviceId Id { get; }
    public string Name => $"FileSystem IEC unit {UnitNumber}";
    public int UnitNumber => _unitNumber;
    public string RootPath => _rootPath;
    public bool IsAttached => !string.IsNullOrWhiteSpace(_rootPath) && Directory.Exists(_rootPath);

    public void Reset() { }

    /// <summary>
    /// Rebind the logical IEC unit (8-11). Used when settings change unit without
    /// rebuilding the machine. Rejects collision with a reserved true-drive unit.
    /// </summary>
    public void SetUnitNumber(int unitNumber, int? reservedTrueDriveUnit = null)
    {
        if (unitNumber is < 8 or > 11)
            throw new ArgumentOutOfRangeException(nameof(unitNumber), "IEC unit must be 8-11.");
        if (reservedTrueDriveUnit is not null && unitNumber == reservedTrueDriveUnit.Value)
            throw new InvalidOperationException(
                $"Filesystem IEC unit {unitNumber} collides with true-drive unit {reservedTrueDriveUnit.Value}.");
        _unitNumber = unitNumber;
    }

    /// <summary>Attach a host directory (PC path or SD mount). Empty detaches.</summary>
    public void AttachDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _rootPath = string.Empty;
            return;
        }

        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException($"IEC filesystem root not found: '{full}'.");
        _rootPath = full;
    }

    public void Detach() => _rootPath = string.Empty;

    /// <summary>List file names in the attached directory (not recursive).</summary>
    public IReadOnlyList<string> ListFiles()
    {
        EnsureAttached();
        return Directory.GetFiles(_rootPath)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Sequential LOAD of a file by CBM-style name (case-insensitive match on host).
    /// Returns raw bytes; if the file starts with a 2-byte load address those are included.
    /// </summary>
    public byte[] LoadFile(string cbmName)
    {
        EnsureAttached();
        ArgumentException.ThrowIfNullOrWhiteSpace(cbmName);

        var name = cbmName.Trim().Trim('"');
        // Strip ,P,R style suffixes if present
        var comma = name.IndexOf(',');
        if (comma > 0)
            name = name[..comma];

        var match = Directory.GetFiles(_rootPath)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileNameWithoutExtension(f), name, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            throw new FileNotFoundException($"File '{cbmName}' not found in IEC root '{_rootPath}'.");

        return File.ReadAllBytes(match);
    }

    /// <summary>Sequential SAVE of payload under <paramref name="cbmName"/> in the root.</summary>
    public void SaveFile(string cbmName, ReadOnlySpan<byte> data)
    {
        EnsureAttached();
        ArgumentException.ThrowIfNullOrWhiteSpace(cbmName);
        var safe = Path.GetFileName(cbmName.Trim().Trim('"'));
        if (string.IsNullOrWhiteSpace(safe))
            throw new ArgumentException("Invalid save name.", nameof(cbmName));
        var path = Path.Combine(_rootPath, safe);
        File.WriteAllBytes(path, data.ToArray());
    }

    /// <summary>Directory listing as PETSCII-ish text lines for UI/tests.</summary>
    public string FormatDirectoryListing()
    {
        var files = ListFiles();
        var sb = new StringBuilder();
        sb.AppendLine($"0 \"{_rootPath}\" 00 2A");
        foreach (var f in files)
        {
            var len = new FileInfo(Path.Combine(_rootPath, f)).Length;
            var blocks = Math.Max(1, (int)((len + 253) / 254));
            sb.AppendLine($"{blocks} \"{f}\" PRG");
        }

        sb.AppendLine("BLOCKS FREE.");
        return sb.ToString();
    }

    private void EnsureAttached()
    {
        if (!IsAttached)
            throw new InvalidOperationException("No host directory attached to the filesystem IEC device.");
    }
}
