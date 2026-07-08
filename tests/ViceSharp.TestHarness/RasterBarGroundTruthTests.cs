using System;
using System.Collections.Generic;
using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FR/TR/TEST: FR-VICII-RASTER-001 / TR-LOCKSTEP-VSF-001 / TEST-RASTERBAR-GT-001.
///
/// Ground-truth raster-bar schedule capture for the "Pieces of Light" demo segment.
/// Resumes a user-staged x64sc .vsf (the PROUDLY-PRESENT screen, where the $1229
/// stable-raster bar engine is live) into the embedded VICE shim - the authoritative
/// reference the user pointed at ("the raster bars are correctly aligned in VICE") -
/// and records, for every VIC border/background colour write the CPU performs, the
/// raster line and in-line cycle at which it lands.
///
/// This is the reference schedule the managed core must reproduce. It deliberately
/// detects writes by opcode at the program counter (STA $D020 = 8D 20 D0,
/// STA $D021 = 8D 21 D0) rather than by a hard-coded handler address, so it captures
/// EVERY bar write regardless of which IRQ source (raster vs CIA timer) drove it -
/// the "two different IRQ sources" the user suspected.
///
/// Snapshot-gated: skips cleanly when the shim or the staged .vsf is absent, so it
/// never runs in CI without the fixture present.
/// </summary>
[Trait("Category", "SnapshotResume")]
public sealed class RasterBarGroundTruthTests
{
    private const string ModelSelector = "c64";

    // ~3 PAL frames at 63 cycles x 312 lines = 19656 cycles/frame. Enough to catch
    // several complete passes of the 31-bar engine.
    private const long MaxCycles = 4 * 19656;

    private static readonly string[] SnapshotCandidates =
    [
        @"F:\GitHub\vice-sharp\vice-snapshot-20260630171307.vsf",
    ];

    private readonly ITestOutputHelper _output;

