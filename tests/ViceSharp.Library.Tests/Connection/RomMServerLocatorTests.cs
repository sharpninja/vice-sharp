using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Connection;

/// <summary>
/// FR-ROMM-CONN-001. Use case: reconnect to the last RomM server without scanning the LAN unless that
/// server is offline.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMServerLocatorTests
{
    /// <summary>When the saved server answers heartbeat, no scan runs and the saved connection is returned.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-05")]
    public async Task Locate_UsesSavedWhenReachable_DoesNotScan()
    {
        var ct = TestContext.Current.CancellationToken;
        var saved = new RomMConnection("http://10.0.0.5:8080/", RomMAuthMode.SubnetShared, "tok");
        var store = new MemoryConnectionStore(saved);
        var probe = new FakeProbe(reachable: true);
        var discovery = new FakeDiscovery();
        var locator = new RomMServerLocator(store, probe, discovery);

        RomMLocateResult result = await locator.LocateAsync(cancellationToken: ct);

        result.BaseUrl.Should().Be(new Uri("http://10.0.0.5:8080/"));
        result.ScannedNetwork.Should().BeFalse();
        result.SavedConnection.Should().Be(saved);
        discovery.ScanCount.Should().Be(0);
        probe.ProbeCount.Should().Be(1);
        result.StatusMessage.Should().Contain("Reconnecting");
    }

    /// <summary>When the saved server is down, the LAN is scanned and the first hit is returned.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-05")]
    public async Task Locate_ScansWhenSavedUnreachable()
    {
        var ct = TestContext.Current.CancellationToken;
        var saved = new RomMConnection("http://10.0.0.5:8080/", RomMAuthMode.ClientToken, "tok");
        var store = new MemoryConnectionStore(saved);
        var probe = new FakeProbe(reachable: false);
        var hit = new DiscoveredRomM(new Uri("http://10.0.0.9:8080/"), "RomM", "5.0.0");
        var discovery = new FakeDiscovery(hit);
        var locator = new RomMServerLocator(store, probe, discovery);

        RomMLocateResult result = await locator.LocateAsync(cancellationToken: ct);

        result.ScannedNetwork.Should().BeTrue();
        discovery.ScanCount.Should().Be(1);
        result.BaseUrl.Should().Be(hit.BaseUrl);
        result.SavedConnection.Should().BeNull();
    }

    /// <summary>With no saved connection, a scan runs immediately.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-05")]
    public async Task Locate_ScansWhenNothingSaved()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new MemoryConnectionStore(null);
        var probe = new FakeProbe(reachable: true);
        var hit = new DiscoveredRomM(new Uri("http://10.0.0.3:8080/"), null, "5.0.0");
        var discovery = new FakeDiscovery(hit);
        var locator = new RomMServerLocator(store, probe, discovery);

        RomMLocateResult result = await locator.LocateAsync(cancellationToken: ct);

        result.ScannedNetwork.Should().BeTrue();
        probe.ProbeCount.Should().Be(0);
        discovery.ScanCount.Should().Be(1);
        result.BaseUrl.Should().Be(hit.BaseUrl);
    }

    /// <summary>Saved offline and empty scan leaves BaseUrl null with a clear status.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-05")]
    public async Task Locate_EmptyScan_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new MemoryConnectionStore(
            new RomMConnection("http://10.0.0.5:8080/", RomMAuthMode.ClientToken, "x"));
        var locator = new RomMServerLocator(store, new FakeProbe(false), new FakeDiscovery());

        RomMLocateResult result = await locator.LocateAsync(cancellationToken: ct);

        result.BaseUrl.Should().BeNull();
        result.ScannedNetwork.Should().BeTrue();
        result.StatusMessage.Should().Contain("offline");
    }

    private sealed class MemoryConnectionStore : IRomMConnectionStore
    {
        private RomMConnection? _connection;

        public MemoryConnectionStore(RomMConnection? connection) => _connection = connection;

        public Task<RomMConnection?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_connection);

        public Task SaveAsync(RomMConnection connection, CancellationToken cancellationToken = default)
        {
            _connection = connection;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _connection = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProbe : IRomMServerProbe
    {
        private readonly bool _reachable;

        public FakeProbe(bool reachable) => _reachable = reachable;

        public int ProbeCount { get; private set; }

        public Task<bool> IsReachableAsync(
            Uri baseUrl,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ProbeCount++;
            return Task.FromResult(_reachable);
        }
    }

    private sealed class FakeDiscovery : IRomMDiscovery
    {
        private readonly IReadOnlyList<DiscoveredRomM> _hits;

        public FakeDiscovery(params DiscoveredRomM[] hits) => _hits = hits;

        public int ScanCount { get; private set; }

        public Task<IReadOnlyList<DiscoveredRomM>> ScanAsync(
            int port = 8080,
            TimeSpan? perHostTimeout = null,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ScanCount++;
            return Task.FromResult(_hits);
        }
    }
}
