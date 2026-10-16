namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;

/// <summary>
/// An immutable directional XY-focus graph: a set of <see cref="FocusNode"/> keyed
/// by their <see cref="FocusNode.Id"/>, describing which control receives focus in
/// each direction for the 10-foot (couch) UI.
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S22 (IMPL-XBOXUWP-022), FR-XBOXUI-002 / TR-XBOXUI-002. The UWP XAML
/// shell looks a control up by id and binds its <c>XYFocusUp/Down/Left/Right</c> to
/// the neighbor ids on the matching node. The map is a snapshot: it copies its input
/// into a private dictionary at construction and exposes it read-only, so it carries
/// no engine, host, or XAML reference (TR-MVVM-001).
/// </para>
/// </remarks>
public sealed class FocusMap
{
    private readonly Dictionary<string, FocusNode> _nodes;

    /// <summary>
    /// Initializes a new <see cref="FocusMap"/> from a sequence of nodes, keyed by
    /// each node's <see cref="FocusNode.Id"/>.
    /// </summary>
    /// <param name="nodes">The nodes to include; their ids must be unique.</param>
    /// <exception cref="ArgumentNullException"><paramref name="nodes"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Two nodes share the same id.</exception>
    public FocusMap(IEnumerable<FocusNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        _nodes = new Dictionary<string, FocusNode>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            _nodes.Add(node.Id, node);
        }
    }

    /// <summary>
    /// Initializes a new <see cref="FocusMap"/> from an id-to-node dictionary. The
    /// contents are copied, so later mutation of <paramref name="nodes"/> does not
    /// affect this map.
    /// </summary>
    /// <param name="nodes">The id-to-node map to copy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="nodes"/> is <c>null</c>.</exception>
    public FocusMap(IReadOnlyDictionary<string, FocusNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        _nodes = new Dictionary<string, FocusNode>(nodes.Count, StringComparer.Ordinal);
        foreach (var pair in nodes)
        {
            _nodes.Add(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// The nodes of the graph, keyed by <see cref="FocusNode.Id"/>.
    /// </summary>
    public IReadOnlyDictionary<string, FocusNode> Nodes => _nodes;

    /// <summary>
    /// Gets the node with the given id.
    /// </summary>
    /// <param name="id">The node id to look up.</param>
    /// <returns>The matching <see cref="FocusNode"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
    /// <exception cref="KeyNotFoundException">No node has the given id.</exception>
    public FocusNode this[string id]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(id);
            return _nodes[id];
        }
    }

    /// <summary>
    /// Attempts to get the node with the given id.
    /// </summary>
    /// <param name="id">The node id to look up.</param>
    /// <param name="node">
    /// When this method returns <c>true</c>, the matching node; otherwise the default
    /// value.
    /// </param>
    /// <returns><c>true</c> if a node with the id exists; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
    public bool TryGet(string id, out FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _nodes.TryGetValue(id, out node);
    }
}
