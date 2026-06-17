using Avalonia;
using System;

namespace ViceSharp.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Enable live SID audio for the in-process emulator host by default.
        // Respect an explicit override (VICESHARP_AUDIO=0 disables it); test and
        // headless hosts leave it unset and run silently.
        if (Environment.GetEnvironmentVariable("VICESHARP_AUDIO") is null)
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", "1");

        // BUG-THROTTLE-001 (temporary): log the emulation worker's per-second
        // wall-time breakdown to %TEMP%/vicesharp-pump.log to locate the GUI
        // under-run. Respect an explicit override.
        if (Environment.GetEnvironmentVariable("VICESHARP_PUMP_DIAG") is null)
            Environment.SetEnvironmentVariable("VICESHARP_PUMP_DIAG", "1");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
