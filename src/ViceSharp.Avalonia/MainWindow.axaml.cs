using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ViceSharp.Architectures;

namespace ViceSharp.Avalonia;

public partial class MainWindow : Window
{
    private readonly C64Machine _machine;
    private readonly VideoSurface _video;
    private readonly DispatcherTimer _renderTimer;

    public MainWindow()
    {
        InitializeComponent();
        _machine = new C64Machine();
        _machine.Reset();

        _video = new VideoSurface(_machine);
        Content = _video;

        _renderTimer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / 50.0), DispatcherPriority.Render, (s, e) =>
        {
            _machine.RunFrame();
            _video.InvalidateVisual();
        });

        _renderTimer.Start();
    }
}
