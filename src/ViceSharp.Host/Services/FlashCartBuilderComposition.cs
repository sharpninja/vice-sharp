using ViceSharp.Abstractions;
using ViceSharp.Core.FlashCarts;

namespace ViceSharp.Host.Services;

/// <summary>
/// Composes the desktop flash cart builder with the host's local file writer.
/// 2026-11-23, TR-MVVM-001 and FR-FLASHCART-001, D6/R18.
/// </summary>
public static class FlashCartBuilderComposition
{
    /// <summary>Creates a builder for a new desktop window.</summary>
    public static IFlashCartImageBuilder Create() => new FlashCartImageBuilderViewModel(new LocalFileIo());

    /// <summary>TR-MVVM-001, 2026-11-23. Writes to the local path selected by the desktop picker.</summary>
    private sealed class LocalFileIo : IFlashBuilderFileIo
    {
        /// <inheritdoc />
        public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default)
            => File.WriteAllBytesAsync(path, data, cancellationToken);
    }
}
