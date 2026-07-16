using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Threading;

/// <summary>
/// TR-ROMM-THREAD-001 (AC-BROWSE-08). Use case: background library loads mutate observable state; the
/// PropertyChanged notification must be marshaled to the captured UI context so the XAML binding never
/// throws RPC_E_WRONG_THREAD.
/// </summary>
[Trait("Category", "Library")]
public sealed class LibraryObservableObjectTests
{
    private sealed class Probe : LibraryObservableObject
    {
        private int _value;

        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    private sealed class RecordingContext : SynchronizationContext
    {
        private int _posts;

        public int Posts => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            d(state);
        }
    }

    /// <summary>AC-BROWSE-08: a change raised from a background thread is dispatched via the captured context.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-08")]
    public async Task OffContext_DispatchesViaCapturedContext()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        var ctx = new RecordingContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(ctx);
            var probe = new Probe();
            int changes = 0;
            probe.PropertyChanged += (_, _) => Interlocked.Increment(ref changes);

            await Task.Run(() => probe.Value = 5);

            changes.Should().Be(1);
            ctx.Posts.Should().BeGreaterThan(0, "the background change must be posted to the captured UI context");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    /// <summary>AC-BROWSE-08: a change raised on the captured context is raised inline (no post).</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-08")]
    public void OnContext_RaisesInline()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        var ctx = new RecordingContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(ctx);
            var probe = new Probe();
            int changes = 0;
            probe.PropertyChanged += (_, _) => changes++;

            probe.Value = 7;

            changes.Should().Be(1);
            ctx.Posts.Should().Be(0, "an on-context change must be raised inline");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
