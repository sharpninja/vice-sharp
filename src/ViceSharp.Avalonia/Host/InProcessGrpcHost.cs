using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ViceSharp.Abstractions;
using ViceSharp.Host.Diagnostics;
using ViceSharp.Host.Runtime;
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

    public IDisposable? SubscribeWarpMode(string sessionId, Action<WarpModeEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        var registry = _app.Services.GetRequiredService<EmulatorRuntimeRegistry>();
        var subscription = new LocalWarpModeSubscription(registry, sessionId, handler);
        if (subscription.TryAttach())
            return subscription;

        subscription.Dispose();
        return null;
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

    private sealed class LocalWarpModeSubscription : IDisposable
    {
        private readonly EmulatorRuntimeRegistry _registry;
        private readonly string _sessionId;
        private readonly Action<WarpModeEvent> _handler;
        private readonly object _syncRoot = new();
        private IPubSub? _pubSub;
        private SubscriptionHandle _handle = SubscriptionHandle.Invalid;
        private bool _disposed;

        public LocalWarpModeSubscription(
            EmulatorRuntimeRegistry registry,
            string sessionId,
            Action<WarpModeEvent> handler)
        {
            _registry = registry;
            _sessionId = sessionId;
            _handler = handler;
            _registry.SessionChanged += OnSessionChanged;
        }

        public bool TryAttach()
        {
            if (!_registry.TryGet(_sessionId, out var session))
                return false;

            Attach(session);
            return _pubSub is not null;
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _registry.SessionChanged -= OnSessionChanged;
                Detach();
            }
        }

        private void OnSessionChanged(object? sender, EmulatorRuntimeSession session)
        {
            if (!string.Equals(session.SessionId, _sessionId, StringComparison.OrdinalIgnoreCase))
                return;

            Attach(session);
        }

        private void Attach(EmulatorRuntimeSession session)
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                Detach();
                if (session.Machine.PubSub is not { } pubSub)
                    return;

                _pubSub = pubSub;
                _handle = pubSub.Subscribe(WarpModeEvent.Topic, _handler);
            }
        }

        private void Detach()
        {
            if (_pubSub is not null && _handle.IsValid)
                _pubSub.Unsubscribe(_handle);

            _pubSub = null;
            _handle = SubscriptionHandle.Invalid;
        }
    }
}
