// PLAN-XBOXUWP S40 (IMPL-XBOXUWP-040), area XROM. FR-XROM-002, TR-MVVM-001. #if HAS_UWP in full.
#if HAS_UWP
namespace ViceSharp.Xbox.RomProvisioning;

using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Pickers;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// The head's UWP <see cref="IStoragePicker"/>: a <see cref="FileOpenPicker"/> adapted to the
/// portable <see cref="PickedFile"/>. The picked file's bytes are read lazily inside the
/// delegate, so <see cref="XboxRomProvisioningViewModel.ImportAsync"/> can reject an oversize
/// pick on its reported length BEFORE any bytes are materialised.
/// </summary>
public sealed class UwpStoragePicker : IStoragePicker
{
    /// <inheritdoc />
    public async Task<PickedFile?> PickAsync(CancellationToken ct = default)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Downloads };
        picker.FileTypeFilter.Add(".bin");
        picker.FileTypeFilter.Add("*");

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return null;

        var properties = await file.GetBasicPropertiesAsync();
        return new PickedFile(
            file.Name,
            file.Path,
            (long)properties.Size,
            async _ =>
            {
                var buffer = await FileIO.ReadBufferAsync(file);
                CryptographicBuffer.CopyToByteArray(buffer, out var bytes);
                return bytes;
            });
    }
}
#endif
