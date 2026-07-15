namespace ViceSharp.TestHarness.Xbox.Fakes;

using System.Threading;

/// <summary>
/// A <see cref="SynchronizationContext"/> that counts posts and runs them inline (no
/// message loop in a test). Shared by the ViewModel dispatch guards
/// (FIX-XSETBLANK-001, IMPL-XBOXUWP-040): a ViewModel that captures the context at
/// construction must Post PropertyChanged raised off it, and raise inline on it.
/// </summary>
internal sealed class RecordingSyncContext : SynchronizationContext
{
    /// <summary>Number of notifications dispatched through <see cref="Post"/>.</summary>
    public int PostCount { get; private set; }

    /// <inheritdoc />
    public override void Post(SendOrPostCallback d, object? state)
    {
        PostCount++;
        d(state);
    }
}
