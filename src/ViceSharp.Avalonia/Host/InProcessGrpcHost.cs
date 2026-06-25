using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ViceSharp.Host.Diagnostics;
using ViceSharp.Host.Services;

namespace ViceSharp.Avalonia.Host;

public sealed class InProcessGrpcHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly DebugAttachFilePublisher _debugAttachFilePublisher;

    private InProcessGrpcHost(
        WebApplication app,
        Uri endpoint,
        bool reflectionEnabled,
        DebugAttachFilePublisher debugAttachFilePublisher)
    {
        _app = app;
        Endpoint = endpoint;
        ReflectionEnabled = reflectionEnabled;
        _debugAttachFilePublisher = debugAttachFilePublisher;
    }

    public Uri Endpoint { get; }

    public bool ReflectionEnabled { get; }

    public HostDiagnosticsState DiagnosticsState => _app.Services.GetRequiredService<HostDiagnosticsState>();

    public ILocalVideoFrameSource VideoFrameSource => _app.Services.GetRequiredService<ILocalVideoFrameSource>();

    public static async Task<InProcessGrpcHost> StartAsync(
        CancellationToken cancellationToken = default,
        string? debugAttachPath = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ApplicationName = typeof(InProcessGrpcHost).Assembly.FullName
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });
        builder.Services.AddViceSharpGrpcHost();
        var reflectionEnabled = IsReflectionEnabled(builder);

        var app = builder.Build();
        app.MapViceSharpGrpcHost();
        if (reflectionEnabled)
            app.MapGrpcReflectionService();

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        var addresses = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses;
        var address = addresses?.SingleOrDefault()
            ?? throw new InvalidOperationException("The in-process gRPC host did not publish a listening address.");
        var endpoint = new Uri(address);
        var diagnosticsState = app.Services.GetRequiredService<HostDiagnosticsState>();
        diagnosticsState.UpdateEndpoint(endpoint);
        var publisher = new DebugAttachFilePublisher(debugAttachPath ?? DebugAttachFilePublisher.DefaultPath, diagnosticsState);
        await publisher.WriteAsync(cancellationToken).ConfigureAwait(false);

        return new InProcessGrpcHost(app, endpoint, reflectionEnabled, publisher);
    }

    public async ValueTask UpdateCurrentSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _debugAttachFilePublisher.UpdateCurrentSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    public Task<string> ReadDebugAttachInfoAsync(CancellationToken cancellationToken = default)
    {
        return _debugAttachFilePublisher.ReadAsync(cancellationToken);
    }

    private static bool IsReflectionEnabled(WebApplicationBuilder builder)
    {
        var value = Environment.GetEnvironmentVariable("VICESHARP_GRPC_REFLECTION");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(builder.Environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await _debugAttachFilePublisher.DisposeAsync().ConfigureAwait(false);
        await _app.StopAsync(timeout.Token).ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}
