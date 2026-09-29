namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Native lockstep for C64 $D800 stores under RAM-only PLA.
/// VICE: c64meminit.c config 4 uses ram_store, not colorram_store.
/// </summary>
[Collection("NativeVice")]
public sealed class C64ColorRamPlaNativeTests
{
    private const byte PlaIo = 0x37;
    private const byte PlaRamOnly = 0x34;

    /// <summary>
    /// FR: FR-MEM-005, TR: TR-MEM-PAGE-005, TR: TR-LOCKSTEP-10K.
    /// Use case: After KERNAL lockstep, $01=$34 must make a $D800 store hit
    ///   RAM on both ViceSharp and native x64sc (VICE ram_store). Color RAM
    ///   must stay at the I/O-mapped nibble from before the bank-out.
    /// Acceptance: Native PeekRam($D800) equals managed Read($D800) equals
    ///   $C5. Banking I/O back in does not show $05 (the would-be clobber).
    /// </summary>
    [ViceFact]
    public void RamOnlyPla_D800Store_MatchesNativeRamPeek()
    {
        using var validator = new LockstepValidator();
        var report = validator.Run(10_000);
        Assert.True(report.Success, $"KERNAL lockstep diverged at cycle {report.FirstMismatchCycle}: {report.Mismatch}");

        validator.HostMachine.Bus.Write(0x0001, PlaRamOnly);
        validator.NativeMachine.WriteBus(0x0001, PlaRamOnly);
        validator.HostMachine.Bus.Write(0xD800, 0xC5);
        validator.NativeMachine.WriteBus(0xD800, 0xC5);

        Assert.Equal(0xC5, validator.HostMachine.Bus.Read(0xD800));
        Assert.Equal(0xC5, validator.NativeMachine.PeekRam(0xD800));
        Assert.Equal(
            validator.NativeMachine.PeekRam(0xD800),
            validator.HostMachine.Bus.Read(0xD800));

        validator.HostMachine.Bus.Write(0x0001, PlaIo);
        Assert.NotEqual(0x05, validator.HostMachine.Bus.Read(0xD800) & 0x0F);
    }

    /// <summary>
    /// FR: FR-Validation-Lockstep.
    /// Use case: Run native x64sc to the Wolf64 first menu, snapshot that
    ///   state, then lockstep ViceSharp and x64sc from the snapshot to the
    ///   first in-game scene.
    /// Acceptance: When VICESHARP_WOLF64_LOCKSTEP=1, Success with no
    ///   FirstMismatchCycle, both sides $D015=$20, and playfield bytes match
    ///   at $A000-$BF3F (bitmap) and $D800-$DBE7 (color RAM).
    /// </summary>
    [ViceFact]
    public void Wolf64Disk_ReportsFirstMismatchCycle()
    {
        var enabled = Environment.GetEnvironmentVariable("VICESHARP_WOLF64_LOCKSTEP");
        if (!string.Equals(enabled, "1", StringComparison.Ordinal))
        {
            Assert.Skip("Set VICESHARP_WOLF64_LOCKSTEP=1 to run the Wolf64 disk lockstep probe.");
        }

        var dir = Path.Combine(FindRepoRoot(), "validation-output", "wolf64-frame-diff-20260915");
        var diskPath = Path.GetFullPath(Path.Combine(dir, "wolf64-lockstep.d64"));
        if (!File.Exists(diskPath))
            diskPath = Path.GetFullPath(Path.Combine(dir, "wolf64.d64"));
        Assert.True(File.Exists(diskPath), diskPath);

        byte[] diskImage;
        using (var stream = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            diskImage = new byte[stream.Length];
            stream.ReadExactly(diskImage);
        }
        var profile = C64MachineProfiles.C64Pal;
        var cyclesPerFrame = profile.CyclesPerLine * profile.RasterLines;
        const int captureFrames = 4000;
        const int lockstepFrames = 1200;
        var snapshotPath = Path.GetFullPath(Path.Combine(dir, "wolf64-menu.vsf"));
        using var validator = new LockstepValidator(
            profile.Id,
            diskImage: diskImage,
            diskPath: diskPath,
            recordRecentTrace: true);
        validator.CompareCia1InterruptStateFromCycle = -1;
        var resnap = string.Equals(
            Environment.GetEnvironmentVariable("VICESHARP_WOLF64_RESNAP"),
            "1",
            StringComparison.Ordinal);
        if (resnap || !File.Exists(snapshotPath))
        {
            validator.CaptureNativeWolf64MenuSnapshot(
                snapshotPath,
                maxNativeCycles: captureFrames * (long)cyclesPerFrame,
                cyclesPerFrame: cyclesPerFrame);
        }

        validator.QueueKeyAtFrame(60, "Return", holdFrames: 20, cyclesPerFrame);
        validator.QueueKeyAtFrame(360, "Return", holdFrames: 20, cyclesPerFrame);
        validator.QueueKeyAtFrame(660, "Return", holdFrames: 20, cyclesPerFrame);
        var report = validator.RunFromSnapshot(
            snapshotPath,
            lockstepFrames * (long)cyclesPerFrame,
            stopWhen: machine => machine.Bus.Peek(0xD015) == 0x20);
        var d015 = validator.HostMachine.Bus.Peek(0xD015);
        var d018 = validator.HostMachine.Bus.Peek(0xD018);
        var d019 = validator.HostMachine.Bus.Peek(0xD019);
        var d01a = validator.HostMachine.Bus.Peek(0xD01A);
        var cpu01 = validator.HostMachine.Bus.Peek(0x0001);
        Assert.True(
            report.Success,
            $"Wolf64 lockstep diverged at sample {report.FirstMismatchCycle} 01={cpu01:X2} d015={d015:X2} d018={d018:X2} d019={d019:X2} d01a={d01a:X2}: {report.Mismatch}{Environment.NewLine}CIA1: {validator.LastCia1InterruptStateMismatch ?? "match"}{Environment.NewLine}{validator.FormatLockstepContext()}");
        Assert.Equal(0x20, d015);
        Assert.Equal(0x20, validator.NativeMachine.PeekBus(0xD015));
        AssertPlayfieldBytesMatch(validator);
    }

