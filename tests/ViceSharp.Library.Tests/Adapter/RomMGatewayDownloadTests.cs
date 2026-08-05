using System.Net.Http;
using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Adapter;

/// <summary>
/// FR-ROMM-LAUNCH-001 (AC-LAUNCH-01). Use case: the adapter streams a ROM to the cache under
/// <c>{romId}/{fileName}</c>, reports progress, and reuses an identical cached file without a second
/// download.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMGatewayDownloadTests
{
    private sealed class SyncProgress : IProgress<double>
    {
        public List<double> Values { get; } = new();

        public void Report(double value) => Values.Add(value);
    }

    /// <summary>AC-LAUNCH-01: streams to the cache, reports progress, and reuses on a size match.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-01")]
    public async Task Streams_Reuses_Progress()
    {
        byte[] payload = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();

        HttpResponseMessage Router(HttpRequestMessage req) =>
            req.RequestUri!.AbsolutePath.StartsWith("/api/roms/101/content/", StringComparison.Ordinal)
                ? FakeRomMHandler.Bytes(payload)
                : RomMFixtures.DefaultRouter(req);

        var handler = new FakeRomMHandler(Router);
        await using var client = RomMFixtures.Client(handler);
        var gateway = new RomMLibraryGateway(client);

        var ct = TestContext.Current.CancellationToken;
        string cacheDir = Path.Combine(Path.GetTempPath(), "vs-romm-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var progress = new SyncProgress();
            AcquiredGame game = await gateway.DownloadAsync(101, "boulderdash.d64", payload.Length, cacheDir, progress, ct);

            game.LocalPath.Should().Be(Path.Combine(cacheDir, "101", "boulderdash.d64"));
            game.Kind.Should().Be(MediaKind.Disk);
            File.ReadAllBytes(game.LocalPath).Should().Equal(payload);

            progress.Values.Should().NotBeEmpty();
            progress.Values[^1].Should().Be(1.0);
            progress.Values.Should().OnlyContain(v => v >= 0.0 && v <= 1.0);

            handler.CountPathPrefix("/api/roms/101/content/").Should().Be(1);

            // Reuse: a second call with the same expected size finds the cached file and does not re-download.
            AcquiredGame reused = await gateway.DownloadAsync(101, "boulderdash.d64", payload.Length, cacheDir, new SyncProgress(), ct);
            handler.CountPathPrefix("/api/roms/101/content/").Should().Be(1);
            reused.LocalPath.Should().Be(game.LocalPath);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, recursive: true);
            }
        }
    }
}
