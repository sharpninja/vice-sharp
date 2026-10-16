using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Connection;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-02). Use case: the device-pairing exchange yields a token that is persisted
/// so the device stays paired.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMPairingTests
{
    /// <summary>AC-CONN-02: pairing exchanges a code for a token and persists the connection.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-02")]
    public async Task Exchange_ReturnsAndPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        var exchange = new FakePairingExchange("rmm_paired");
        var store = new RecordingConnectionStore();
        var coordinator = new RomMPairingCoordinator(exchange, store);

        RomMConnection connection = await coordinator.PairAsync("https://romm.local/", "7Q4-2FX", ct);

        connection.Token.Should().Be("rmm_paired");
        connection.AuthMode.Should().Be(RomMAuthMode.DevicePair);
        connection.BaseUrl.Should().Be("https://romm.local/");
        store.Saved.Should().Be(connection);
    }

    private sealed class FakePairingExchange : IRomMPairingExchange
    {
        private readonly string _token;

        public FakePairingExchange(string token) => _token = token;

        public Task<string> ExchangeAsync(string baseUrl, string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(_token);
    }

    private sealed class RecordingConnectionStore : IRomMConnectionStore
    {
        public RomMConnection? Saved { get; private set; }

        public Task<RomMConnection?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Saved);

        public Task SaveAsync(RomMConnection connection, CancellationToken cancellationToken = default)
        {
            Saved = connection;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Saved = null;
            return Task.CompletedTask;
        }
    }
}
