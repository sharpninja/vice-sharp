using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Covers;

/// <summary>
/// FR-ROMM-COVER-001 (AC-COVER-02/03/04). Use case: covers are cached, fetches are gated and cancellable,
/// and a failed fetch yields a placeholder rather than throwing.
/// </summary>
[Trait("Category", "Library")]
public sealed class CoverCacheTests
{
    private static readonly byte[] Placeholder = { 0 };

    private sealed class FakeImageLoader : ILibraryImageLoader
    {
        private readonly Func<CancellationToken, Task<Stream>> _open;
        private int _calls;

        public FakeImageLoader(Func<CancellationToken, Task<Stream>> open) => _open = open;

        public int Calls => Volatile.Read(ref _calls);

        public Task<Stream> OpenCoverAsync(CoverRef cover, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            return _open(cancellationToken);
        }
    }

    /// <summary>AC-COVER-02: a second request for the same cover hits the cache (no second fetch).</summary>
    [Fact]
    [Trait("AC", "AC-COVER-02")]
    public async Task SecondRequest_Cached()
    {
        var ct = TestContext.Current.CancellationToken;
        var loader = new FakeImageLoader(_ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 7, 8, 9 })));
        var cache = new CoverCache(loader, Placeholder);
        var cover = new CoverRef(null, "/p1");

        byte[] first = await cache.GetAsync(cover, ct);
        byte[] second = await cache.GetAsync(cover, ct);

        first.Should().Equal(new byte[] { 7, 8, 9 });
        second.Should().Equal(first);
        loader.Calls.Should().Be(1);
    }

    /// <summary>AC-COVER-03: the gate is bounded and cancellation propagates.</summary>
    [Fact]
    [Trait("AC", "AC-COVER-03")]
    public async Task Concurrency_Gated_Cancellable()
    {
        var loader = new FakeImageLoader(_ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1 })));
        var cache = new CoverCache(loader, Placeholder, maxConcurrency: 4);

        cache.MaxConcurrency.Should().Be(4);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = async () => await cache.GetAsync(new CoverRef(null, "/p"), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>AC-COVER-04: a failed fetch yields the placeholder and never throws.</summary>
    [Fact]
    [Trait("AC", "AC-COVER-04")]
    public async Task Failure_Placeholder()
    {
        var ct = TestContext.Current.CancellationToken;
        var loader = new FakeImageLoader(_ => throw new IOException("boom"));
        var cache = new CoverCache(loader, Placeholder);

        byte[] result = await cache.GetAsync(new CoverRef(null, "/pfail"), ct);

        result.Should().Equal(Placeholder);
    }
}
