namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S22 (IMPL-XBOXUWP-022). TEST-XBOXUI-002a: the pure,
/// unit-testable directional XY-focus grid graph in
/// <see cref="FocusMapBuilder"/> (<c>ViceSharp.Xbox.ViewModels</c>).
/// </summary>
/// <remarks>
/// <para>
/// The 10-foot (couch) UWP UI binds each focusable control's
/// <c>XYFocusUp/Down/Left/Right</c> to the neighbor that
/// <see cref="FocusMapBuilder.BuildGrid(int, int, Func{int, int, string})"/>
/// computes for a rows x columns grid. Each node (r,c) has Id = idAt(r,c);
/// Up = idAt(r-1,c) or null at the top edge (r==0); Down = idAt(r+1,c) or null
/// at the bottom edge; Left = idAt(r,c-1) or null at the left edge (c==0);
/// Right = idAt(r,c+1) or null at the right edge. There is NO wrap-around: edges
/// are null, never the opposite side.
/// </para>
/// <para>
/// The builder is PURE: no static mutable state, and the only allocation is the
/// returned <see cref="FocusMap"/>. Because the graph math is decoupled from XAML
/// it is fully testable off-console (TR-MVVM-001).
/// </para>
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class FocusMapBuilderTests
{
    /// <summary>
    /// FR-XBOXUI-002, TR-XBOXUI-002 (IMPL-XBOXUWP-022), TEST-XBOXUI-002a
    /// interior / edge / no-wrap guard.
    /// Use case: a 3x2 grid of focusable tiles wires its directional focus so an
    /// interior row navigates up and down to the correct neighbors while the outer
    /// edges dead-end (null) instead of wrapping to the opposite side.
    /// Acceptance: BuildGrid(3, 2, (r,c) =&gt; $"n{r}{c}") yields 6 nodes; the
    /// interior node n10 has Up == "n00" and Down == "n20"; the top-left node n00
    /// has Up == null and Left == null; the right-edge node n01 has Right == null
    /// (no wrap to n00); the bottom-edge node n21 has Down == null.
    /// </summary>
    [Fact]
    public void BuildGrid_ThreeByTwo_WiresInteriorAndEdgeNeighborsWithoutWrap()
    {
        var map = FocusMapBuilder.BuildGrid(3, 2, (r, c) => $"n{r}{c}");

        Assert.Equal(6, map.Nodes.Count);

        // Interior node (row 1, col 0): up and down cross to the adjacent rows.
        var n10 = map["n10"];
        Assert.Equal("n00", n10.Up);
        Assert.Equal("n20", n10.Down);
        Assert.Null(n10.Left);   // left edge (c == 0)
        Assert.Equal("n11", n10.Right);

        // Top-left node (row 0, col 0): both the top and left edges dead-end.
        var n00 = map["n00"];
        Assert.Null(n00.Up);     // top edge (r == 0)
        Assert.Null(n00.Left);   // left edge (c == 0)
        Assert.Equal("n10", n00.Down);
        Assert.Equal("n01", n00.Right);

        // Right-edge node (row 0, col 1): Right dead-ends, it does NOT wrap to n00.
        var n01 = map["n01"];
        Assert.Null(n01.Right);  // right edge (c == columns - 1), no wrap
        Assert.Null(n01.Up);
        Assert.Equal("n00", n01.Left);
        Assert.Equal("n11", n01.Down);

        // Bottom-edge node (row 2, col 1): Down dead-ends, it does NOT wrap to n01.
        var n21 = map["n21"];
        Assert.Null(n21.Down);   // bottom edge (r == rows - 1), no wrap
        Assert.Null(n21.Right);  // right edge (c == columns - 1)
        Assert.Equal("n11", n21.Up);
        Assert.Equal("n20", n21.Left);
    }

    /// <summary>
    /// FR-XBOXUI-002, TR-XBOXUI-002 (IMPL-XBOXUWP-022), TEST-XBOXUI-002a
    /// single-cell guard.
    /// Use case: a 1x1 focus region (a lone tile) must have no directional
    /// neighbors at all rather than pointing at itself.
    /// Acceptance: BuildGrid(1, 1, ...) yields one node whose Up, Down, Left and
    /// Right are all null.
    /// </summary>
    [Fact]
    public void BuildGrid_OneByOne_HasAllFourNeighborsNull()
    {
        var map = FocusMapBuilder.BuildGrid(1, 1, (r, c) => $"only{r}{c}");

        Assert.Single(map.Nodes);

        var only = map["only00"];
        Assert.Null(only.Up);
        Assert.Null(only.Down);
        Assert.Null(only.Left);
        Assert.Null(only.Right);
    }

    /// <summary>
    /// FR-XBOXUI-002, TR-XBOXUI-002 (IMPL-XBOXUWP-022), TEST-XBOXUI-002a
    /// invalid-dimension guard.
    /// Use case: a non-positive row or column count is a programming error and must
    /// fail fast rather than yielding a degenerate or empty focus map.
    /// Acceptance: BuildGrid with rows &lt;= 0 or columns &lt;= 0 throws
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    [InlineData(-1, 2)]
    [InlineData(2, -1)]
    [InlineData(0, 0)]
    public void BuildGrid_InvalidDimensions_Throws(int rows, int columns)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FocusMapBuilder.BuildGrid(rows, columns, (r, c) => $"n{r}{c}"));
    }

    /// <summary>
    /// FR-XBOXUI-002, TR-XBOXUI-002 (IMPL-XBOXUWP-022), TEST-XBOXUI-002a
    /// null-argument guard.
    /// Use case: the id factory is mandatory (every node needs a stable id to bind
    /// XYFocus targets against); a null factory must fail fast.
    /// Acceptance: BuildGrid with a null idAt throws
    /// <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void BuildGrid_NullIdFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => FocusMapBuilder.BuildGrid(2, 2, null!));
    }

    /// <summary>
    /// FR-XBOXUI-002, TR-XBOXUI-002 (IMPL-XBOXUWP-022), TEST-XBOXUI-002a purity /
    /// determinism guard.
    /// Use case: the focus graph is a pure projection of its inputs, so the same
    /// dimensions and id factory must always produce the same map (no hidden state
    /// carried between calls).
    /// Acceptance: two BuildGrid calls with identical arguments produce maps with
    /// the same node set and, for every id, an equal <see cref="FocusNode"/>.
    /// </summary>
    [Fact]
    public void BuildGrid_SameArguments_ProduceEqualMaps()
    {
        static string IdAt(int r, int c) => $"n{r}_{c}";

        var first = FocusMapBuilder.BuildGrid(4, 3, IdAt);
        var second = FocusMapBuilder.BuildGrid(4, 3, IdAt);

        Assert.Equal(
            first.Nodes.Keys.OrderBy(k => k, StringComparer.Ordinal),
            second.Nodes.Keys.OrderBy(k => k, StringComparer.Ordinal));

        foreach (var id in first.Nodes.Keys)
        {
            // FocusNode is a record struct: this is value equality across all fields.
            Assert.Equal(first[id], second[id]);
        }
    }

    /// <summary>
    /// FR-XBOXUI-002, TR-XBOXUI-002 (IMPL-XBOXUWP-022), TEST-XBOXUI-002a no
    /// static-mutable-state guard.
    /// Use case: the builder must be genuinely pure so concurrent or repeated grid
    /// builds cannot interfere; a mutable static field would be a hidden shared
    /// channel.
    /// Acceptance: <see cref="FocusMapBuilder"/> declares no static field that is
    /// neither <c>const</c> (literal) nor <c>readonly</c> (init-only).
    /// </summary>
    [Fact]
    public void FocusMapBuilder_HasNoMutableStaticState()
    {
        var mutableStatics = typeof(FocusMapBuilder)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.Name)
            .ToArray();

        Assert.Empty(mutableStatics);
    }

    /// <summary>
    /// FR-XBOXUI-002, TR-XBOXUI-002 (IMPL-XBOXUWP-022), TEST-XBOXUI-002a
    /// map-access guard.
    /// Use case: consumers look up a node by id when wiring a control; a missing id
    /// must be distinguishable via TryGet rather than throwing.
    /// Acceptance: TryGet returns true and the node for a present id, false for an
    /// absent id, and the indexer throws for an absent id.
    /// </summary>
    [Fact]
    public void FocusMap_LookupByIdViaTryGetAndIndexer()
    {
        var map = FocusMapBuilder.BuildGrid(2, 2, (r, c) => $"n{r}{c}");

        Assert.True(map.TryGet("n11", out var present));
        Assert.Equal("n11", present.Id);

        Assert.False(map.TryGet("missing", out _));
        Assert.Throws<KeyNotFoundException>(() => { _ = map["missing"]; });
    }
}