    public RasterBarGroundTruthTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// FR: FR-VICII-RASTER-001, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-RASTERBAR-GT-001.
    /// Use case: capture the authoritative raster-bar schedule - resume the user-staged
    /// Pieces-of-Light .vsf in the embedded VICE shim and record, for every STA $D020 /
    /// STA $D021 the CPU reaches (rising-edge opcode detection at the PC, so writes from
    /// EVERY IRQ source are caught), the raster line and in-line cycle where it lands,
    /// over ~4 PAL frames.
    /// Acceptance: the .vsf resumes with rc 0, and the captured colour-write schedule is
    /// non-empty (an empty schedule means the live demo state did not resume, not a
    /// passing no-op); the schedule is written to artifacts/rastercmp/groundtruth-bars.txt.
    /// Skips when the shim or the staged .vsf is absent.
    /// </summary>
    [Fact]
    public void GroundTruth_RasterBarSchedule_FromUserSnapshot()
    {
        if (!ViceNative.IsAvailable)
        {
            Assert.Skip(ViceNative.AvailabilityMessage);
            return;
        }

        var vsf = Array.Find(SnapshotCandidates, File.Exists);
        if (vsf is null)
        {
            Assert.Skip("Staged demo .vsf snapshot not present.");
            return;
        }

        using var native = ViceNative.CreateInstance(ModelSelector);
        native.Reset();
        var rc = native.ReadSnapshot(vsf);
        var err = ViceNative.SnapshotLastError();
        Assert.True(rc == 0,
            $"Staged demo .vsf must resume in the shim; rc={rc}, snapshot_last_error={err}.");

        var entry = (ushort)(native.PeekRam(0x0314) | (native.PeekRam(0x0315) << 8));
        var vic = native.GetVicState();
        _output.WriteLine(
            $"resumed rc={rc} err={err}; CINV $0314 -> ${entry:X4}; " +
            $"start VIC line=${vic.RasterLine:X3} cyc={vic.RasterCycle} bad={vic.BadLine}");
        _output.WriteLine("---- ground-truth colour-write schedule (VICE shim) ----");
        _output.WriteLine("idx  reg     PC    X   color  rasterLine  inLineCycle  dCycle");

        var samples = new List<Sample>(256);
        bool wasAtStore = false;
        long prevCycle = 0;

        for (long i = 0; i < MaxCycles; i++)
        {
            native.Step();
            var s = native.GetState();

            // Rising-edge detect: capture once when the PC first lands on a
            // STA $D020 / STA $D021 instruction, not on every master cycle the
            // multi-cycle store occupies.
            var op = native.PeekRam(s.PC);
            var lo = native.PeekRam((ushort)(s.PC + 1));
            var hi = native.PeekRam((ushort)(s.PC + 2));
            var isBorderStore = op == 0x8D && hi == 0xD0 && (lo == 0x20 || lo == 0x21);

            if (isBorderStore && !wasAtStore)
            {
                var v = native.GetVicState();
                samples.Add(new Sample(
                    Register: lo,
                    Pc: s.PC,
                    X: s.X,
                    Color: s.A,
                    RasterLine: v.RasterLine,
                    InLineCycle: v.RasterCycle,
                    NativeCycle: s.Cycle,
                    DeltaCycle: s.Cycle - prevCycle));
                prevCycle = s.Cycle;
            }

            wasAtStore = isBorderStore;
        }

        var lines = new List<string>
        {
            $"# ground-truth raster-bar schedule (VICE shim) from {Path.GetFileName(vsf)}",
            $"# CINV $0314 -> ${entry:X4}; start line=${vic.RasterLine:X3} cyc={vic.RasterCycle}",
            "idx\treg\tPC\tX\tcolor\trasterLine\tinLineCycle\tdCycle",
        };

        for (var i = 0; i < samples.Count; i++)
        {
            var smp = samples[i];
            var row =
                $"{i}\t$D0{smp.Register:X2}\t{smp.Pc:X4}\t{smp.X:X2}\t{smp.Color:X2}\t" +
                $"{smp.RasterLine}\t{smp.InLineCycle}\t{smp.DeltaCycle}";
            lines.Add(row);
            _output.WriteLine(row);
        }

        _output.WriteLine($"---- total colour writes captured: {samples.Count} over {MaxCycles} cycles ----");

        TryWriteArtifact("groundtruth-bars.txt", lines);

        // The bar engine must actually run in this snapshot; an empty schedule means
        // the snapshot did not resume into the live demo state (the real failure to
        // surface), not a passing no-op.
        Assert.NotEmpty(samples);
    }

