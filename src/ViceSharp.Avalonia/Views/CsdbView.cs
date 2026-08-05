using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// PLAN-ROMM-001 (FR-CSDB-001, AC-AUI parity). The desktop CSDb discovery tab: search scene releases
/// (demo/crack/SID), multi-select, and ingest+scan into RomM. Programmatic UI, driven by the shared
/// <see cref="RomMLibraryViewModel"/> (its <see cref="RomMLibraryViewModel.Csdb"/> populates on Connect
/// via the bridge). Runtime ingest against a live bridge is the [V] E2E step.
/// </summary>
public sealed class CsdbView : UserControl
{
    private readonly RomMLibraryViewModel _host;
    private readonly ListBox _results;
    private readonly TextBox _query;
    private readonly CheckBox _demo;
    private readonly CheckBox _crack;
    private readonly CheckBox _sid;
    private readonly TextBlock _status;

    /// <summary>Creates the view.</summary>
    /// <param name="host">The shared RomM library host view-model.</param>
    public CsdbView(RomMLibraryViewModel host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        DataContext = host;

        _query = new TextBox { PlaceholderText = "Search scene releases...", MinWidth = 200 };
        _demo = new CheckBox { Content = "Demo", IsChecked = true };
        _crack = new CheckBox { Content = "Crack", IsChecked = true };
        _sid = new CheckBox { Content = "SID", IsChecked = true };

        var searchButton = new Button { Content = "Search" };
        searchButton.Click += async (_, _) => await SearchAsync();

        var searchRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _query, _demo, _crack, _sid, searchButton },
        };

        _results = new ListBox
        {
            SelectionMode = SelectionMode.Multiple,
            ItemTemplate = new FuncDataTemplate<CsdbHit>((hit, _) => new TextBlock
            {
                Text = hit is null ? string.Empty : $"{hit.Title}  ·  {hit.Kind}  ·  {hit.Source}",
            }),
        };

        var ingest = new Button { Content = "Ingest + scan" };
        ingest.Click += async (_, _) => await IngestAsync();

        _status = new TextBlock
        {
            Text = "Connect on the Library tab to discover CSDb.",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap,
        };

        var top = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                searchRow,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { ingest } },
                _status,
            },
        };
        DockPanel.SetDock(top, Dock.Top);

        var root = new DockPanel { Margin = new Thickness(10) };
        root.Children.Add(top);
        root.Children.Add(_results);
        Content = root;

        _host.PropertyChanged += OnHostChanged;
        Bind();
    }

    private void OnHostChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RomMLibraryViewModel.Csdb))
        {
            Bind();
        }
    }

    private void Bind()
    {
        _results.ItemsSource = _host.Csdb?.Results;
        _status.Text = _host.Csdb is null
            ? "Connect on the Library tab to discover CSDb."
            : "Search CSDb, select releases, then Ingest + scan.";
    }

    private async Task SearchAsync()
    {
        if (_host.Csdb is null)
        {
            return;
        }

        _host.Csdb.Query = _query.Text ?? string.Empty;
        _host.Csdb.Kinds.Clear();
        if (_demo.IsChecked == true)
        {
            _host.Csdb.Kinds.Add(CsdbKind.Demo);
        }

        if (_crack.IsChecked == true)
        {
            _host.Csdb.Kinds.Add(CsdbKind.Crack);
        }

        if (_sid.IsChecked == true)
        {
            _host.Csdb.Kinds.Add(CsdbKind.Sid);
        }

        await _host.Csdb.SearchAsync();
    }

    private async Task IngestAsync()
    {
        if (_host.Csdb is null)
        {
            return;
        }

        var selections = _results.SelectedItems?
            .OfType<CsdbHit>()
            .Select(h => new CsdbSelection(h.CsdbId, h.Kind))
            .ToList() ?? new List<CsdbSelection>();

        if (selections.Count == 0)
        {
            _status.Text = "Select one or more releases first.";
            return;
        }

        await _host.Csdb.IngestAsync(selections, force: false);
        _status.Text = _host.Csdb.LastResult is { } result
            ? $"Ingested {result.Ingested}, skipped {result.Skipped}, failed {result.Failed}; scanned={result.Scanned}."
            : "Ingest requested.";
    }
}
