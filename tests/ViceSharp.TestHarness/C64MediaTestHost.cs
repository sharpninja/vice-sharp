namespace ViceSharp.TestHarness;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;

/// <summary>
/// FR-DRVTRUE-001 / FR-ROMM-LAUNCH-001: real C64 media/session protocol test host, without a
/// minimal-machine fallback. Uses existing C64 ROMs and a generated idle 1541 ROM: these tests
/// exercise image parsing, rig recreation and accepted commands, not DOS loading or gameplay.
/// </summary>
internal sealed class C64MediaTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private C64MediaTestHost(WebApplication app, Uri endpoint)
    {
        _app = app;
        Endpoint = endpoint;
    }

    public Uri Endpoint { get; }
    public EmulatorRuntimeRegistry Registry => _app.Services.GetRequiredService<EmulatorRuntimeRegistry>();

    public static DefaultEmulatorRuntimeFactory CreateRuntimeFactory()
    {
        var builder = new ArchitectureBuilder(new MediaTestRomProvider(MachineTestFactory.CreateC64RomProvider()));
        return new DefaultEmulatorRuntimeFactory(builder, [new C64Descriptor()], C64MachineProfiles.C64Pal.Id);
    }

    public static async Task<C64MediaTestHost> StartAsync(
        CancellationToken cancellationToken,
        IEmulatorRuntimeFactory? runtimeFactory = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.WebHost.ConfigureKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddViceSharpGrpcHost();
        builder.Services.AddSingleton<IEmulatorRuntimeFactory>(runtimeFactory ?? CreateRuntimeFactory());
        var app = builder.Build();
        app.MapViceSharpGrpcHost();
        try
        {
            await app.StartAsync(cancellationToken);
            string address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            return new C64MediaTestHost(app, new Uri(address));
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _app.StopAsync(timeout.Token);
        await _app.DisposeAsync();
    }

    private sealed class MediaTestRomProvider(IRomProvider c64Roms) : IRomProvider
    {
        private readonly byte[] _driveRom = CreateIdleDriveRom();

        public ReadOnlyMemory<byte> LoadRom(string romName, string architecture) =>
            IsDriveRom(romName, architecture) ? _driveRom : c64Roms.LoadRom(romName, architecture);

        public bool IsAvailable(string romName, string architecture) =>
            IsDriveRom(romName, architecture) || c64Roms.IsAvailable(romName, architecture);

        private static bool IsDriveRom(string romName, string architecture) =>
            architecture == C1541ViceRomNames.ArchitectureKey && romName == C1541ViceRomNames.Dos1541;

        private static byte[] CreateIdleDriveRom()
        {
            var rom = new byte[C1541ViceRomNames.Dos1541RomSize];
            rom[0] = 0x4C; // JMP $C000; keep the drive CPU idle while the real rig advances.
            rom[2] = 0xC0;
            for (int vector = 0x3FFA; vector <= 0x3FFE; vector += 2)
                rom[vector + 1] = 0xC0;
            return rom;
        }
    }
}
