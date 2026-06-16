namespace ViceSharp.Abstractions;

/// <summary>
/// Published by the VIC-II once per scanline at the line boundary, immediately before the completed
/// line is rendered. A host subscriber can react to <see cref="LineToRender"/> to reprogram VIC mode
/// registers ($D011/$D016/$D018) so the just-finished line rasterizes in the desired mode, enabling
/// host-driven raster splits without cycle-stepping the clock from C#.
/// </summary>
public readonly record struct RasterLineEvent(int LineToRender, int CurrentRasterLine)
{
    /// <summary>
    /// Pub/Sub topic used for per-scanline raster notifications.
    /// </summary>
    public static readonly Topic Topic = Topic.FromName("vic.raster-line");
}
