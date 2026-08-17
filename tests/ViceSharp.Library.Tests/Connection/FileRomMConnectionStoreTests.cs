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
            var store = new FileRomMConnectionStore(path);

            (await store.LoadAsync(ct)).Should().BeNull();

            var connection = new RomMConnection("https://romm.local/", RomMAuthMode.ClientToken, "rmm_secret");
            await store.SaveAsync(connection, ct);

            string persisted = await File.ReadAllTextAsync(path, ct);
            persisted.Should().NotContain("rmm_secret");
            persisted.Should().NotContain("\"token\"", "the credential must not be serialized in the legacy plaintext shape");
            persisted.Should().Contain("protected_token");
            (await store.LoadAsync(ct)).Should().Be(connection);

            await store.ClearAsync(ct);
            (await store.LoadAsync(ct)).Should().BeNull();
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

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
            var store = new FileRomMConnectionStore(path);

            RomMConnection? loaded = await store.LoadAsync(ct);

            loaded.Should().NotBeNull();
            loaded!.Token.Should().Be("legacy_secret");
            string migrated = await File.ReadAllTextAsync(path, ct);
            migrated.Should().NotContain("legacy_secret");
            migrated.Should().Contain("protected_token");
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
