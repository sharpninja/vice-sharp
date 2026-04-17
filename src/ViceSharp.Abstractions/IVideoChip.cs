namespace ViceSharp.Abstractions;

/// <summary>
/// Common interface for all video display chips.
/// </summary>
public interface IVideoChip : IClockedDevice
{
    /// <summary>Current raster line being rendered</summary>
    ushort CurrentRasterLine { get; }

    /// <summary>Number of cycles per raster line</summary>
    int CyclesPerLine { get; }

    /// <summary>Total visible lines per frame</summary>
    int VisibleLines { get; }

    /// <summary>Total lines per frame including vertical blank</summary>
    int TotalLines { get; }

    /// <summary>True when currently in vertical blanking interval</summary>
    bool IsVBlank { get; }
}