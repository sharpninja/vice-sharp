namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct unit tests for <see cref="EmulatorRuntimeRegistry"/>, the
/// in-memory session store underlying every service host
/// (EmulatorHostService, MediaServiceHost, InputServiceHost,
/// VideoServiceHost, SettingsServiceHost, CaptureServiceHost,
/// SnapshotServiceHost, MonitorServiceHost). These tests cover the
/// four public operations of the registry's surface (Add / TryGet /
/// Remove / Replace) plus the cross-cutting invariants every caller
/// relies on: case-insensitive lookup, duplicate-id rejection by Add,
/// duplicate-id replacement by Replace, null/whitespace guards, and
/// concurrency safety under simultaneous Add/TryGet/Remove from
/// multiple threads. Sessions are built via
/// <see cref="DefaultEmulatorRuntimeFactory"/> against
/// <see cref="MinimalHostArchitectureDescriptor"/> so the tests do
/// not require C64 ROM assets on disk and run in roughly the same
/// time as the rest of the host-boundary suite.
/// </summary>
public sealed class EmulatorRuntimeRegistryTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller adds a session, then asks the registry to
    /// resolve it.
    /// Acceptance: TryGet returns true and yields the exact same
    /// session reference that was added (no clone, no wrapping).
    /// </summary>
    [Fact]
    public void Add_ThenTryGet_ReturnsSameSession()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession();

        registry.Add(session);

        Assert.True(registry.TryGet(session.SessionId, out var resolved));
        Assert.Same(session, resolved);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller asks the registry to resolve a session id
    /// that was never added.
    /// Acceptance: TryGet returns false and emits a null session,
    /// which is the contract every service host relies on to translate
    /// into the standard missing-session NotFound RPC status.
    /// </summary>
    [Fact]
    public void TryGet_UnknownId_ReturnsFalseAndNull()
    {
        var registry = new EmulatorRuntimeRegistry();

        var found = registry.TryGet("ghost-session", out var session);

        Assert.False(found);
        Assert.Null(session);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller passes a null, empty, or whitespace session
    /// id to TryGet (bug in upstream code).
    /// Acceptance: TryGet short-circuits and returns false with a null
    /// session, so the host RPC translates it into NotFound rather
    /// than crashing with an internal exception.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGet_NullOrWhitespaceId_ReturnsFalse(string? sessionId)
    {
        var registry = new EmulatorRuntimeRegistry();

        var found = registry.TryGet(sessionId!, out var session);

        Assert.False(found);
        Assert.Null(session);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller resolves a session by an id that differs
    /// from the stored id only by letter case (the factory mints lower
    /// case ids; callers may upper-case it).
    /// Acceptance: TryGet succeeds for any case variant because the
    /// underlying dictionary is OrdinalIgnoreCase.
    /// </summary>
    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession();
        registry.Add(session);

        var found = registry.TryGet(session.SessionId.ToUpperInvariant(), out var resolved);

        Assert.True(found);
        Assert.Same(session, resolved);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: Defensive caller passes a null session to Add (bug).
    /// Acceptance: Add throws <see cref="ArgumentNullException"/>
    /// immediately, surfacing the misuse rather than registering a
    /// null entry that would later NRE inside an RPC handler.
    /// </summary>
    [Fact]
    public void Add_NullSession_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Add(null!));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller adds two sessions sharing the same id (bug
    /// in id minting or accidental re-Create with a constant id).
    /// Acceptance: Add throws <see cref="ArgumentException"/> on the
    /// duplicate, leaving the first session intact and reachable. This
    /// is the documented behaviour: Add is non-replacing; callers that
    /// want overwrite semantics must use Replace.
    /// </summary>
    [Fact]
    public void Add_DuplicateId_ThrowsAndPreservesOriginal()
    {
        var registry = new EmulatorRuntimeRegistry();
        var first = CreateSession("dup-id");
        var second = CreateSession("dup-id");

        registry.Add(first);

        Assert.Throws<ArgumentException>(() => registry.Add(second));

        Assert.True(registry.TryGet("dup-id", out var resolved));
        Assert.Same(first, resolved);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller intentionally replaces an existing session
    /// (e.g. on reset-and-rebuild flow where the session id is
    /// preserved but the underlying machine is swapped).
    /// Acceptance: Replace overwrites the existing entry; the second
    /// session is the one TryGet returns afterwards.
    /// </summary>
    [Fact]
    public void Replace_ExistingId_OverwritesSession()
    {
        var registry = new EmulatorRuntimeRegistry();
        var first = CreateSession("same-id");
        var second = CreateSession("same-id");
        registry.Add(first);

        registry.Replace(second);

        Assert.True(registry.TryGet("same-id", out var resolved));
        Assert.Same(second, resolved);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller invokes Replace for an id that has not been
    /// added yet (effectively upsert).
    /// Acceptance: Replace inserts the session and TryGet resolves it,
    /// matching the Dictionary indexer semantics on which Replace is
    /// built.
    /// </summary>
    [Fact]
    public void Replace_MissingId_InsertsSession()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession("new-id");

        registry.Replace(session);

        Assert.True(registry.TryGet("new-id", out var resolved));
        Assert.Same(session, resolved);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller passes a null session to Replace (bug).
    /// Acceptance: Replace throws <see cref="ArgumentNullException"/>
    /// immediately, mirroring Add's guard.
    /// </summary>
    [Fact]
    public void Replace_NullSession_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Replace(null!));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller closes a session via Remove and then asks
    /// the registry to resolve it again.
    /// Acceptance: Remove returns true on the first call, subsequent
    /// TryGet returns false. This is the contract
    /// EmulatorHostService.CloseSessionAsync relies on to make the
    /// session "vanish" from every downstream service host.
    /// </summary>
    [Fact]
    public void Remove_ExistingId_RemovesSession()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession();
        registry.Add(session);

        var removed = registry.Remove(session.SessionId);

        Assert.True(removed);
        Assert.False(registry.TryGet(session.SessionId, out _));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller invokes Remove for an unknown session id.
    /// Acceptance: Remove returns false and the registry remains
    /// unchanged (no side effect on other sessions).
    /// </summary>
    [Fact]
    public void Remove_UnknownId_ReturnsFalse()
    {
        var registry = new EmulatorRuntimeRegistry();
        var existing = CreateSession("keep-me");
        registry.Add(existing);

        var removed = registry.Remove("ghost-session");

        Assert.False(removed);
        Assert.True(registry.TryGet("keep-me", out var resolved));
        Assert.Same(existing, resolved);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: A caller passes a null, empty, or whitespace id to
    /// Remove.
    /// Acceptance: Remove returns false without consulting or mutating
    /// the dictionary, mirroring the TryGet guard.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Remove_NullOrWhitespaceId_ReturnsFalse(string? sessionId)
    {
        var registry = new EmulatorRuntimeRegistry();

        var removed = registry.Remove(sessionId!);

        Assert.False(removed);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 RuntimeRegistry).
    /// Use case: Many host threads concurrently Add, TryGet, and
    /// Remove sessions (worst case under heavy multi-client gRPC
    /// load). The registry is documented thread-safe via an internal
    /// lock.
    /// Acceptance: After all threads complete, the registry's state is
    /// consistent: every session that was added but not removed is
    /// resolvable, and no exception escaped to a worker thread (which
    /// would otherwise mark the test as failed).
    /// </summary>
    [Fact]
    public async Task Concurrent_AddTryGetRemove_DoesNotCorruptState()
    {
        var registry = new EmulatorRuntimeRegistry();
        const int sessionsPerThread = 25;
        const int threadCount = 8;
        var sessions = new EmulatorRuntimeSession[threadCount * sessionsPerThread];
        for (var i = 0; i < sessions.Length; i++)
            sessions[i] = CreateSession($"concurrent-{i}");

        using var addBarrier = new System.Threading.Barrier(threadCount);
        var addTasks = new Task[threadCount];
        for (var t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            addTasks[t] = Task.Run(() =>
            {
                addBarrier.SignalAndWait();
                var start = threadIndex * sessionsPerThread;
                for (var i = 0; i < sessionsPerThread; i++)
                    registry.Add(sessions[start + i]);
            }, TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(addTasks);

        foreach (var session in sessions)
            Assert.True(registry.TryGet(session.SessionId, out _));

        using var readBarrier = new System.Threading.Barrier(threadCount);
        var readRemoveTasks = new Task[threadCount];
        for (var t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            readRemoveTasks[t] = Task.Run(() =>
            {
                readBarrier.SignalAndWait();
                var start = threadIndex * sessionsPerThread;
                for (var i = 0; i < sessionsPerThread; i++)
                {
                    var id = sessions[start + i].SessionId;
                    registry.TryGet(id, out _);
                    if ((i & 1) == 0)
                        registry.Remove(id);
                }
            }, TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(readRemoveTasks);

        for (var t = 0; t < threadCount; t++)
        {
            for (var i = 0; i < sessionsPerThread; i++)
            {
                var id = sessions[(t * sessionsPerThread) + i].SessionId;
                var shouldBePresent = (i & 1) == 1;
                Assert.Equal(shouldBePresent, registry.TryGet(id, out _));
            }
        }
    }

    private static EmulatorRuntimeSession CreateSession(string? sessionId = null)
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);

        var session = factory.Create(new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId));

        if (sessionId is null)
            return session;

        return new EmulatorRuntimeSession(sessionId, session.Architecture, session.Machine);
    }
}
