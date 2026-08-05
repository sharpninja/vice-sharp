namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-002, TR-XPATH-001. A file the
/// user picked through the storage seam (<see cref="IStoragePicker"/>). It carries provenance
/// (name + path) and its reported length, and defers reading the bytes so an oversize file can
/// be rejected on <see cref="Length"/> BEFORE its contents are ever materialised.
/// </summary>
/// <remarks>
/// The head implements this over the UWP file picker: <see cref="Name"/> is the picked file's
/// display name, <see cref="Path"/> is its provenance path, <see cref="Length"/> is the
/// <c>StorageFile</c> size, and the read delegate streams the bytes on demand.
/// </remarks>
public sealed class PickedFile
{
    private readonly Func<CancellationToken, Task<byte[]>> _readBytesAsync;

    /// <summary>Creates a picked-file handle.</summary>
    /// <param name="name">The picked file's display name.</param>
    /// <param name="path">The picked file's provenance path.</param>
    /// <param name="length">The reported file length in bytes (used by the import ceiling guard).</param>
    /// <param name="readBytesAsync">A delegate that reads the file's bytes on demand.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="path"/>, or <paramref name="readBytesAsync"/> is <c>null</c>.</exception>
    public PickedFile(string name, string path, long length, Func<CancellationToken, Task<byte[]>> readBytesAsync)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Length = length;
        _readBytesAsync = readBytesAsync ?? throw new ArgumentNullException(nameof(readBytesAsync));
    }

    /// <summary>The picked file's display name.</summary>
    public string Name { get; }

    /// <summary>The picked file's provenance path.</summary>
    public string Path { get; }

    /// <summary>The reported file length in bytes.</summary>
    public long Length { get; }

    /// <summary>Reads the file's bytes.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The file's contents.</returns>
    public Task<byte[]> ReadBytesAsync(CancellationToken cancellationToken = default) =>
        _readBytesAsync(cancellationToken);
}
