namespace ViceSharp.Xbox.ViewModels;

using System;

/// <summary>
/// FIX-XNTSCFPS-001 (PLAN-XBOXUWP, area XVIDEO). Precomputed nearest-neighbor coordinate
/// maps for the video blit: destination index -> source index, computed ONCE per geometry
/// change so the per-frame hot path is map lookups and row copies instead of two integer
/// divisions per painted pixel (~2.9M divisions per tick at the operator's panel size,
/// which held the render loop to ~22 fps).
/// </summary>
/// <remarks>
/// Portable (System only, TR-MVVM-001) and direction-agnostic: build once for columns
/// (source width -> draw width) and once for rows (content height -> draw height).
/// </remarks>
public static class NearestNeighborMap
{
    /// <summary>
    /// Builds the destination-to-source index map: <c>map[d] = d * sourceLength /
    /// targetLength</c>, clamped inside the source. Non-positive lengths yield an empty map.
    /// </summary>
    /// <param name="sourceLength">The source extent in pixels.</param>
    /// <param name="targetLength">The destination extent in pixels.</param>
    /// <returns>One source index per destination index; empty for degenerate extents.</returns>
    public static int[] Build(int sourceLength, int targetLength)
    {
        if (sourceLength <= 0 || targetLength <= 0)
            return Array.Empty<int>();

        var map = new int[targetLength];
        for (var d = 0; d < targetLength; d++)
        {
            var s = (int)((long)d * sourceLength / targetLength);
            map[d] = s >= sourceLength ? sourceLength - 1 : s;
        }

        return map;
    }
}
