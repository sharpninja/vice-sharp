using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ViceSharp.Abstractions;
using ViceSharp.Architectures;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.RomFetch;

namespace ViceSharp.Avalonia;

public partial class MainWindow : Window
{
    private readonly IMachine _machine;
    private readonly VideoSurface _video;

    public MainWindow()
    {
        InitializeComponent();

        // Build C64 machine with ROM provider pointing to local roms folder
        var romProvider = new RomProvider(Path.Combine(AppContext.BaseDirectory, "roms"));
        var builder = new ArchitectureBuilder(romProvider);
        var descriptor = new C64Descriptor();
        _machine = builder.Build(descriptor);

        // Create video surface connected to machine
        _video = new VideoSurface(_machine);
        Content = _video;
        
        // Force initial frame render so we see something immediately
        _machine.RunFrame();

        // 50 FPS frame rendering
        var renderTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1.0 / 50.0),
            DispatcherPriority.Render,
            (s, e) => _machine.RunFrame());

        renderTimer.Start();
    }
}
