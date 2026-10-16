namespace ViceSharp.Core.FlashCarts;

/// <summary>
/// Machine-agnostic flash/ROM image layout used by the cart image builder.
/// VIC-20 FE3/Ultimem/Mega-Cart and C64 EasyFlash-style profiles implement this.
/// </summary>
public interface IFlashCartProfile
{
    /// <summary>Stable id: <c>fe3</c>, <c>ultimem</c>, <c>megacart</c>, <c>easyflash</c>.</summary>
    string Id { get; }

    /// <summary>UI display name.</summary>
    string DisplayName { get; }

    /// <summary>Primary image size in bytes (flash or combined ROM image).</summary>
    int ImageSizeBytes { get; }

    /// <summary>Logical bank/window size for import grid.</summary>
    int BankSizeBytes { get; }

    /// <summary><see cref="ImageSizeBytes"/> / <see cref="BankSizeBytes"/>.</summary>
    int BankCount { get; }

    /// <summary>Byte used for unfilled flash/ROM (typically <c>0xFF</c>).</summary>
    byte EmptyFill { get; }

    /// <summary>
    /// Optional secondary region size (e.g. Mega-Cart NVRAM). Zero if none.
    /// </summary>
    int SecondarySizeBytes { get; }
}
