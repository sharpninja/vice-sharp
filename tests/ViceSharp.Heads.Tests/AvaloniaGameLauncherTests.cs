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

        public Task<RpcStatus> DropAndStartFileAsync(string filePath, CancellationToken ct = default)
        {
            Dropped = filePath;
            return Task.FromResult(RpcStatus.Ok());
        }

        public Task<RpcStatus> AttachFileAsync(MediaSlot slot, string filePath, CancellationToken ct = default)
        {
            Attached = (slot, filePath);
            return Task.FromResult(RpcStatus.Ok());
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
}
