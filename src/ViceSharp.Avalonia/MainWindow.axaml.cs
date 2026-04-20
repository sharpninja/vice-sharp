using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ViceSharp.Abstractions;
using ViceSharp.Architectures;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Core;

namespace ViceSharp.Avalonia;

public partial class MainWindow : Window
{
    private readonly IMachine _machine;
    private readonly VideoSurface _video;
    private readonly DispatcherTimer _renderTimer;

    public MainWindow()
    {
        InitializeComponent();

        // Build C64 machine using ArchitectureBuilder
        var builder = new ArchitectureBuilder();
        var descriptor = new EmptyMachineDescriptor();
        _machine = builder.Build(descriptor);

        // Create video surface connected to machine
        _video = new VideoSurface(_machine);
        Content = _video;

        // 50 FPS frame rendering
        _renderTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1.0 / 50.0),
            DispatcherPriority.Render,
            (s, e) =>
            {
                _machine.RunFrame();
                _video.InvalidateVisual();
            });

        _renderTimer.Start();
    }
}
