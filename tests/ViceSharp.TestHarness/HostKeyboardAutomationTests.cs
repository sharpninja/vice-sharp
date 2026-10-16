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
        var bus = new MemoryBus(memory);
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

    /// <summary>
    /// FR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 autostart), TR: TR-MVVM-001,
    /// TEST-HOSTUI-001.
    /// Use case: Auto 8 must not lose physical key transitions while warp mode runs.
    /// Acceptance: once BASIC is ready, autostart feeds the KERNAL keyboard buffer
    /// with the LOAD command bytes instead of relying on frame-paced key down/up events.
    /// </summary>
    [Fact]
    public void Autostart_ReadyAndCursorBlinking_FeedsKernalKeyboardBuffer()
    {
        var memory = MemoryWithReady(cursorBlinkFlag: 0);
        var machine = MachineWithMemory(memory);
        var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();

        for (var i = 0; i < 30; i++)
            automation.AdvanceFrame(machine);

        Assert.Equal(10, memory[0x00C6]);
        Assert.Equal(
            [(byte)'L', (byte)'O', (byte)'A', (byte)'D', (byte)'"', (byte)'*', (byte)'"', (byte)',', (byte)'8', (byte)','],
            memory[0x0277..0x0281]);
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a BASIC-start PRG drop must type RUN once the prompt is ready.
    /// Acceptance: CreateBasicRun feeds RUN plus return into the KERNAL keyboard
    /// buffer after READY and a blinking cursor.
    /// </summary>
    [Fact]
    public void BasicRun_ReadyAndCursorBlinking_FeedsRun()
    {
        var memory = MemoryWithReady(cursorBlinkFlag: 0);
        var machine = MachineWithMemory(memory);
        var automation = HostKeyboardAutomation.CreateBasicRun();

        for (var i = 0; i < 30; i++)
            automation.AdvanceFrame(machine);

        Assert.Equal(4, memory[0x00C6]);
        Assert.Equal([(byte)'R', (byte)'U', (byte)'N', 13], memory[0x0277..0x027B]);
        Assert.Equal("BASIC RUN", automation.Description);
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: VIC-20 unexpanded screen RAM is $1E00 (KERNAL HIBASE $0288 =
    /// $1E), not C64 $0400. RUN after a BASIC-start PRG drop must wait on that
    /// screen.
    /// Acceptance: READY only at $1E00 with HIBASE $1E feeds RUN; $0400 stays
    /// untouched.
    /// </summary>
    [Fact]
    public void BasicRun_Vic20UnexpandedHibaseScreen_FeedsRun()
    {
        var memory = new byte[0x10000];
        memory[0x0288] = 0x1E;
        memory[CursorBlinkEnableFlag] = 0;
        ReadOnlySpan<byte> ready = [18, 5, 1, 4, 25];
        for (var i = 0; i < ready.Length; i++)
            memory[0x1E00 + i] = ready[i];

        var machine = MachineWithMemory(memory);
        var automation = HostKeyboardAutomation.CreateBasicRun();
        for (var i = 0; i < 30; i++)
            automation.AdvanceFrame(machine);

        Assert.Equal(4, memory[0x00C6]);
        Assert.Equal([(byte)'R', (byte)'U', (byte)'N', 13], memory[0x0277..0x027B]);
        Assert.Equal(0, memory[0x0400]);
    }

    private sealed class MemoryBus(byte[] memory) : IBus
    {
        public byte Read(ushort address) => memory[address];

        public void Write(ushort address, byte value) => memory[address] = value;

        public byte Peek(ushort address) => memory[address];

        public void RegisterDevice(IAddressSpace device)
        {
        }

        public void UnregisterDevice(IAddressSpace device)
        {
        }
    }
}
