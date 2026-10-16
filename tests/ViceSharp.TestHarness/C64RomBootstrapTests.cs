namespace ViceSharp.TestHarness;

using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Host.Startup;
using Xunit;

/// <summary>FR-CFG-002 / TEST-CFG-001: desktop first-run ROM acquisition and offline reuse.</summary>
public sealed class C64RomBootstrapTests
{
    private static readonly string[] Required =
        [C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character];

    /// <summary>FR-CFG-002 / TEST-CFG-001. Use case: Fresh install. Acceptance: All three canonical ROMs are acquired.</summary>
    [Fact]
    public async Task EmptyCache_DownloadsCanonicalSet()
    {
        var available = new HashSet<string>();
        var provider = CreateProvider(available);
        var downloaded = new List<string>();

        await C64RomBootstrap.EnsureCoreSetAsync(provider, (name, _) =>
        {
            downloaded.Add(name);
            available.Add(name);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        Assert.Equal(Required, downloaded);
        Assert.True(new C64RomSet().IsComplete(provider));
    }

    /// <summary>FR-CFG-002 / TEST-CFG-001. Use case: Offline cached start. Acceptance: No download is attempted.</summary>
    [Fact]
    public async Task CompleteCache_DoesNotDownload()
    {
        var provider = CreateProvider(new HashSet<string>(Required));
        await C64RomBootstrap.EnsureCoreSetAsync(provider,
            (_, _) => throw new HttpRequestException("Offline"), TestContext.Current.CancellationToken);
    }

    /// <summary>FR-CFG-002 / TEST-CFG-001. Use case: Interrupted first run. Acceptance: Only the missing ROM is fetched.</summary>
    [Fact]
    public async Task PartialCache_DownloadsOnlyMissingRom()
    {
        var available = new HashSet<string>(Required.Take(2));
        var downloaded = new List<string>();
        await C64RomBootstrap.EnsureCoreSetAsync(CreateProvider(available), (name, _) =>
        {
            downloaded.Add(name);
            available.Add(name);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);
        Assert.Equal([C64ViceRomNames.Character], downloaded);
    }

    /// <summary>FR-CFG-002 / TEST-CFG-001. Use case: Download failure. Acceptance: Provisioning fails with the original error.</summary>
    [Fact]
    public async Task DownloadFailure_IsPropagated()
    {
        var attempts = 0;
        await Assert.ThrowsAsync<HttpRequestException>(() => C64RomBootstrap.EnsureCoreSetAsync(
            CreateProvider([]), (_, _) =>
            {
                attempts++;
                throw new HttpRequestException("Offline");
            }, TestContext.Current.CancellationToken));
        Assert.Equal(1, attempts);
    }

    /// <summary>FR-CFG-002 / TEST-CFG-001. Use case: Cancelled startup. Acceptance: No download occurs and cancellation propagates.</summary>
    [Fact]
    public async Task CancelledStartup_DoesNotDownload()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => C64RomBootstrap.EnsureCoreSetAsync(
            CreateProvider([]), (_, _) => throw new InvalidOperationException("Unexpected download"), cancellation.Token));
    }

    /// <summary>FR-CFG-002 / TEST-CFG-001. Use case: Incomplete acquisition. Acceptance: Missing files prevent success.</summary>
    [Fact]
    public async Task DownloadWithoutUsableRom_Fails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => C64RomBootstrap.EnsureCoreSetAsync(
            CreateProvider([]), (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken));
    }

    private static IRomProvider CreateProvider(HashSet<string> available)
    {
        var provider = Substitute.For<IRomProvider>();
        provider.IsAvailable(Arg.Any<string>(), "C64").Returns(call => available.Contains(call.ArgAt<string>(0)));
        return provider;
    }
}
