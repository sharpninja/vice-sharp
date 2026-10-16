using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using ViceSharp.Xbox.RomM;
using Xunit;

namespace ViceSharp.Heads.Tests;

/// <summary>
/// FR-ROMM-LAUNCH-001 (AC-LAUNCH-05). Use case: the Xbox launcher reads the cached bytes, payload-attaches
/// them, and boots (Drive 8 autostart for a disk, cold reset for a cartridge).
/// </summary>
[Trait("Category", "Heads")]
public sealed class XboxGameLauncherTests
{
    private sealed class FakeSession : IXboxLaunchSession
    {
        public (MediaSlot Slot, string Name, byte[] Payload)? Attached { get; private set; }

        public int AutostartCalls { get; private set; }

        public int ColdResetCalls { get; private set; }

        public bool AttachResult { get; init; } = true;

        public ValueTask<bool> AttachMediaAsync(MediaSlot slot, string filePath, bool isReadOnly, byte[] payload, string displayName, CancellationToken cancellationToken = default)
        {
            Attached = (slot, displayName, payload);
            return ValueTask.FromResult(AttachResult);
        }

        public ValueTask AutostartDrive8Async(CancellationToken cancellationToken = default)
        {
            AutostartCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask ColdResetAsync(CancellationToken cancellationToken = default)
        {
            ColdResetCalls++;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>AC-LAUNCH-05: reads bytes, payload-attaches, and boots per media type.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-05")]
    public async Task PayloadAttachAndBoot()
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] bytes = { 1, 2, 3, 4, 5 };
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-xlaunch");
        try
        {
            string diskPath = Path.Combine(dir.FullName, "b.d64");
            await File.WriteAllBytesAsync(diskPath, bytes, ct);

            // Disk autostart -> payload attach to Drive 8 + AutostartDrive8, no cold reset.
            var disk = new FakeSession();
            LaunchOutcome diskOutcome = await new XboxGameLauncher(disk)
                .LaunchAsync(new AcquiredGame(diskPath, "b.d64", MediaKind.Disk), MediaSlot.Drive8, autostart: true, ct);

            diskOutcome.Success.Should().BeTrue();
            disk.Attached.Should().NotBeNull();
            disk.Attached!.Value.Slot.Should().Be(MediaSlot.Drive8);
            disk.Attached.Value.Name.Should().Be("b.d64");
            disk.Attached.Value.Payload.Should().Equal(bytes);
            disk.AutostartCalls.Should().Be(1);
            disk.ColdResetCalls.Should().Be(0);

            // Cartridge autostart -> cold reset, not disk autostart.
            string cartPath = Path.Combine(dir.FullName, "w.crt");
            await File.WriteAllBytesAsync(cartPath, bytes, ct);
            var cart = new FakeSession();
            await new XboxGameLauncher(cart)
                .LaunchAsync(new AcquiredGame(cartPath, "w.crt", MediaKind.Cartridge), MediaSlot.Cartridge, autostart: true, ct);

            cart.ColdResetCalls.Should().Be(1);
            cart.AutostartCalls.Should().Be(0);

            // Attach failure -> unsuccessful outcome, no boot.
            var failing = new FakeSession { AttachResult = false };
            LaunchOutcome failOutcome = await new XboxGameLauncher(failing)
                .LaunchAsync(new AcquiredGame(diskPath, "b.d64", MediaKind.Disk), MediaSlot.Drive8, autostart: true, ct);

            failOutcome.Success.Should().BeFalse();
            failing.AutostartCalls.Should().Be(0);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
