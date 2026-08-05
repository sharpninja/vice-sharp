using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// PLAN-ROMM-001 (FR-ROMM-AVUI-001, AC-AUI-04). The desktop list-management tab: the user's RomM
/// collections with create/delete/refresh. Programmatic UI, driven by the shared
/// <see cref="RomMLibraryViewModel"/> (its <see cref="RomMLibraryViewModel.Collections"/> populates on
/// Connect). Runtime add/remove against a live server is the [V] E2E step.
/// </summary>
public sealed class ListsView : UserControl
{
    private readonly RomMLibraryViewModel _host;
    private readonly ListBox _list;
    private readonly TextBox _newName;
    private readonly TextBlock _status;

    /// <summary>Creates the view.</summary>
    /// <param name="host">The shared RomM library host view-model.</param>
    public ListsView(RomMLibraryViewModel host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        DataContext = host;

        _list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<LibraryCollection>((collection, _) => new TextBlock
            {
                Text = collection is null
                    ? string.Empty
                    : $"{collection.Name}  ({collection.Count}){(collection.ReadOnly ? "  ·  read-only" : string.Empty)}",
            }),
        };
        _list.SelectionChanged += (_, _) =>
        {
            if (_host.Collections is not null)
            {
                _host.Collections.SelectedCollection = _list.SelectedItem as LibraryCollection;
            }
        };

        _newName = new TextBox { PlaceholderText = "New list name", MinWidth = 160 };

        var create = new Button { Content = "Create" };
        create.Click += async (_, _) =>
        {
            if (_host.Collections is not null && !string.IsNullOrWhiteSpace(_newName.Text))
            {
                await _host.Collections.CreateAsync(_newName.Text!.Trim());
                _newName.Text = string.Empty;
            }
        };

        var refresh = new Button { Content = "Refresh" };
        refresh.Click += async (_, _) =>
        {
            if (_host.Collections is not null)
            {
                await _host.Collections.RefreshAsync();
            }
        };

        var delete = new Button { Content = "Delete" };
        delete.Click += async (_, _) =>
        {
            if (_host.Collections?.SelectedCollection is { ReadOnly: false } selected)
            {
                await _host.Collections.DeleteAsync(selected.Id);
            }
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _newName, create, refresh, delete },
        };

        _status = new TextBlock
        {
            Text = "Connect on the Library tab to manage lists.",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap,
        };

        var top = new StackPanel { Spacing = 8, Children = { actions, _status } };
        DockPanel.SetDock(top, Dock.Top);

        var root = new DockPanel { Margin = new Thickness(10) };
        root.Children.Add(top);
        root.Children.Add(_list);
        Content = root;

        _host.PropertyChanged += OnHostChanged;
        Bind();
    }

    private void OnHostChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RomMLibraryViewModel.Collections))
        {
            Bind();
        }
    }

    private void Bind()
    {
        _list.ItemsSource = _host.Collections?.Collections;
        _status.Text = _host.Collections is null
            ? "Connect on the Library tab to manage lists."
            : $"{_host.Collections.Collections.Count} lists.";
    }
}
