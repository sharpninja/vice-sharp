using FluentAssertions;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using Xunit;

namespace ViceSharp.Heads.Tests;

/// <summary>
/// FR-ROMM-LAUNCH-001 (AC-LAUNCH-06). Use case: the Avalonia launcher attaches+boots on autostart and
/// attaches-only otherwise, delegating to the shell surface.
/// </summary>
[Trait("Category", "Heads")]
public sealed class AvaloniaGameLauncherTests
{
    private sealed class FakeLaunchTarget : IGameLaunchTarget
    {
        public string? Dropped { get; private set; }

        public (MediaSlot Slot, string Path)? Attached { get; private set; }

        public Func<Task<RpcStatus>>? Launch { get; set; }

        public Task<RpcStatus> DropAndStartFileAsync(string filePath, CancellationToken ct = default)
        {
            Dropped = filePath;
            return Launch?.Invoke() ?? Task.FromResult(RpcStatus.Ok());
        }

        public Task<RpcStatus> AttachFileAsync(MediaSlot slot, string filePath, CancellationToken ct = default)
        {
            Attached = (slot, filePath);
            return Launch?.Invoke() ?? Task.FromResult(RpcStatus.Ok());
        }
    }

    /// <summary>AC-LAUNCH-06: autostart drops+starts; attach-only attaches to the resolved slot.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-06")]
    public async Task DelegatesToShell()
    {
        var ct = TestContext.Current.CancellationToken;
        var game = new AcquiredGame(@"C:\cache\1\b.d64", "b.d64", MediaKind.Disk);

        var autostartTarget = new FakeLaunchTarget();
        LaunchOutcome autostart = await new AvaloniaGameLauncher(autostartTarget)
            .LaunchAsync(game, MediaSlot.Drive8, autostart: true, ct);

        autostartTarget.Dropped.Should().Be(game.LocalPath);
        autostartTarget.Attached.Should().BeNull();
        autostart.Success.Should().BeTrue();

        var attachTarget = new FakeLaunchTarget();
        await new AvaloniaGameLauncher(attachTarget).LaunchAsync(game, MediaSlot.Drive8, autostart: false, ct);

        attachTarget.Attached.Should().Be((MediaSlot.Drive8, game.LocalPath));
        attachTarget.Dropped.Should().BeNull();
    }

    /// <summary>
    /// FR-ROMM-LAUNCH-001 / AC-LAUNCH-06: target failures are reported without escaping the
    /// desktop action, and a later synthetic game can still be launched through the same target.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("AC", "AC-LAUNCH-06")]
    public async Task TargetFailure_ReturnsActionableOutcome_AndAllowsNextLaunch(bool autostart, bool asynchronous)
    {
        var target = new FakeLaunchTarget
        {
            Launch = asynchronous
                ? FailAfterYieldAsync
                : () => throw new InvalidOperationException("Synthetic media attachment failed."),
        };
        var launcher = new AvaloniaGameLauncher(target);
        var game = new AcquiredGame("/synthetic/first.t64", "first.t64", MediaKind.Tape);

        LaunchOutcome failed = await launcher.LaunchAsync(game, MediaSlot.Tape, autostart,
            TestContext.Current.CancellationToken);

        failed.Success.Should().BeFalse();
        failed.Message.Should().Contain("first.t64").And.Contain("Synthetic media attachment failed.");

        target.Launch = null;
        LaunchOutcome retry = await launcher.LaunchAsync(
            new AcquiredGame("/synthetic/second.t64", "second.t64", MediaKind.Tape),
            MediaSlot.Tape, autostart, TestContext.Current.CancellationToken);

        retry.Success.Should().BeTrue();
        (autostart ? target.Dropped : target.Attached?.Path).Should().Be("/synthetic/second.t64");

        static async Task<RpcStatus> FailAfterYieldAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("Synthetic media attachment failed.");
        }
    }

    /// <summary>AC-LAUNCH-06: requested cancellation is a recoverable UI outcome, not an async-event exception.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("AC", "AC-LAUNCH-06")]
    public async Task RequestedCancellation_ReturnsCanceledOutcome_AndAllowsNextLaunch(bool autostart)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var target = new FakeLaunchTarget { Launch = () => Task.FromCanceled<RpcStatus>(cancellation.Token) };
        var launcher = new AvaloniaGameLauncher(target);
        var game = new AcquiredGame("/synthetic/first.t64", "first.t64", MediaKind.Tape);

        LaunchOutcome canceled = await launcher.LaunchAsync(game, MediaSlot.Tape, autostart, cancellation.Token);

        canceled.Success.Should().BeFalse();
        canceled.Message.Should().Be("Launch canceled.");
        target.Launch = null;
        (await launcher.LaunchAsync(game, MediaSlot.Tape, autostart,
            TestContext.Current.CancellationToken)).Success.Should().BeTrue();
    }
}
