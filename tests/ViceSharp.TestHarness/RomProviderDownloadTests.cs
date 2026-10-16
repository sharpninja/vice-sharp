namespace ViceSharp.TestHarness;

using System.Net;
using ViceSharp.RomFetch;
using Xunit;

/// <summary>
/// FR-CFG-002: verified ROM acquisition must recognize the desktop's canonical
/// filenames and reuse available files without depending on the network.
/// </summary>
public sealed class RomProviderDownloadTests
{
    /// <summary>
    /// FR-CFG-002.
    /// Use case: A previous download left an invalid canonical C64 ROM in the cache.
    /// Acceptance: Every canonical C64 role is rejected by its pinned SHA256 hash.
    /// </summary>
    [Theory]
    [InlineData("basic-901226-01.bin")]
    [InlineData("kernal-901227-03.bin")]
    [InlineData("chargen-901225-01.bin")]
    public void IsAvailable_CorruptCanonicalRom_ReturnsFalse(string fileName)
    {
        var root = CreateTempRoot();
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "C64"));
            File.WriteAllBytes(Path.Combine(directory.FullName, fileName), [1, 2, 3]);

            Assert.False(new RomProvider(root).IsAvailable(fileName, "C64"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-CFG-002.
    /// Use case: A requested ROM already exists in a configured fallback root.
    /// Acceptance: Acquisition returns that file's bytes without reading a nonexistent
    /// primary-root path or creating a duplicate copy.
    /// </summary>
    [Fact]
    public async Task DownloadRom_AvailableInFallbackRoot_ReturnsResolvedFile()
    {
        var root = CreateTempRoot();
        try
        {
            var primary = Path.Combine(root, "primary");
            var fallback = Path.Combine(root, "fallback");
            Directory.CreateDirectory(Path.Combine(fallback, "C64"));
            byte[] expected = [1, 2, 3, 4];
            const string fileName = "custom-kernal.bin";
            File.WriteAllBytes(Path.Combine(fallback, "C64", fileName), expected);
            var provider = new RomProvider(primary, [fallback]);

            var actual = await provider.DownloadRom(fileName, "C64", TestContext.Current.CancellationToken);

            Assert.Equal(expected, actual.ToArray());
            Assert.False(File.Exists(Path.Combine(primary, "C64", fileName)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-CFG-002.
    /// Use case: Desktop bootstrap requests a canonical ROM using a shared HTTP client.
    /// Acceptance: Each canonical name uses its pinned VICE URL, rejects corrupt
    /// responses before writing, and leaves the caller's client usable for retries.
    /// </summary>
    [Theory]
    [InlineData("basic-901226-01.bin")]
    [InlineData("kernal-901227-03.bin")]
    [InlineData("chargen-901225-01.bin")]
    public async Task DownloadRom_CanonicalName_RejectsUnverifiedBytesWithoutDisposingClient(string fileName)
    {
        var root = CreateTempRoot();
        try
        {
            using var handler = new RomResponseHandler(HttpStatusCode.OK);
            using var client = new HttpClient(handler);
            var provider = new RomProvider(root, [], client);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    provider.DownloadRom(fileName, "C64", TestContext.Current.CancellationToken));
                Assert.Contains("hash verification failed", error.Message, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Equal(2, handler.Requests.Count);
            Assert.All(handler.Requests, uri => Assert.Equal(
                new Uri($"https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/C64/{fileName}"), uri));
            Assert.False(File.Exists(Path.Combine(root, "C64", fileName)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-CFG-002.
    /// Use case: Desktop startup is cancelled before a ROM request begins.
    /// Acceptance: Cancellation propagates without issuing HTTP or writing a ROM.
    /// </summary>
    [Fact]
    public async Task DownloadRom_Cancelled_DoesNotRequestOrWriteFile()
    {
        var root = CreateTempRoot();
        try
        {
            using var handler = new RomResponseHandler(HttpStatusCode.OK);
            using var client = new HttpClient(handler);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var provider = new RomProvider(root, [], client);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                provider.DownloadRom("basic-901226-01.bin", "C64", cancellation.Token));

            Assert.Empty(handler.Requests);
            Assert.False(Directory.Exists(Path.Combine(root, "C64")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-CFG-002.
    /// Use case: The configured download service is unavailable during first startup.
    /// Acceptance: The HTTP error propagates and no failed response becomes a cached ROM.
    /// </summary>
    [Fact]
    public async Task DownloadRom_HttpFailure_DoesNotWriteFile()
    {
        var root = CreateTempRoot();
        try
        {
            using var handler = new RomResponseHandler(HttpStatusCode.ServiceUnavailable);
            using var client = new HttpClient(handler);
            var provider = new RomProvider(root, [], client);

            var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
                provider.DownloadRom("basic-901226-01.bin", "C64", TestContext.Current.CancellationToken));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
            Assert.Single(handler.Requests);
            Assert.False(File.Exists(Path.Combine(root, "C64", "basic-901226-01.bin")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ViceSharpRomDownloadTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class RomResponseHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public List<Uri?> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request.RequestUri);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new ByteArrayContent([1, 2, 3]),
            });
        }
    }
}
