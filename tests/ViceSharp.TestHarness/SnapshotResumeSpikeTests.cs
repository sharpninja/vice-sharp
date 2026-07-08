using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VSFLOCKSTEP-001 Slice 0 (throwaway feasibility spike).
///
/// Measures how cycle-deep a managed vice# machine stays in lockstep with native
/// VICE when both resume from the same staged .vsf, given only the state we can
/// inject TODAY (CPU registers + 64K RAM + processor port). The result decides
/// whether the production slices need full chip-state injection (VIC/CIA/SID).
///
/// Phase A diagnoses whether an externally-staged x64sc .vsf even loads into the
/// embedded shim (it does not, by machine-name/engine build differences). Phase B
/// uses a shim-generated .vsf - the correct lockstep oracle - to get the managed
/// divergence measurement. Exploratory: expected to be replaced by S1-S4.
/// </summary>
[Trait("Category", "SnapshotResume")]
public sealed class SnapshotResumeSpikeTests
{
    private const string ModelSelector = "c64";
    private const long StageCycles = 100_000;
    private const long MaxCompareCycles = 50_000;

    private readonly ITestOutputHelper _output;

    public SnapshotResumeSpikeTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// FR: FR-SNP-001, TR: TR-LOCKSTEP-VSF-001 (PLAN-VSFLOCKSTEP-001 Slice 0 spike).
    /// Use case: Phase A diagnostic - determine whether an externally-staged x64sc GUI
    /// .vsf (the supplied lockstep baseline, ready-c64sc-truedrive.vsf) gets past the
    /// embedded shim's machine-identity gate, logging the snapshot's machine name and
    /// the shim's ReadSnapshot rc / snapshot_last_error.
    /// Acceptance: snapshot_last_error is not 21 (SNAPSHOT_MACHINE_MISMATCH_ERROR) -
    /// the shim now identifies as C64SC - regardless of whether every optional module
    /// (true-drive DRIVE8-11 etc.) loads fully. Skips when the native shim or the .vsf
    /// fixture is absent.
    /// </summary>
    [Fact]
    public void Spike_ExternalX64ScVsf_LoadDiagnostic()
    {
        if (!ViceNative.IsAvailable)
            Assert.Skip(ViceNative.AvailabilityMessage);

        var vsfPath = LocateFixture("ready-c64sc-truedrive.vsf");
        if (vsfPath is null)
            Assert.Skip("External x64sc .vsf fixture not present.");

        var bytes = File.ReadAllBytes(vsfPath);
        var machineName = Encoding.ASCII.GetString(bytes, 21, 16).TrimEnd('\0');
        _output.WriteLine($"external .vsf machine name = '{machineName}' (snap v{bytes[19]}.{bytes[20]})");

        using var native = ViceNative.CreateInstance(ModelSelector);
        native.Reset();
        var rc = native.ReadSnapshot(vsfPath);
        var err = ViceNative.SnapshotLastError();
        _output.WriteLine($"shim ReadSnapshot rc={rc}, snapshot_last_error={err} ({DescribeSnapshotError(err)})");

        // The shim now identifies as C64SC (machine_class = VICE_MACHINE_C64SC) and
        // uses reSID, so an external x64sc .vsf is past the machine-identity gate
        // (no longer SNAPSHOT_MACHINE_MISMATCH_ERROR=21). Full module-level loading
        // of GUI-saved snapshots (true-drive DRIVE8-11 etc.) is a tracked follow-up;
        // the lockstep oracle uses shim-generated snapshots (see round-trip test).
        Assert.NotEqual(21, err); // past SNAPSHOT_MACHINE_MISMATCH_ERROR
    }

