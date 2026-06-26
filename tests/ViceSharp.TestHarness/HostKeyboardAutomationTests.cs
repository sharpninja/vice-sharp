namespace ViceSharp.TestHarness;

using System;
using System.Linq;
using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// Drive-8 autostart keyboard automation. The host types LOAD/RUN only once the
/// C64 BASIC prompt is genuinely ready for input: the
/// "READY." text is on screen AND the editor is flashing the cursor in its input loop
/// (zero-page $CC == 0). Checking the text alone raced the boot/LOAD and dropped the
/// keystrokes ("?SYNTAX ERROR" on autostart).
/// </summary>
public sealed class HostKeyboardAutomationTests
{
    private const int ScreenStart = 0x0400;
    private const int CursorBlinkEnableFlag = 0x00CC;

    // A fake C64-ish machine whose bus reads from a 64K memory image and which exposes
    // no keyboard device: the readiness gate is exercised before any key is pressed.
    private static IMachine MachineWithMemory(byte[] memory)
    {
        var bus = Substitute.For<IBus>();
        bus.Peek(Arg.Any<ushort>()).Returns(call => memory[(ushort)call[0]]);
        var devices = Substitute.For<IDeviceRegistry>();
        devices.All.Returns(Array.Empty<IDevice>());
        var machine = Substitute.For<IMachine>();
        machine.Bus.Returns(bus);
        machine.Devices.Returns(devices);
        return machine;
    }

    private static byte[] MemoryWithReady(byte cursorBlinkFlag)
    {
        var memory = new byte[0x10000];
        // "READY" in PETSCII screen codes at the top of screen RAM ($0400).
        ReadOnlySpan<byte> ready = [18, 5, 1, 4, 25];
        for (var i = 0; i < ready.Length; i++)
            memory[ScreenStart + i] = ready[i];
        memory[CursorBlinkEnableFlag] = cursorBlinkFlag;
        return memory;
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 autostart), TR: TR-MVVM-001,
    /// TEST-HOSTUI-001.
    /// Use case: drive-8 autostart must NOT press RUN while the C64 is still booting or
    /// loading - the cursor is not yet flashing - even though stale "READY" text is
    /// already on the screen.
    /// Acceptance: with READY present but the cursor-blink flag ($CC) non-zero, the
    /// automation stays in its waiting phase, never applies the ready action,
    /// and reports no error.
    /// </summary>
    [Fact]
    public void Autostart_ReadyTextButCursorNotBlinking_DoesNotProceed()
    {
        var machine = MachineWithMemory(MemoryWithReady(cursorBlinkFlag: 1));
        var applied = false;
        var automation = HostKeyboardAutomation.CreateC64Drive8Autostart(_ => { applied = true; return null; });

        for (var i = 0; i < 100; i++)
            automation.AdvanceFrame(machine);

        Assert.False(applied);
        Assert.True(automation.IsActive);
        Assert.Null(automation.LastError);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 autostart), TR: TR-MVVM-001,
    /// TEST-HOSTUI-001.
    /// Use case: once the BASIC prompt is genuinely ready - READY shown and the cursor
    /// flashing - drive-8 autostart proceeds with its configured ready action.
    /// Acceptance: with READY present and the cursor-blink flag ($CC) zero, the
    /// automation leaves its waiting phase and applies the ready action.
    /// </summary>
    [Fact]
    public void Autostart_ReadyAndCursorBlinking_AppliesReadyAction()
    {
        var machine = MachineWithMemory(MemoryWithReady(cursorBlinkFlag: 0));
        var applied = false;
        var automation = HostKeyboardAutomation.CreateC64Drive8Autostart(_ => { applied = true; return null; });

        for (var i = 0; i < 20; i++)
            automation.AdvanceFrame(machine);

        Assert.True(applied);
    }
}
