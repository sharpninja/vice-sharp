using System;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ViceSharp.Avalonia;

public partial class App : Application
{
    // UI-REMOTECTRL-001: keep the lifetime registration + provider alive for the
    // process lifetime so the embedded remote-control server is started on
    // Avalonia startup and stopped on exit.
    private IServiceProvider? _remoteControlServices;
    private IDisposable? _remoteControlLifetime;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            TryStartRemoteControl(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// UI-REMOTECTRL-001: optionally start the embeddable Avalonia.RemoteControl
    /// gRPC server for live visual-tree inspection. Disabled by default; it only
    /// runs when <c>VICESHARP_REMOTECONTROL_ENABLE</c> is truthy, and then only
    /// with a bearer token so the loopback transport is never anonymous.
    /// </summary>
    private void TryStartRemoteControl(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (!IsTruthy(Environment.GetEnvironmentVariable("VICESHARP_REMOTECONTROL_ENABLE")))
            return;

        var token = Environment.GetEnvironmentVariable("VICESHARP_REMOTECONTROL_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            // Fail closed: an enabled-but-tokenless transport would be rejected by
            // startup validation anyway, so do not even register the host.
            return;
        }

        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl(options =>
        {
            options.IsEnabled = true;
            options.AuthenticationToken = token;
            options.AuthenticatedClientIdentity = "vicesharp-inspector";

            if (TryParseHost(Environment.GetEnvironmentVariable("VICESHARP_REMOTECONTROL_HOST"), out var host))
                options.Host = host;

            if (int.TryParse(Environment.GetEnvironmentVariable("VICESHARP_REMOTECONTROL_PORT"), out var port) && port > 0)
                options.Port = port;

            // Interaction + live frames stay deny-by-default; opt in explicitly.
            options.AllowRemoteActions = IsTruthy(Environment.GetEnvironmentVariable("VICESHARP_REMOTECONTROL_ALLOW_ACTIONS"));
            options.AllowRemoteFrames = IsTruthy(Environment.GetEnvironmentVariable("VICESHARP_REMOTECONTROL_ALLOW_FRAMES"));
            options.AllowRemoteInput = options.AllowRemoteActions
                && IsTruthy(Environment.GetEnvironmentVariable("VICESHARP_REMOTECONTROL_ALLOW_INPUT"));
        });

        // Override the default no-op root provider so snapshots see the live window.
        services.AddSingleton<IRemoteControlRootProvider>(new MainWindowRootProvider(desktop));

        _remoteControlServices = services.BuildServiceProvider();
        _remoteControlLifetime = desktop.AttachAvaloniaRemoteControl(_remoteControlServices);
    }

    private static bool IsTruthy(string? value)
        => value is not null
            && (value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase));

    private static bool TryParseHost(string? value, out IPAddress host)
    {
        if (!string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value, out var parsed))
        {
            host = parsed;
            return true;
        }

        host = IPAddress.Loopback;
        return false;
    }

    /// <summary>
    /// Returns the desktop main window as the remote-control inspection root.
    /// </summary>
    private sealed class MainWindowRootProvider(IClassicDesktopStyleApplicationLifetime desktop)
        : IRemoteControlRootProvider
    {
        public Control? GetRootControl() => desktop.MainWindow;
    }
}