    /// <summary>
    /// FR: FR-VICII-RASTER-001, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-RASTERBAR-GT-002.
    /// Use case: settle whether the demo segment is driven by one raster IRQ or by
    /// raster + CIA-timer IRQ ("two different IRQ sources") by decoding the IRQ topology
    /// directly from the staged .vsf chip modules (VIC-II, CIA1, CIA2) - no shim
    /// required - scoping exactly which chip state the managed lockstep must inject.
    /// Acceptance: the CIA1 module is present and decodable from the snapshot (its
    /// timer/ICR/CRA/CRB fields plus armed-IRQ flags are reported and written to
    /// artifacts/rastercmp/irq-topology.txt); the large version-specific VIC-II blob is
    /// reported as shim-read only, not hand-parsed. Skips when the staged .vsf is absent.
    /// </summary>
    [Fact]
    public void IrqTopology_FromUserSnapshot_ReportsArmedSources()
    {
        var vsf = Array.Find(SnapshotCandidates, File.Exists);
        if (vsf is null)
        {
            Assert.Skip("Staged demo .vsf snapshot not present.");
            return;
        }

        var b = File.ReadAllBytes(vsf);

        var lines = new List<string>
        {
            $"# IRQ topology from {Path.GetFileName(vsf)}",
        };

        // VIC-II: in this x64sc (VICE 3.x) build the VIC-II snapshot module is a
        // large, version-specific blob (~123 KB, raster cache included), so the
        // historical fixed register offset documented in vicii-snapshot.c is NOT
        // reliable here. VIC register extraction for the managed lockstep is done
        // via a shim accessor (vicii.regs[]) rather than hand-parsed. Raster-IRQ
        // activity is instead proven empirically by
        // GroundTruth_RasterBarSchedule_FromUserSnapshot, which shows the $1229
        // handler acking $D019 and painting the bar staircase at raster positions.
        var vic = TryFindModuleData(b, "VIC-II");
        lines.Add(vic < 0
            ? "VIC-II: module not present"
            : $"VIC-II: module present (dataStart={vic}); registers read via shim accessor, not hand-parsed");

        // --- CIA1 / CIA2 modules (binary order per ciacore snapshot). ---
        foreach (var name in new[] { "CIA1", "CIA2" })
        {
            int off = TryFindModuleData(b, name);
            if (off < 0)
            {
                lines.Add($"{name}: module not present");
                continue;
            }

            int taCur = b[off + 4] | (b[off + 5] << 8);
            int tbCur = b[off + 6] | (b[off + 7] << 8);
            byte icrMask = b[off + 13];
            byte cra = b[off + 14];
            byte crb = b[off + 15];
            int taLatch = b[off + 16] | (b[off + 17] << 8);
            int tbLatch = b[off + 18] | (b[off + 19] << 8);
            byte icrData = b[off + 20];
            bool taIrqArmed = (icrMask & 0x01) != 0 && (cra & 0x01) != 0;
            bool tbIrqArmed = (icrMask & 0x02) != 0 && (crb & 0x01) != 0;
            lines.Add(
                $"{name}: ICRmask={icrMask:X2} ICRdata={icrData:X2} CRA={cra:X2} CRB={crb:X2} " +
                $"TAcur={taCur} TAlatch={taLatch} TBcur={tbCur} TBlatch={tbLatch} " +
                $"timerA_irqArmed={taIrqArmed} timerB_irqArmed={tbIrqArmed}");
        }

        foreach (var l in lines)
            _output.WriteLine(l);
        TryWriteArtifact("irq-topology.txt", lines);

        // Reliable invariant: the C64 CIA1 module must be present and decodable; the
        // CIA layout is compact and stable (unlike the large VIC-II blob), so this is
        // honest evidence the snapshot's interrupt-controller state was parsed.
        Assert.True(TryFindModuleData(b, "CIA1") >= 0, "CIA1 module must be present in the snapshot.");
    }

    private static int TryFindModuleData(byte[] b, string name)
    {
        try { return FindModuleData(b, name); }
        catch (InvalidOperationException) { return -1; }
    }

    private static int FindModuleData(byte[] b, string name)
    {
        var pos = 58; // file header is 58 bytes
        while (pos + 22 <= b.Length)
        {
            var moduleName = System.Text.Encoding.ASCII.GetString(b, pos, 16).TrimEnd('\0', ' ');
            var size = BitConverter.ToUInt32(b, pos + 18);
            if (moduleName == name)
                return pos + 22;
            if (size < 22 || pos + (int)size > b.Length)
                break;
            pos += (int)size;
        }

        throw new InvalidOperationException($"Module '{name}' not found in snapshot.");
    }

    private void TryWriteArtifact(string fileName, IEnumerable<string> lines)
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            for (var i = 0; i < 12 && dir is not null; i++)
            {
                if (File.Exists(Path.Combine(dir, "ViceSharp.slnx")))
                {
                    var outDir = Path.Combine(dir, "artifacts", "rastercmp");
                    Directory.CreateDirectory(outDir);
                    var path = Path.Combine(outDir, fileName);
                    File.WriteAllLines(path, lines);
                    _output.WriteLine($"wrote {path}");
                    return;
                }

                dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"artifact write skipped: {ex.Message}");
        }
    }

    private readonly record struct Sample(
        byte Register,
        ushort Pc,
        byte X,
        byte Color,
        ushort RasterLine,
        byte InLineCycle,
        long NativeCycle,
        long DeltaCycle);
}