    /// <summary>
    /// FR: FR-SNP-001, TR: TR-LOCKSTEP-VSF-001 (PLAN-VSFLOCKSTEP-001 acceptance).
    /// Use case: the "stage in real VICE, snapshot, resume in vice#" contract - an
    /// externally-staged x64sc GUI .vsf (the supplied lockstep baseline, with the
    /// true-drive DRIVE8-11 module set) must resume fully into the embedded shim, which
    /// the round-trip spike only approximated with a shim-written snapshot.
    /// Acceptance: machine_read_snapshot returns rc 0 (every C64 module MAINCPU..USERPORT
    /// consumed; the non-fatal snapshot_last_error residue from probing optional modules
    /// is logged, not asserted), and the resumed MAINCPU A/X/Y/SP/PC each equal the
    /// values parsed from the snapshot's MAINCPU module. Skips when the native shim or
    /// the .vsf fixture is absent.
    /// </summary>
    [Fact]
    public void ExternalX64ScVsf_FullyResumes_CpuMatchesSnapshot()
    {
        if (!ViceNative.IsAvailable)
            Assert.Skip(ViceNative.AvailabilityMessage);

        var vsfPath = LocateFixture("ready-c64sc-truedrive.vsf");
        if (vsfPath is null)
            Assert.Skip("External x64sc .vsf fixture not present.");

        var parsed = ParseVsf(File.ReadAllBytes(vsfPath));

        using var native = ViceNative.CreateInstance(ModelSelector);
        native.Reset();
        var rc = native.ReadSnapshot(vsfPath);
        var err = ViceNative.SnapshotLastError();

        // machine_read_snapshot returning 0 is the authoritative load-success
        // signal: every C64 module (MAINCPU..USERPORT) was consumed. VICE leaves
        // a non-fatal snapshot_last_error residue from probing optional modules,
        // so it is logged rather than hard-asserted.
        Assert.True(rc == 0,
            $"External x64sc .vsf must load fully; rc={rc}, snapshot_last_error={err} ({DescribeSnapshotError(err)}).");
        _output.WriteLine($"load rc={rc}, snapshot_last_error={err} ({DescribeSnapshotError(err)})");

        var st = native.GetState();
        _output.WriteLine(
            $"snapshot MAINCPU A={parsed.A:X2} X={parsed.X:X2} Y={parsed.Y:X2} SP={parsed.S:X2} PC={parsed.PC:X4} | " +
            $"resumed A={st.A:X2} X={st.X:X2} Y={st.Y:X2} SP={st.S:X2} PC={st.PC:X4}");
        Assert.Equal(parsed.A, st.A);
        Assert.Equal(parsed.X, st.X);
        Assert.Equal(parsed.Y, st.Y);
        Assert.Equal(parsed.S, st.S);
        Assert.Equal(parsed.PC, st.PC);
    }

    /// <summary>
    /// FR: FR-SNP-001, TR: TR-LOCKSTEP-VSF-001 (PLAN-VSFLOCKSTEP-001 Slice 0 spike).
    /// Use case: Phase B - measure how cycle-deep a managed vice# machine stays in CPU
    /// lockstep with native VICE when both resume from the same shim-generated .vsf,
    /// injecting only the state available today (CPU registers + 64K RAM + processor
    /// port; no VIC/CIA/SID chip state). The measured depth decides whether the
    /// production slices need full chip-state injection.
    /// Acceptance: the shim stages ~100k cycles and writes a .vsf (rc 0), resumes it
    /// (rc 0), and the in-test MAINCPU/C64MEM parser matches the shim's authoritative
    /// resumed A/X/Y/SP/PC exactly; the first managed-vs-native CPU divergence within a
    /// 50k-cycle window is measured and logged (diagnostic, not gated). Skips when the
    /// native shim is unavailable.
    /// </summary>
    [Fact]
    public void Spike_ShimRoundTripResume_MeasuresLockstepDepth()
    {
        if (!ViceNative.IsAvailable)
            Assert.Skip(ViceNative.AvailabilityMessage);

        var stagePath = Path.Combine(Path.GetTempPath(), "vicesharp-shim-stage.vsf");

        // --- Stage a non-trivial state in the shim and snapshot it. ---
        using (var staging = ViceNative.CreateInstance(ModelSelector))
        {
            staging.Reset();
            for (long i = 0; i < StageCycles; i++)
                staging.Step();
            var wr = staging.WriteSnapshot(stagePath);
            Assert.True(wr == 0, $"vice_machine_write_snapshot returned {wr} (err={ViceNative.SnapshotLastError()}).");
        }

        Assert.True(File.Exists(stagePath), "Shim did not produce a .vsf.");
        var bytes = File.ReadAllBytes(stagePath);
        var parsed = ParseVsf(bytes);

        // --- Native: resume from the shim-written snapshot (authoritative oracle). ---
        using var native = ViceNative.CreateInstance(ModelSelector);
        native.Reset();
        var rc = native.ReadSnapshot(stagePath);
        Assert.True(rc == 0, $"ReadSnapshot returned {rc} (err={ViceNative.SnapshotLastError()}).");

        var nativeStart = native.GetState();
        _output.WriteLine(
            $"staged @~{StageCycles} cyc | vsf MAINCPU A={parsed.A:X2} X={parsed.X:X2} Y={parsed.Y:X2} " +
            $"SP={parsed.S:X2} P={parsed.P:X2} PC={parsed.PC:X4}");
        _output.WriteLine(
            $"native resumed       A={nativeStart.A:X2} X={nativeStart.X:X2} Y={nativeStart.Y:X2} " +
            $"SP={nativeStart.S:X2} P={nativeStart.P:X2} PC={nativeStart.PC:X4}");

        // Parser validated against native's own authoritative resume.
        Assert.Equal(parsed.A, nativeStart.A);
        Assert.Equal(parsed.X, nativeStart.X);
        Assert.Equal(parsed.Y, nativeStart.Y);
        Assert.Equal(parsed.S, nativeStart.S);
        Assert.Equal(parsed.PC, nativeStart.PC);

        // --- Managed: power-on reset, then inject the tractable state. ---
        var machine = MachineTestFactory.CreateC64Machine(ModelSelector);
        machine.Reset();

        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory ram)
            throw new InvalidOperationException("Managed machine does not expose system RAM as IMemory.");
        parsed.Ram.AsSpan().CopyTo(ram.Span);

