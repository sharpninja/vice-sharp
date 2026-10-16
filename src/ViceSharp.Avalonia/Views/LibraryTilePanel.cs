using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// Fixed-size library tiles, realized only for the effective viewport. Uses Avalonia's
/// container generator so ListBox selection and keyboard navigation retain their normal semantics.
/// FR-ROMM-COVER-001 AC-COVER-03: removing off-screen containers cancels their cover requests.
/// </summary>
internal sealed class LibraryTilePanel : VirtualizingPanel
{
    private const double TileWidth = 136;
    private const double TileHeight = 174;
    private readonly Dictionary<int, Control> _realized = [];
    private Rect _viewport;
    private int _columns = 1;

    public LibraryTilePanel()
    {
        EffectiveViewportChanged += (_, e) =>
        {
            if (_viewport == e.EffectiveViewport) return;
            _viewport = e.EffectiveViewport;
            InvalidateMeasure();
        };
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsFinite(availableSize.Width) ? Math.Max(0, availableSize.Width) : TileWidth;
        _columns = Math.Max(1, (int)(width / TileWidth));
        int rows = (Items.Count + _columns - 1) / _columns;
        double top = Math.Max(0, _viewport.Top);
        double bottom = Math.Max(top, _viewport.Bottom);
        // Before the first viewport notification, realize one row to establish the extent.
        if (bottom <= top) bottom = top + TileHeight;
        int first = Math.Min(Items.Count, (int)(top / TileHeight) * _columns);
        int end = Math.Min(Items.Count, (int)Math.Ceiling(bottom / TileHeight) * _columns);
        foreach (int index in _realized.Keys.Where(i => i < first || i >= end).ToArray()) Remove(index);
        for (int index = first; index < end; index++)
            Realize(index).Measure(new Size(TileWidth, TileHeight));
        return new Size(width, rows * TileHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var (index, container) in _realized) container.Arrange(TileRect(index));
        return finalSize;
    }

    private Rect TileRect(int index) => new((index % _columns) * TileWidth, (index / _columns) * TileHeight, TileWidth, TileHeight);

    private Control Realize(int index)
    {
        if (_realized.TryGetValue(index, out var existing)) return existing;
        object? item = Items[index];
        var generator = ItemContainerGenerator!;
        Control container = generator.NeedsContainer(item, index, out var key)
            ? generator.CreateContainer(item, index, key) : (Control)item!;
        generator.PrepareItemContainer(container, item, index);
        _realized.Add(index, container);
        AddInternalChild(container);
        generator.ItemContainerPrepared(container, item, index);
        return container;
    }

    private void Remove(int index)
    {
        Control container = _realized[index];
        // Keep keyboard input in the list when scrolling recycles its focused tile.
        // The selection remains authoritative even while its container is unrealized.
        if (container.IsKeyboardFocusWithin) ItemsControl?.Focus();
        _realized.Remove(index);
        RemoveInternalChild(container);
        ItemContainerGenerator?.ClearItemContainer(container);
    }

    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        // Appending a page must not detach existing visible tiles and restart their image loads.
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewStartingIndex < _realized.Keys.DefaultIfEmpty(-1).Max() + 1)
            foreach (int index in _realized.Keys.ToArray()) Remove(index);
        InvalidateMeasure();
        base.OnItemsChanged(items, e);
    }

    protected override Control? ScrollIntoView(int index)
    {
        if (index < 0 || index >= Items.Count) return null;
        var container = Realize(index);
        container.Measure(new Size(TileWidth, TileHeight));
        container.Arrange(TileRect(index));
        this.BringIntoView(TileRect(index));
        return container;
    }

    protected override Control? ContainerFromIndex(int index) => _realized.GetValueOrDefault(index);
    protected override int IndexFromContainer(Control container) => _realized.FirstOrDefault(x => ReferenceEquals(x.Value, container), new(-1, null!)).Key;
    protected override IEnumerable<Control> GetRealizedContainers() => _realized.OrderBy(x => x.Key).Select(x => x.Value);

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var container = (from as Visual)?.GetSelfAndVisualAncestors().OfType<Control>().FirstOrDefault(c => IndexFromContainer(c) >= 0);
        int index = container is null ? (ItemsControl as SelectingItemsControl)?.SelectedIndex ?? -1 : IndexFromContainer(container);
        int next = direction switch
        {
            NavigationDirection.First => 0,
            NavigationDirection.Last => Items.Count - 1,
            NavigationDirection.Left or NavigationDirection.Previous => index - 1,
            NavigationDirection.Right or NavigationDirection.Next => index + 1,
            NavigationDirection.Up => index - _columns,
            NavigationDirection.Down => index + _columns,
            _ => index,
        };
        if (index < 0 && direction is not NavigationDirection.Last) next = 0;
        if (wrap && Items.Count > 0) next = (next % Items.Count + Items.Count) % Items.Count;
        return ScrollIntoView(next);
    }
}
