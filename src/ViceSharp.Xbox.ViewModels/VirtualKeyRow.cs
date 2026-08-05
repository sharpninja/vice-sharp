namespace ViceSharp.Xbox.ViewModels;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FEAT-XAOTBIND-001: one physical row of the virtual C64 keyboard, exposed as a named
/// type so UWP compiled bindings (<c>{x:Bind Keys}</c>) can address the row without
/// reflection <c>{Binding}</c> (which dies under CsWinRT AOT binding mode).
/// </summary>
/// <remarks>
/// Implements <see cref="IReadOnlyList{T}"/> so existing layout tests that index
/// <c>Rows[r][c]</c> or <c>SelectMany(row =&gt; row)</c> keep working without a rewrite.
/// </remarks>
public sealed class VirtualKeyRow : IReadOnlyList<VirtualKeyEntry>
{
    private readonly IReadOnlyList<VirtualKeyEntry> _keys;

    /// <summary>Creates a row from the already-ordered key list.</summary>
    /// <param name="keys">The keys left-to-right in this row. Must not be null.</param>
    public VirtualKeyRow(IReadOnlyList<VirtualKeyEntry> keys)
    {
        _keys = keys ?? throw new System.ArgumentNullException(nameof(keys));
    }

    /// <summary>
    /// The keys in this row (left-to-right). The property name is the compiled-binding
    /// surface for the nested row ItemsControl on the virtual keyboard overlay.
    /// </summary>
    public IReadOnlyList<VirtualKeyEntry> Keys => _keys;

    /// <inheritdoc />
    public int Count => _keys.Count;

    /// <inheritdoc />
    public VirtualKeyEntry this[int index] => _keys[index];

    /// <inheritdoc />
    public IEnumerator<VirtualKeyEntry> GetEnumerator() => _keys.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
