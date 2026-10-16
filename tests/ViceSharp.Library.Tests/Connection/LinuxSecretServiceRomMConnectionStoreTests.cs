using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Connection;

/// <summary>FR-ROMM-CONN-001 / TR-ROMM-SEC-001: Linux credentials remain in Secret Service.</summary>
public sealed class LinuxSecretServiceRomMConnectionStoreTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("vs-romm-secret-service-").FullName;
    private string MetadataPath => Path.Combine(_directory, "connection.json");
    private static RomMConnection Connection(string token = "rmm_secret'\";$(touch not-run)&")
        => new("http://localhost:8080/", RomMAuthMode.ClientToken, token);

    /// <summary>FR-ROMM-CONN-001. Use case: Restart and sign out. Acceptance: Keyring round-trip works without a token in metadata or arguments.</summary>
    [Fact]
    public async Task SaveLoadClear_UsesKeyringAndMetadataWithoutToken()
    {
        var runner = new FakeSecretTool();
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        var connection = Connection();
        Assert.Null(await store.LoadAsync(TestContext.Current.CancellationToken));

        await store.SaveAsync(connection, TestContext.Current.CancellationToken);
        var json = await File.ReadAllTextAsync(MetadataPath, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(connection.Token, json);
        Assert.DoesNotContain("\"token\"", json);
        Assert.Contains("secret_id", json);
        Assert.Equal(connection, await store.LoadAsync(TestContext.Current.CancellationToken));
        Assert.All(runner.Arguments, args => Assert.DoesNotContain(connection.Token, string.Join(" ", args)));
        Assert.Equal(connection.Token, runner.LastInput);

        await store.ClearAsync(TestContext.Current.CancellationToken);
        Assert.False(File.Exists(MetadataPath));
        Assert.Empty(runner.Secrets);
    }

    /// <summary>TR-ROMM-SEC-001. Use case: Unavailable or locked keyring. Acceptance: Save fails without writing plaintext or exposing command output.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(127)]
    public async Task Save_KeyringFailure_FailsWithoutTokenLeakage(int exitCode)
    {
        var runner = new FakeSecretTool { FailCommand = "store", ExitCode = exitCode };
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(Connection(), TestContext.Current.CancellationToken));
        Assert.DoesNotContain(Connection().Token, error.ToString());
        Assert.False(File.Exists(MetadataPath));
    }

    /// <summary>TR-ROMM-SEC-001. Use case: A saved keyring item is locked or missing. Acceptance: Load fails clearly instead of returning an empty token.</summary>
    [Fact]
    public async Task Load_KeyringLookupFailure_DoesNotReturnEmptyCredential()
    {
        var runner = new FakeSecretTool();
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        await store.SaveAsync(Connection(), TestContext.Current.CancellationToken);
        runner.FailCommand = "lookup";
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain(Connection().Token, error.ToString());
    }

    /// <summary>TR-ROMM-SEC-001. Use case: Metadata publication fails on replacement. Acceptance: The old connection survives and the new keyring item is cleaned.</summary>
    [Fact]
    public async Task Save_MetadataCommitFailure_PreservesPreviousCredential()
    {
        var runner = new FakeSecretTool();
        var failCommit = false;
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync, (source, destination) =>
        {
            if (failCommit)
                throw new IOException("Simulated metadata write failure.");
            File.Move(source, destination, overwrite: true);
        });
        await store.SaveAsync(Connection("old-token"), TestContext.Current.CancellationToken);
        var previous = await File.ReadAllTextAsync(MetadataPath, TestContext.Current.CancellationToken);
        failCommit = true;

        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(Connection("new-token"), TestContext.Current.CancellationToken));

        Assert.Equal(previous, await File.ReadAllTextAsync(MetadataPath, TestContext.Current.CancellationToken));
        Assert.Equal(Connection("old-token"), await store.LoadAsync(TestContext.Current.CancellationToken));
        Assert.Single(runner.Secrets);
    }

    /// <summary>TR-ROMM-SEC-001. Use case: Old credential cleanup fails after replacement. Acceptance: The new saved connection remains usable.</summary>
    [Fact]
    public async Task Save_OldSecretCleanupFailure_DoesNotInvalidateNewConnection()
    {
        var runner = new FakeSecretTool();
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        await store.SaveAsync(Connection("old-token"), TestContext.Current.CancellationToken);
        runner.FailCommand = "clear";

        await store.SaveAsync(Connection("new-token"), TestContext.Current.CancellationToken);

        Assert.Equal(Connection("new-token"), await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>TR-ROMM-SEC-001. Use case: Upgrade from plaintext metadata. Acceptance: Successful load migrates the token into the keyring before returning it.</summary>
    [Fact]
    public async Task Load_LegacyPlaintext_MigratesToKeyring()
    {
        var runner = new FakeSecretTool();
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        await File.WriteAllTextAsync(MetadataPath, LegacyJson(), TestContext.Current.CancellationToken);

        Assert.Equal(Connection("legacy-secret"), await store.LoadAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain("legacy-secret", await File.ReadAllTextAsync(MetadataPath, TestContext.Current.CancellationToken));
        Assert.Single(runner.Secrets);
    }

    /// <summary>TR-ROMM-SEC-001. Use case: Migration while the keyring is unavailable. Acceptance: Load fails and preserves the original file.</summary>
    [Fact]
    public async Task Load_LegacyMigrationFailure_PreservesOriginalFile()
    {
        var runner = new FakeSecretTool { FailCommand = "store" };
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        var previous = LegacyJson();
        await File.WriteAllTextAsync(MetadataPath, previous, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(previous, await File.ReadAllTextAsync(MetadataPath, TestContext.Current.CancellationToken));
    }

    /// <summary>TR-ROMM-SEC-001. Use case: Startup is cancelled. Acceptance: Cancellation propagates before invoking Secret Service.</summary>
    [Fact]
    public async Task Save_Cancelled_DoesNotInvokeKeyring()
    {
        var runner = new FakeSecretTool();
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(Connection(), cancellation.Token));
        Assert.Empty(runner.Arguments);
    }

    /// <summary>TR-ROMM-SEC-001. Use case: Empty authentication input. Acceptance: It is rejected before creating keyring or metadata state.</summary>
    [Fact]
    public async Task Save_EmptyToken_IsRejected()
    {
        var runner = new FakeSecretTool();
        var store = new LinuxSecretServiceRomMConnectionStore(MetadataPath, runner.RunAsync);
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(Connection(string.Empty), TestContext.Current.CancellationToken));
        Assert.Empty(runner.Arguments);
        Assert.False(File.Exists(MetadataPath));
    }

    private static string LegacyJson()
        => """{"base_url":"http://localhost:8080/","auth_mode":0,"token":"legacy-secret","username":null}""";

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private sealed class FakeSecretTool
    {
        public Dictionary<string, string> Secrets { get; } = [];
        public List<IReadOnlyList<string>> Arguments { get; } = [];
        public string? LastInput { get; private set; }
        public string? FailCommand { get; set; }
        public int ExitCode { get; set; } = 1;

        public Task<SecretToolResult> RunAsync(IReadOnlyList<string> arguments, string? input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Arguments.Add(arguments);
            if (arguments[0] == FailCommand)
                return Task.FromResult(new SecretToolResult(ExitCode, Connection().Token));
            var id = arguments[^1];
            switch (arguments[0])
            {
                case "store":
                    LastInput = input;
                    Secrets[id] = input!;
                    return Task.FromResult(new SecretToolResult(0, string.Empty));
                case "lookup":
                    return Task.FromResult(Secrets.TryGetValue(id, out var token)
                        ? new SecretToolResult(0, token + "\n")
                        : new SecretToolResult(1, string.Empty));
                case "clear":
                    Secrets.Remove(id);
                    return Task.FromResult(new SecretToolResult(0, string.Empty));
                default:
                    throw new InvalidOperationException("Unexpected command.");
            }
        }
    }
}
