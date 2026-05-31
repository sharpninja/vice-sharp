// PLATFORM-CROSS-001 Phase 1: UWP Xbox host shell entry point.
//
// This shell intentionally contains NO UI of its own; it delegates to the
// shared ViceSharp.Avalonia.App so the Xbox build inherits the same view
// model graph, in-process gRPC host wiring (ViceSharp.Host), and protocol
// surface that the desktop build uses. Duplicating UI here would violate
// FR-Host-UI-Boundary.

using System;
using Avalonia;
using AvaloniaApp = ViceSharp.Avalonia.App;

namespace ViceSharp.Host.Xbox;

/// <summary>
/// Entry point for the UWP Xbox host shell. Boots Avalonia with the shared
/// ViceSharp.Avalonia.App and starts the classic desktop lifetime, which is
/// the lifetime model supported by Xbox developer mode running .NET 10 apps.
/// </summary>
public static class Program
{
    /// <summary>
    /// Program entry point.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to Avalonia.</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Avalonia configuration. Also used by the Avalonia visual designer.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
