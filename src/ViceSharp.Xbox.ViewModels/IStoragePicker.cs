namespace ViceSharp.Xbox.ViewModels;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-002, TR-XPATH-001. The seam the
/// first-run wizard drives to let the user pick a ROM file from USB/storage for import.
/// </summary>
/// <remarks>
/// The head implements this over the UWP file picker; keeping it behind this seam lets the
/// portable ViewModels stay free of any WinRT reference (TR-MVVM-001).
/// </remarks>
public interface IStoragePicker
{
    /// <summary>Prompts the user to pick a file.</summary>
    /// <param name="ct">A token to cancel the pick.</param>
    /// <returns>The picked file, or <c>null</c> if the user cancelled.</returns>
    Task<PickedFile?> PickAsync(CancellationToken ct = default);
}
