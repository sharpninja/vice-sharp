namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;

/// <summary>
/// Pure builder that computes the directional XY-focus <see cref="FocusMap"/> the
/// 10-foot (couch) UWP UI binds <c>XYFocusUp/Down/Left/Right</c> against.
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S22 (IMPL-XBOXUWP-022), FR-XBOXUI-002 / TR-XBOXUI-002. The graph math
/// is decoupled from XAML so it is fully unit-testable off-console (TR-MVVM-001). The
/// builder holds no static mutable state; the only allocation is the returned map, so
/// repeated or concurrent builds cannot interfere.
/// </para>
/// </remarks>
public static class FocusMapBuilder
{
    /// <summary>
    /// Builds the directional focus graph for a <paramref name="rows"/> x
    /// <paramref name="columns"/> grid. Each cell (r,c) becomes a node with
    /// Id = <c>idAt(r, c)</c>; its Up/Down/Left/Right neighbors are the ids of the
    /// adjacent cells, or <c>null</c> at a grid edge. There is NO wrap-around: an edge
    /// neighbor is <c>null</c>, never the opposite side.
    /// </summary>
    /// <param name="rows">The number of grid rows; must be positive.</param>
    /// <param name="columns">The number of grid columns; must be positive.</param>
    /// <param name="idAt">
    /// Factory that returns the stable focus id for the cell at (row, column). Called
    /// once per cell; expected to produce unique ids.
    /// </param>
    /// <returns>
    /// A <see cref="FocusMap"/> with exactly <c>rows * columns</c> nodes.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="rows"/> or <paramref name="columns"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="idAt"/> is <c>null</c>.</exception>
    public static FocusMap BuildGrid(int rows, int columns, Func<int, int, string> idAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentNullException.ThrowIfNull(idAt);

        var nodes = new List<FocusNode>(rows * columns);

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                var id = idAt(r, c);
                var up = r > 0 ? idAt(r - 1, c) : null;
                var down = r < rows - 1 ? idAt(r + 1, c) : null;
                var left = c > 0 ? idAt(r, c - 1) : null;
                var right = c < columns - 1 ? idAt(r, c + 1) : null;

                nodes.Add(new FocusNode(id, up, down, left, right));
            }
        }

        return new FocusMap(nodes);
    }
}