        machine.Bus.Write(0x0000, parsed.Port0);
        machine.Bus.Write(0x0001, parsed.Port1);

        if (machine.Devices.GetByRole(DeviceRole.Cpu) is not Mos6502 cpu)
            throw new InvalidOperationException("Managed machine does not expose an Mos6502 CPU.");
        cpu.A = parsed.A;
        cpu.X = parsed.X;
        cpu.Y = parsed.Y;
        cpu.S = parsed.S;
        cpu.PC = parsed.PC;
        cpu.P = parsed.P;

        var ramMismatches = 0;
        foreach (var addr in new ushort[] { 0x0002, 0x00C1, 0x0277, 0x0400, 0x07E7, 0xC000, 0xFFFA })
        {
            if (ram.Span[addr] != native.PeekRam(addr))
                ramMismatches++;
        }
        _output.WriteLine($"RAM probe mismatches after inject: {ramMismatches}/7");

        // --- Step both forward; measure first CPU divergence. ---
        long firstMismatch = -1;
        string mismatchDetail = "(none within window)";
        long prevNativeCycle = 0;

        for (long i = 0; i < MaxCompareCycles; i++)
        {
            native.Step();
            var nativeCycle = native.GetState().Cycle;
            var delta = nativeCycle - prevNativeCycle;
            prevNativeCycle = nativeCycle;

            for (long j = 0; j < delta; j++)
                machine.Clock.Step();

            var n = native.GetState();
            var m = machine.GetState();
            if (n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P || n.PC != m.PC)
            {
                firstMismatch = nativeCycle;
                mismatchDetail =
                    $"native A={n.A:X2} X={n.X:X2} Y={n.Y:X2} SP={n.S:X2} P={n.P:X2} PC={n.PC:X4} | " +
                    $"managed A={m.A:X2} X={m.X:X2} Y={m.Y:X2} SP={m.S:X2} P={m.P:X2} PC={m.PC:X4}";
                break;
            }
        }

        _output.WriteLine("==== SPIKE RESULT (CPU regs + 64K RAM + port; no VIC/CIA/SID chip state) ====");
        if (firstMismatch < 0)
            _output.WriteLine($"lockstep HELD for all {MaxCompareCycles} compared cycles.");
        else
            _output.WriteLine($"first CPU divergence at native cycle {firstMismatch}: {mismatchDetail}");

        try { File.Delete(stagePath); } catch { /* best effort */ }
    }

    private readonly record struct ParsedVsf(
        ulong Clock, byte A, byte X, byte Y, byte S, byte P, ushort PC,
        byte Port0, byte Port1, byte[] Ram);

    private static ParsedVsf ParseVsf(byte[] b)
    {
        var maincpu = FindModuleData(b, "MAINCPU");
        var clock = BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(maincpu, 8));
        var a = b[maincpu + 8];
        var x = b[maincpu + 9];
        var y = b[maincpu + 10];
        var sp = b[maincpu + 11];
        var pc = BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(maincpu + 12, 2));
        var p = b[maincpu + 14];

        var c64mem = FindModuleData(b, "C64MEM");
        var port0 = b[c64mem + 0];
        var port1 = b[c64mem + 1];
        var ram = new byte[0x10000];
        Array.Copy(b, c64mem + 4, ram, 0, ram.Length);

        return new ParsedVsf(clock, a, x, y, sp, p, pc, port0, port1, ram);
    }

    /// <summary>
    /// Walks the .vsf container (each module's size field includes its 22-byte
    /// header) and returns the byte offset of the named module's DATA region.
    /// </summary>
    private static int FindModuleData(byte[] b, string name)
    {
        var pos = 58; // file header is 58 bytes
        while (pos + 22 <= b.Length)
        {
            var moduleName = Encoding.ASCII.GetString(b, pos, 16).TrimEnd('\0');
            var size = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(pos + 18, 4));
            if (moduleName == name)
                return pos + 22;
            if (size < 22 || pos + (int)size > b.Length)
                break;
            pos += (int)size;
        }

        throw new InvalidOperationException($"Module '{name}' not found in snapshot.");
    }

    private static string DescribeSnapshotError(int err) => err switch
    {
        0 => "no error",
        18 => "magic string mismatch",
        19 => "cannot read version",
        20 => "cannot read machine name",
        21 => "machine mismatch",
        24 => "module higher version",
        25 => "module incompatible",
        27 => "cannot read snapshot",
        _ => "see SNAPSHOT_* enum"
    };

    private static string? LocateFixture(string fileName)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 12 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "tests", "ViceSharp.TestHarness", "Fixtures", "Vsf", fileName);
            if (File.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }

        return null;
    }
}
