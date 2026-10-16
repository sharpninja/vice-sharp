using System.Security.Cryptography;
using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Connection;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-05). Use case: a head persists the RomM connection so it can reconnect
/// without re-authenticating.
/// </summary>
[Trait("Category", "Library")]
public sealed class FileRomMConnectionStoreTests
{
    /// <summary>AC-CONN-05: save then load round-trips the connection; clear removes it.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-05")]
    public async Task SaveLoad_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-conn");
        try
        {
            string path = Path.Combine(dir.FullName, "connection.json");
            var protector = new TestCredentialProtector();
            var store = protector.CreateStore(path);

            (await store.LoadAsync(ct)).Should().BeNull();

            var connection = new RomMConnection("https://romm.local/", RomMAuthMode.ClientToken, "rmm_secret");
            await store.SaveAsync(connection, ct);

            string persisted = await File.ReadAllTextAsync(path, ct);
            persisted.Should().NotContain("rmm_secret");
            persisted.Should().NotContain("\"token\"", "the credential must not be serialized in the legacy plaintext shape");
            persisted.Should().Contain("protected_token");
            (await protector.CreateStore(path).LoadAsync(ct)).Should().Be(connection);
            protector.ProtectCalls.Should().Be(1);
            protector.UnprotectCalls.Should().Be(1);

            await store.ClearAsync(ct);
            (await store.LoadAsync(ct)).Should().BeNull();
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>AC-CONN-05 / TR-ROMM-SEC-001: migrate before returning, then reload the protected record.</summary>
    [Fact]
    public async Task Load_LegacyPlaintext_MigratesImmediately()
    {
        var ct = TestContext.Current.CancellationToken;
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-legacy");
        try
        {
            string path = Path.Combine(dir.FullName, "connection.json");
            await File.WriteAllTextAsync(
                path,
                """{"base_url":"https://romm.local/","auth_mode":0,"token":"legacy_secret","username":null}""",
                ct);
            var protector = new TestCredentialProtector();
            var store = protector.CreateStore(path);

            RomMConnection? loaded = await store.LoadAsync(ct);

            loaded.Should().NotBeNull();
            loaded!.Token.Should().Be("legacy_secret");
            string migrated = await File.ReadAllTextAsync(path, ct);
            migrated.Should().NotContain("legacy_secret");
            migrated.Should().Contain("protected_token");
            migrated.Should().NotContain("\"token\"");
            (await protector.CreateStore(path).LoadAsync(ct)).Should().Be(loaded);
            protector.ProtectCalls.Should().Be(1);
            protector.UnprotectCalls.Should().Be(1);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>TR-ROMM-SEC-001: the public store uses current-user DPAPI on Windows and fails closed elsewhere.</summary>
    [Fact]
    public async Task DefaultProtector_EnforcesPlatformContract()
    {
        var ct = TestContext.Current.CancellationToken;
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-platform");
        try
        {
            string path = Path.Combine(dir.FullName, "connection.json");
            var store = new FileRomMConnectionStore(path);
            var connection = new RomMConnection("https://romm.local/", RomMAuthMode.ClientToken, "platform_test_secret");

            if (OperatingSystem.IsWindows())
            {
                await store.SaveAsync(connection, ct);
                string persisted = await File.ReadAllTextAsync(path, ct);
                persisted.Should().NotContain(connection.Token);
                persisted.Should().NotContain("\"token\"");
                persisted.Should().Contain("protected_token");
                (await new FileRomMConnectionStore(path).LoadAsync(ct)).Should().Be(connection);
                await store.ClearAsync(ct);
                File.Exists(path).Should().BeFalse();
            }
            else
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => store.SaveAsync(connection, ct));
                Directory.GetFiles(dir.FullName).Should().BeEmpty("unsupported platforms must not persist a token");

                const string protectedJson = """{"base_url":"https://romm.local/","auth_mode":0,"protected_token":"opaque_test_value","username":null}""";
                await File.WriteAllTextAsync(path, protectedJson, ct);
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => store.LoadAsync(ct));
                (await File.ReadAllTextAsync(path, ct)).Should().Be(protectedJson);

                const string legacyJson = """{"base_url":"https://romm.local/","auth_mode":0,"token":"legacy_test_secret","username":null}""";
                await File.WriteAllTextAsync(path, legacyJson, ct);
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => store.LoadAsync(ct));
                (await File.ReadAllTextAsync(path, ct)).Should().Be(legacyJson);
                Directory.GetFiles(dir.FullName).Should().ContainSingle();
            }
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>TR-ROMM-SEC-001: protection failure cannot replace a saved connection or write a plaintext fallback.</summary>
    [Fact]
    public async Task Save_ProtectionFailure_PreservesPreviousConnection()
    {
        var ct = TestContext.Current.CancellationToken;
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-protection-failure");
        try
        {
            string path = Path.Combine(dir.FullName, "connection.json");
            var protector = new TestCredentialProtector();
            var store = protector.CreateStore(path);
            var original = new RomMConnection("https://romm.local/", RomMAuthMode.ClientToken, "original_test_secret");
            await store.SaveAsync(original, ct);
            string persisted = await File.ReadAllTextAsync(path, ct);
            protector.RejectProtection = true;

            var replacement = original with { Token = "replacement_test_secret" };
            await Assert.ThrowsAsync<CryptographicException>(() => store.SaveAsync(replacement, ct));

            (await File.ReadAllTextAsync(path, ct)).Should().Be(persisted);
            (await store.LoadAsync(ct)).Should().Be(original);
            Directory.GetFiles(dir.FullName).Should().ContainSingle();
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>TR-ROMM-SEC-001: failed legacy migration preserves the original file without returning its credential.</summary>
    [Fact]
    public async Task Load_LegacyProtectionFailure_PreservesOriginalFile()
    {
        var ct = TestContext.Current.CancellationToken;
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-migration-failure");
        try
        {
            string path = Path.Combine(dir.FullName, "connection.json");
            const string legacyJson = """{"base_url":"https://romm.local/","auth_mode":0,"token":"legacy_test_secret","username":null}""";
            await File.WriteAllTextAsync(path, legacyJson, ct);
            var protector = new TestCredentialProtector { RejectProtection = true };
            var store = protector.CreateStore(path);

            await Assert.ThrowsAsync<CryptographicException>(() => store.LoadAsync(ct));

            (await File.ReadAllTextAsync(path, ct)).Should().Be(legacyJson);
            Directory.GetFiles(dir.FullName).Should().ContainSingle();
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    // Only tests receive this opaque in-memory mapping; public construction always uses DPAPI.
    private sealed class TestCredentialProtector
    {
        private readonly Dictionary<string, string> _tokens = new(StringComparer.Ordinal);
        public int ProtectCalls { get; private set; }
        public int UnprotectCalls { get; private set; }
        public bool RejectProtection { get; set; }

        public FileRomMConnectionStore CreateStore(string path) => new(path, Protect, Unprotect);

        private string Protect(string token)
        {
            ProtectCalls++;
            if (RejectProtection)
                throw new CryptographicException("Simulated credential protection failure.");
            string protectedToken = Guid.NewGuid().ToString("N");
            _tokens.Add(protectedToken, token);
            return protectedToken;
        }

        private string Unprotect(string protectedToken)
        {
            UnprotectCalls++;
            return _tokens[protectedToken];
        }
    }
}