    /// <summary>
    /// FR: FR-CIA-TIMER, TR: TR-LOCKSTEP-10K.
    /// Use case: Wolf64 2060433. With the d64 true-drive rig, managed CIA1
    ///   Timer A was 3 counts ahead of x64sc so underflow IRQ fired early.
    ///   Idle 10k/100k Timer A already matches; this pins the disk path.
    /// Acceptance: After 20,000 lockstep cycles with wolf64.d64 attached,
    ///   CIA1 Timer A equals native GetCiaState.
    /// </summary>
    [ViceFact]
    public void Wolf64Disk_Cia1TimerA_MatchesNativeAfter20000Cycles()
    {
        var dir = Path.Combine(FindRepoRoot(), "validation-output", "wolf64-frame-diff-20260915");
        var diskPath = Path.GetFullPath(Path.Combine(dir, "wolf64-lockstep.d64"));
        if (!File.Exists(diskPath))
            diskPath = Path.GetFullPath(Path.Combine(dir, "wolf64.d64"));
        Assert.True(File.Exists(diskPath), diskPath);

        byte[] diskImage;
        using (var stream = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            diskImage = new byte[stream.Length];
            stream.ReadExactly(diskImage);
        }

        using var validator = new LockstepValidator(
            C64MachineProfiles.C64Pal.Id,
            diskImage: diskImage,
            diskPath: diskPath,
            recordRecentTrace: false);
        var report = validator.Run(20_000);
        Assert.True(report.Success, $"CPU lockstep diverged at {report.FirstMismatchCycle}: {report.Mismatch}");

        var mTa = (ushort)(validator.HostMachine.Bus.Peek(0xDC04)
            | (validator.HostMachine.Bus.Peek(0xDC05) << 8));
        var nCia = validator.NativeMachine.GetCiaState(0);
        Assert.Equal(nCia.TimerA, mTa);
    }

    /// <summary>
    /// FR: FR-CIA-TIMER, TR: TR-LOCKSTEP-10K.
    /// Use case: Wolf64 2093252. Idle/disk Timer A matches through 2.05M,
    ///   then managed is 3 counts ahead ($4020 vs $4023) so Timer A IRQ is
    ///   delay-elapsed on LDA #$20 last CLK. Native irq_delay_cycles is still
    ///   1, so x64sc FETCHes STA ($D1),Y.
    /// Acceptance: With VICESHARP_WOLF64_LOCKSTEP=1, CIA1 Timer A matches
    ///   native GetCiaState from cycle 2,050,000 through 2,093,300.
    /// </summary>
    [ViceFact]
    public void Wolf64Disk_Cia1TimerA_MatchesNativeThroughIrqWindow()
    {
        var enabled = Environment.GetEnvironmentVariable("VICESHARP_WOLF64_LOCKSTEP");
        if (!string.Equals(enabled, "1", StringComparison.Ordinal))
        {
            Assert.Skip("Set VICESHARP_WOLF64_LOCKSTEP=1 to run the Wolf64 Timer A phase probe.");
        }

        var dir = Path.Combine(FindRepoRoot(), "validation-output", "wolf64-frame-diff-20260915");
        var diskPath = Path.GetFullPath(Path.Combine(dir, "wolf64-lockstep.d64"));
        if (!File.Exists(diskPath))
            diskPath = Path.GetFullPath(Path.Combine(dir, "wolf64.d64"));
        Assert.True(File.Exists(diskPath), diskPath);

        byte[] diskImage;
        using (var stream = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            diskImage = new byte[stream.Length];
            stream.ReadExactly(diskImage);
        }

        using var validator = new LockstepValidator(
            C64MachineProfiles.C64Pal.Id,
            diskImage: diskImage,
            diskPath: diskPath,
            recordRecentTrace: true);
        validator.CompareCia1TimerAFromCycle = 2_050_000;
        var report = validator.Run(2_093_300);
        Assert.True(
            report.Success,
            $"CIA1 Timer A or CPU diverged at sample {report.FirstMismatchCycle}: {validator.LastCia1TimerAMismatch} {report.Mismatch}{Environment.NewLine}{validator.FormatLockstepContext()}");
    }

    private static void AssertPlayfieldBytesMatch(LockstepValidator validator)
    {
        var host = validator.HostMachine.Bus;
        var native = validator.NativeMachine;
        for (var addr = 0xA000; addr <= 0xBF3F; addr++)
        {
            Assert.Equal(
                native.PeekRam((ushort)addr),
                host.Peek((ushort)addr));
        }

        for (var addr = 0xD800; addr <= 0xDBE7; addr++)
        {
            Assert.Equal(
                native.PeekRam((ushort)addr) & 0x0F,
                host.Peek((ushort)addr) & 0x0F);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
