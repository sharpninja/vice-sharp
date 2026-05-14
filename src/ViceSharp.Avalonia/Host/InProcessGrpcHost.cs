using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ViceSharp.Host.Services;

namespace ViceSharp.Avalonia.Host;

public sealed class InProcessGrpcHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private InProcessGrpcHost(WebApplication app, Uri endpoint)
    {
        _app = app;
        Endpoint = endpoint;
    }

    public Uri Endpoint { get; }

    public ILocalVideoFrameSource VideoFrameSource => _app.Services.GetRequiredService<ILocalVideoFrameSource>();

    public static async Task<InProcessGrpcHost> StartAsync(CancellationToken cancellationToken = default)
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

        var app = builder.Build();
        app.MapViceSharpGrpcHost();

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        var addresses = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses;
        var address = addresses?.SingleOrDefault()
            ?? throw new InvalidOperationException("The in-process gRPC host did not publish a listening address.");

        return new InProcessGrpcHost(app, new Uri(address));
    }

    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await _app.StopAsync(timeout.Token).ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}
