using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ViceSharp.Abstractions;
using ViceSharp.Architectures;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;

namespace ViceSharp.Avalonia;

public partial class MainWindow : Window
{
    private readonly IMachine _machine;
    private readonly VideoSurface _video;

    public MainWindow()
    {
        InitializeComponent();

        // Build C64 machine using ArchitectureBuilder
        var builder = new ArchitectureBuilder();
        var descriptor = new C64Descriptor();
        _machine = builder.Build(descriptor);

        // Create video surface connected to machine
        _video = new VideoSurface(_machine);
        Content = _video;

        // 50 FPS frame rendering
        var renderTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1.0 / 50.0),
            DispatcherPriority.Render,
            (s, e) => _machine.RunFrame());

        renderTimer.Start();
    }
}
