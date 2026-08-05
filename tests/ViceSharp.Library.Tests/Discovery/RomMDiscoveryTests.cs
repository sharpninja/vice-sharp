using System.Net.Http;
using FluentAssertions;
using ViceSharp.Library.Tests.Adapter;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Discovery;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-07, operator request 2026-07-16). Use case: the head cannot assume a fixed
/// server URL, so it scans the local subnet for RomM API services by probing each host's unauthenticated
/// <c>/api/heartbeat</c> and keeps only the ones that answer with a RomM heartbeat (SYSTEM.VERSION).
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMDiscoveryTests
{
    private const string HeartbeatJson =
        """{"SYSTEM":{"VERSION":"5.0.0","SHOW_SETUP_WIZARD":false},"FRONTEND":{}}""";

    /// <summary>AC-CONN-07: a host answering /api/heartbeat with SYSTEM.VERSION is discovered; others are not.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-07")]
    public async Task Scan_FindsRomMHostsByHeartbeat()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            req.RequestUri!.AbsolutePath == "/api/heartbeat" && req.RequestUri.Host == "10.0.0.5"
                ? FakeRomMHandler.Json(HeartbeatJson)
                : FakeRomMHandler.NotFound();

        var handler = new FakeRomMHandler(Router);
        var discovery = new RomMSubnetDiscovery(handler, new[] { "10.0.0.5", "10.0.0.6" });

        IReadOnlyList<DiscoveredRomM> found =
            await discovery.ScanAsync(port: 8080, perHostTimeout: TimeSpan.FromSeconds(1), progress: null, cancellationToken: ct);

        found.Should().ContainSingle();
        found[0].BaseUrl.Should().Be(new Uri("http://10.0.0.5:8080/"));
        found[0].Version.Should().Be("5.0.0");
    }

    /// <summary>AC-CONN-07: a host that answers 200 but is not RomM (no SYSTEM.VERSION) is ignored.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-07")]
    public async Task Scan_IgnoresNonRomMResponders()
    {
        var ct = TestContext.Current.CancellationToken;

        static HttpResponseMessage Router(HttpRequestMessage req) =>
            FakeRomMHandler.Json("""{"service":"something-else"}""");

        var handler = new FakeRomMHandler(Router);
        var discovery = new RomMSubnetDiscovery(handler, new[] { "10.0.0.7", "10.0.0.8" });

        IReadOnlyList<DiscoveredRomM> found =
            await discovery.ScanAsync(port: 8080, perHostTimeout: TimeSpan.FromSeconds(1), progress: null, cancellationToken: ct);

        found.Should().BeEmpty();
    }

    /// <summary>AC-CONN-07: progress reports one tick per host scanned.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-07")]
    public async Task Scan_ReportsProgressPerHost()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeRomMHandler(_ => FakeRomMHandler.NotFound());
        var discovery = new RomMSubnetDiscovery(handler, new[] { "10.0.0.1", "10.0.0.2", "10.0.0.3" });

        var progress = new CountingProgress();
        await discovery.ScanAsync(port: 8080, perHostTimeout: TimeSpan.FromSeconds(1), progress: progress, cancellationToken: ct);

        progress.Last.Should().Be(3);
    }

    private sealed class CountingProgress : IProgress<int>
    {
        public int Last { get; private set; }

        public void Report(int value) => Last = value;
    }
}
