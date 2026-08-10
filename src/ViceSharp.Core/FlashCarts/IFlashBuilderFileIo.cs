namespace ViceSharp.Core.FlashCarts;

/// <summary>Platform file read/write for the flash cart image builder ViewModel.</summary>
public interface IFlashBuilderFileIo
{
    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional session hook so heads can attach a built image without the VM knowing MediaService.
/// </summary>
public interface IFlashBuilderSessionHook
{
    Task AttachImageAsync(string profileId, byte[] image, string displayName, CancellationToken cancellationToken = default);
}
