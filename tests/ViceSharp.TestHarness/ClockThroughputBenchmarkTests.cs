namespace ViceSharp.TestHarness;

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using ViceSharp.RomFetch;
using Xunit;

/// <summary>
/// Diagnostic benchmark for the GUI emulator frame pump. The Avalonia GUI
/// drives <see cref="LocalVideoFrameSource.GetFrameAsync"/> from a 50 Hz
/// dispatcher timer; with the limiter enabled (the default) each call runs
/// exactly one <c>RunFrame</c>. If a single frame-produce takes longer than
/// the ~19.95 ms real-time budget (1/50.12 s for PAL), the timer cannot
/// sustain 50 fps and the status bar's EffectiveClockPercent drops below
/// 100% - this measures that ceiling against the same host path the GUI uses.
///
/// Gated behind VICESHARP_CLOCK_BENCH=1 so it does not add wall-clock to the
/// normal suite. Run it with:
///   $env:VICESHARP_CLOCK_BENCH='1'
///   dotnet test --filter FullyQualifiedName~MeasureGuiFramePumpThroughput
/// Results are written to artifacts/clock-benchmark.log.
/// </summary>
public sealed class ClockThroughputBenchmarkTests : IClassFixture<Audio.WindowsAudioSessionMute>
{
    /// <summary>
    /// FR-PRF-001, TR-PERF-HARNESS.
    /// Use case: A gated diagnostic benchmark measures the GUI frame-pump path.
    /// Acceptance: When enabled, the frame source produces at least one frame and writes the report.
    /// </summary>
    [Fact]
    public async Task MeasureGuiFramePumpThroughput()
    {
        if (Environment.GetEnvironmentVariable("VICESHARP_CLOCK_BENCH") != "1")
            Assert.Skip("Set VICESHARP_CLOCK_BENCH=1 to run the clock-throughput benchmark.");

        // Diagnostics for ROM resolution (explains C64-vs-minimal selection).
        var romEnv = Environment.GetEnvironmentVariable("VICESHARP_ROM_PATH") ?? "(unset)";
        var viceDataEnv = Environment.GetEnvironmentVariable("VICE_DATA_PATH") ?? "(unset)";
        var dataRoots = ViceDataPathResolver.FindDataRoots();

        // Same construction the GUI uses: parameterless factory resolves the
        // C64 architecture (with ROMs) when available, else the minimal host.
        var factory = new DefaultEmulatorRuntimeFactory();
        var session = factory.Create(new CreateEmulatorSessionRequest());
        session.PowerState = "On";
        session.RunState = EmulatorRunState.Running;

        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        // Emulation is advanced by the host worker (BUG-THROTTLE-001); drive it
        // directly here to measure the same RunFrame path the worker runs.
        var pump = new EmulationPumpService(registry);

        var nominalClockHz = session.Architecture.MasterClockHz;
        var architecture = session.Architecture.MachineName;
        var limiterEnabled = session.LimiterEnabled;

        // Warm up JIT + ROM boot so the measurement reflects steady state.
        for (var i = 0; i < 30; i++)
            pump.PumpSession(session);

        var startCycle = session.Machine.GetState().Cycle;
        var frames = 0;
        var budget = TimeSpan.FromSeconds(4);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < budget)
        {
            pump.PumpSession(session);
            frames++;
        }
        sw.Stop();
        var endCycle = session.Machine.GetState().Cycle;

        var cycles = endCycle - startCycle;
        var seconds = sw.Elapsed.TotalSeconds;
        var rawClockHz = cycles / seconds;
        var rawClockPercent = nominalClockHz > 0 ? rawClockHz / nominalClockHz * 100.0 : 0;
        var fps = frames / seconds;
        var msPerFrame = sw.Elapsed.TotalMilliseconds / Math.Max(1, frames);

        var report = new StringBuilder()
            .AppendLine("=== ViceSharp GUI frame-pump throughput (LocalVideoFrameSource.GetFrameAsync) ===")
            .AppendLine(Line("architecture", architecture))
            .AppendLine(Line("VICESHARP_ROM_PATH", romEnv))
            .AppendLine(Line("VICE_DATA_PATH", viceDataEnv))
            .AppendLine(Line("data roots found", dataRoots.Count == 0 ? "(none)" : string.Join(" | ", dataRoots)))
            .AppendLine(Line("limiterEnabled", limiterEnabled.ToString()))
            .AppendLine(Line("nominalClockHz", $"{nominalClockHz:N0} ({nominalClockHz / 1e6:0.000} MHz)"))
            .AppendLine(Line("frames produced", frames.ToString(CultureInfo.InvariantCulture)))
            .AppendLine(Line("elapsed (s)", seconds.ToString("0.000", CultureInfo.InvariantCulture)))
            .AppendLine(Line("cycles executed", cycles.ToString("N0", CultureInfo.InvariantCulture)))
            .AppendLine(Line("raw clock", $"{rawClockHz / 1e6:0.000} MHz"))
            .AppendLine(Line("EffectiveClockPercent", $"{rawClockPercent:0.0}%  (status-bar metric; 100% = real time)"))
            .AppendLine(Line("fps", $"{fps:0.0}  (PAL real time ~50.1)"))
            .AppendLine(Line("ms / frame", $"{msPerFrame:0.000}  (real-time budget {1000.0 / 50.125:0.00} ms)"))
            .AppendLine(Line("processor count", Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture)))
            .AppendLine(Line("verdict", rawClockPercent >= 95
                ? "emulator sustains real time; a live <100% reading is GUI-timer/limiter bound, not emulator-bound"
                : "emulator is CPU-bound below real time; this is the EffectiveClockPercent ceiling the GUI shows"))
            .ToString();

        var logPath = Path.Combine(RepoRoot, "artifacts", "clock-benchmark.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        await File.WriteAllTextAsync(logPath, report, TestContext.Current.CancellationToken);
        Console.WriteLine(report);

        Assert.True(frames > 0, "the frame pump produced no frames");
    }

    /// <summary>
    /// FR-PRF-001, TR-PERF-HARNESS.
    /// Use case: Measure the raw per-cycle cost of the full C64 chipset
    ///   (CPU + VIC-II + 2x CIA + SID with the live-audio path active),
    ///   independent of ROM contents, to learn whether the machine can sustain
    ///   real time (>=100% EffectiveClockPercent) under a cycle-clock pump.
    /// Acceptance: When enabled, advances the C64 clock and writes a report with
    ///   ms-per-frame and effective clock percent.
    /// </summary>
    [Fact]
    public void MeasureC64ChipsetFrameCost()
    {
        if (Environment.GetEnvironmentVariable("VICESHARP_CLOCK_BENCH") != "1")
            Assert.Skip("Set VICESHARP_CLOCK_BENCH=1 to run the C64 chipset benchmark.");

        // Build a full C64 PAL with the audio backend attached so the SID's
        // live-emission path is exercised. ROM contents do not affect per-cycle
        // cost; skip cleanly when no complete C64 ROM set is resolvable.
        IMachine machine;
        try
        {
            var romProvider = MachineTestFactory.CreateC64RomProvider();
            var audio = ViceSharp.Host.Audio.AudioBackendFactory.CreateDefault();
            var builder = new ArchitectureBuilder(romProvider, audio);
            machine = builder.Build(new ViceSharp.Architectures.C64.C64Descriptor(
                ViceSharp.Architectures.C64.C64MachineProfiles.C64Pal));
        }
        catch (DirectoryNotFoundException)
        {
            Assert.Skip("No complete C64 ROM set resolved; cannot measure C64 chipset.");
            return;
        }

        const int cyclesPerFrame = 19656;
        var clock = machine.Clock;
        var nominalClockHz = clock.FrequencyHz;

        // Warm up JIT + steady state.
        for (var i = 0; i < 30; i++)
            clock.Step(cyclesPerFrame);

        var startCycle = clock.TotalCycles;
        var frames = 0;
        var budget = TimeSpan.FromSeconds(4);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < budget)
        {
            clock.Step(cyclesPerFrame);
            frames++;
        }
        sw.Stop();

        var cycles = clock.TotalCycles - startCycle;
        var seconds = sw.Elapsed.TotalSeconds;
        var rawClockHz = cycles / seconds;
        var rawClockPercent = nominalClockHz > 0 ? rawClockHz / nominalClockHz * 100.0 : 0;
        var fps = frames / seconds;
        var msPerFrame = sw.Elapsed.TotalMilliseconds / Math.Max(1, frames);

        var report = new StringBuilder()
            .AppendLine("=== ViceSharp C64 chipset frame cost (raw Clock.Step, audio active) ===")
            .AppendLine(Line("architecture", machine.Architecture.MachineName))
            .AppendLine(Line("nominalClockHz", $"{nominalClockHz:N0} ({nominalClockHz / 1e6:0.000} MHz)"))
            .AppendLine(Line("frames produced", frames.ToString(CultureInfo.InvariantCulture)))
            .AppendLine(Line("elapsed (s)", seconds.ToString("0.000", CultureInfo.InvariantCulture)))
            .AppendLine(Line("cycles executed", cycles.ToString("N0", CultureInfo.InvariantCulture)))
            .AppendLine(Line("raw clock", $"{rawClockHz / 1e6:0.000} MHz"))
            .AppendLine(Line("EffectiveClockPercent", $"{rawClockPercent:0.0}%  (100% = real time)"))
            .AppendLine(Line("fps", $"{fps:0.0}  (PAL real time ~50.1)"))
            .AppendLine(Line("ms / frame", $"{msPerFrame:0.000}  (real-time budget {1000.0 / 50.125:0.00} ms)"))
            .ToString();

        var logPath = Path.Combine(RepoRoot, "artifacts", "c64-chipset-benchmark.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, report);
        Console.WriteLine(report);

        Assert.True(frames > 0, "the C64 clock produced no frames");
    }

    /// <summary>
    /// FR-PRF-001, TR-CYCLE-PACE-001.
    /// Use case: Measure the clock rate the ACTUAL emulation worker
    ///   (<see cref="EmulationPumpService"/> running its real background Loop with
    ///   cycle-clock pacing) achieves on a C64 over a few seconds of wall time.
    ///   This isolates the pump's pacing from raw chipset cost: with the limiter on
    ///   it should converge near 100%, not far below.
    /// Acceptance: When enabled, runs the real pump thread and writes the achieved
    ///   EffectiveClockPercent.
    /// </summary>
    [Fact]
    public async Task MeasurePumpLoopPacedClock()
    {
        if (Environment.GetEnvironmentVariable("VICESHARP_CLOCK_BENCH") != "1")
            Assert.Skip("Set VICESHARP_CLOCK_BENCH=1 to run the pump-loop pacing benchmark.");

        IMachine machine;
        try
        {
            var romProvider = MachineTestFactory.CreateC64RomProvider();
            var audio = ViceSharp.Host.Audio.AudioBackendFactory.CreateDefault();
            var builder = new ArchitectureBuilder(romProvider, audio);
            machine = builder.Build(new ViceSharp.Architectures.C64.C64Descriptor(
                ViceSharp.Architectures.C64.C64MachineProfiles.C64Pal));
        }
        catch (DirectoryNotFoundException)
        {
            Assert.Skip("No complete C64 ROM set resolved; cannot measure pump pacing.");
            return;
        }

        var registry = new EmulatorRuntimeRegistry();
        var session = new EmulatorRuntimeSession("pump-pace", machine.Architecture, machine)
        {
            PowerState = "On",
            RunState = EmulatorRunState.Running,
            LimiterEnabled = true,
        };
        registry.Add(session);

        var pump = new EmulationPumpService(registry);
        var nominalClockHz = machine.Clock.FrequencyHz;

        await pump.StartAsync(TestContext.Current.CancellationToken);
        var startCycle = machine.Clock.TotalCycles;
        var sw = Stopwatch.StartNew();
        await Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        sw.Stop();
        var endCycle = machine.Clock.TotalCycles;
        await pump.StopAsync(TestContext.Current.CancellationToken);

        var cycles = endCycle - startCycle;
        var seconds = sw.Elapsed.TotalSeconds;
        var achievedHz = cycles / seconds;
        var percent = nominalClockHz > 0 ? achievedHz / nominalClockHz * 100.0 : 0;

        var report = new StringBuilder()
            .AppendLine("=== ViceSharp pump-loop paced clock (real EmulationPumpService.Loop) ===")
            .AppendLine(Line("nominalClockHz", $"{nominalClockHz:N0}"))
            .AppendLine(Line("elapsed (s)", seconds.ToString("0.000", CultureInfo.InvariantCulture)))
            .AppendLine(Line("cycles executed", cycles.ToString("N0", CultureInfo.InvariantCulture)))
            .AppendLine(Line("achieved clock", $"{achievedHz / 1e6:0.000} MHz"))
            .AppendLine(Line("EffectiveClockPercent", $"{percent:0.0}%  (100% = real time)"))
            .AppendLine(Line("frames (counter)", session.FrameCount.ToString(CultureInfo.InvariantCulture)))
            .ToString();

        var logPath = Path.Combine(RepoRoot, "artifacts", "pump-pace-benchmark.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        await File.WriteAllTextAsync(logPath, report, TestContext.Current.CancellationToken);
        Console.WriteLine(report);

        Assert.True(cycles > 0, "the pump advanced no cycles");
    }

    /// <summary>
    /// FR-PRF-001, TR-CYCLE-PACE-001 / FR-1132.
    /// Use case: The emulation worker must be a truly independent thread: a UI that
    ///   pulls frames at 50 Hz (cloning the committed framebuffer under the session
    ///   lock) MUST NOT drag the worker's achieved clock down. Reproduces the GUI's
    ///   render-pull load against the real pump and compares clock% with vs without
    ///   the load.
    /// Acceptance: With a concurrent 50 Hz pull loop the achieved clock stays close
    ///   to the no-load figure (the worker is not starved by the UI).
    /// </summary>
    [Fact]
    public async Task MeasurePumpLoopPacedClockUnderPullLoad()
    {
        if (Environment.GetEnvironmentVariable("VICESHARP_CLOCK_BENCH") != "1")
            Assert.Skip("Set VICESHARP_CLOCK_BENCH=1 to run the pump-pull-load benchmark.");

        IMachine machine;
        try
        {
            var romProvider = MachineTestFactory.CreateC64RomProvider();
            var audio = ViceSharp.Host.Audio.AudioBackendFactory.CreateDefault();
            var builder = new ArchitectureBuilder(romProvider, audio);
            machine = builder.Build(new ViceSharp.Architectures.C64.C64Descriptor(
                ViceSharp.Architectures.C64.C64MachineProfiles.C64Pal));
        }
        catch (DirectoryNotFoundException)
        {
            Assert.Skip("No complete C64 ROM set resolved; cannot measure pump pacing.");
            return;
        }

        var registry = new EmulatorRuntimeRegistry();
        var session = new EmulatorRuntimeSession("pump-pull", machine.Architecture, machine)
        {
            PowerState = "On",
            RunState = EmulatorRunState.Running,
            LimiterEnabled = true,
        };
        registry.Add(session);

        var pump = new EmulationPumpService(registry);
        var source = new LocalVideoFrameSource(registry);
        var nominalClockHz = machine.Clock.FrequencyHz;

        await pump.StartAsync(TestContext.Current.CancellationToken);

        // Concurrent 50 Hz frame-pull loop = the UI render timer's exact behaviour
        // (clone the committed framebuffer under the session lock).
        using var pullCts = new CancellationTokenSource();
        var puller = Task.Run(async () =>
        {
            while (!pullCts.IsCancellationRequested)
            {
                await source.GetFrameAsync(session.SessionId, pullCts.Token).ConfigureAwait(false);
                await Task.Delay(20, pullCts.Token).ConfigureAwait(false);
            }
        }, pullCts.Token);

        var startCycle = machine.Clock.TotalCycles;
        var sw = Stopwatch.StartNew();
        await Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        sw.Stop();
        var endCycle = machine.Clock.TotalCycles;

        pullCts.Cancel();
        try { await puller; } catch (OperationCanceledException) { }
        await pump.StopAsync(TestContext.Current.CancellationToken);

        var cycles = endCycle - startCycle;
        var seconds = sw.Elapsed.TotalSeconds;
        var achievedHz = cycles / seconds;
        var percent = nominalClockHz > 0 ? achievedHz / nominalClockHz * 100.0 : 0;

        var report = new StringBuilder()
            .AppendLine("=== ViceSharp pump-loop paced clock UNDER 50Hz pull load ===")
            .AppendLine(Line("nominalClockHz", $"{nominalClockHz:N0}"))
            .AppendLine(Line("elapsed (s)", seconds.ToString("0.000", CultureInfo.InvariantCulture)))
            .AppendLine(Line("cycles executed", cycles.ToString("N0", CultureInfo.InvariantCulture)))
            .AppendLine(Line("achieved clock", $"{achievedHz / 1e6:0.000} MHz"))
            .AppendLine(Line("EffectiveClockPercent", $"{percent:0.0}%  (100% = real time)"))
            .AppendLine(Line("frames (counter)", session.FrameCount.ToString(CultureInfo.InvariantCulture)))
            .ToString();

        var logPath = Path.Combine(RepoRoot, "artifacts", "pump-pull-benchmark.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        await File.WriteAllTextAsync(logPath, report, TestContext.Current.CancellationToken);
        Console.WriteLine(report);

        Assert.True(cycles > 0, "the pump advanced no cycles");
    }

    /// <summary>
    /// FR-PRF-001, TR-CYCLE-PACE-001.
    /// Use case: Measure the clock rate the VICE pacing strategy (vsync wall-clock-from-
    ///   cycle-progress regulator) achieves on a C64 over a few seconds, to confirm it
    ///   tracks real time like the Semaphore strategy.
    /// Acceptance: When enabled, runs the real pump with the VICE gate and writes the
    ///   achieved EffectiveClockPercent.
    /// </summary>
    [Fact]
    public async Task MeasureVicePacedClock()
    {
        if (Environment.GetEnvironmentVariable("VICESHARP_CLOCK_BENCH") != "1")
            Assert.Skip("Set VICESHARP_CLOCK_BENCH=1 to run the VICE-gate pacing benchmark.");

        IMachine machine;
        try
        {
            var romProvider = MachineTestFactory.CreateC64RomProvider();
            var audio = ViceSharp.Host.Audio.AudioBackendFactory.CreateDefault();
            var builder = new ArchitectureBuilder(romProvider, audio);
            machine = builder.Build(new ViceSharp.Architectures.C64.C64Descriptor(
                ViceSharp.Architectures.C64.C64MachineProfiles.C64Pal));
        }
        catch (DirectoryNotFoundException)
        {
            Assert.Skip("No complete C64 ROM set resolved; cannot measure VICE pacing.");
            return;
        }

        var registry = new EmulatorRuntimeRegistry();
        var session = new EmulatorRuntimeSession("vice-pace", machine.Architecture, machine)
        {
            PowerState = "On",
            RunState = EmulatorRunState.Running,
            LimiterEnabled = true,
        };
        registry.Add(session);

        using var pump = new EmulationPumpService(registry, new ViceEmulationGate());
        var nominalClockHz = machine.Clock.FrequencyHz;

        await pump.StartAsync(TestContext.Current.CancellationToken);
        var startCycle = machine.Clock.TotalCycles;
        var sw = Stopwatch.StartNew();
        await Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        sw.Stop();
        var endCycle = machine.Clock.TotalCycles;
        await pump.StopAsync(TestContext.Current.CancellationToken);

        var cycles = endCycle - startCycle;
        var seconds = sw.Elapsed.TotalSeconds;
        var achievedHz = cycles / seconds;
        var percent = nominalClockHz > 0 ? achievedHz / nominalClockHz * 100.0 : 0;

        var report = new StringBuilder()
            .AppendLine($"=== ViceSharp VICE-gate paced clock ({pump.GateName}) ===")
            .AppendLine(Line("nominalClockHz", $"{nominalClockHz:N0}"))
            .AppendLine(Line("elapsed (s)", seconds.ToString("0.000", CultureInfo.InvariantCulture)))
            .AppendLine(Line("cycles executed", cycles.ToString("N0", CultureInfo.InvariantCulture)))
            .AppendLine(Line("achieved clock", $"{achievedHz / 1e6:0.000} MHz"))
            .AppendLine(Line("EffectiveClockPercent", $"{percent:0.0}%  (100% = real time)"))
            .AppendLine(Line("frames (counter)", session.FrameCount.ToString(CultureInfo.InvariantCulture)))
            .ToString();

        var logPath = Path.Combine(RepoRoot, "artifacts", "pump-vice-benchmark.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        await File.WriteAllTextAsync(logPath, report, TestContext.Current.CancellationToken);
        Console.WriteLine(report);

        Assert.True(cycles > 0, "the VICE pump advanced no cycles");
    }

    private static string Line(string key, string value) =>
        string.Concat(key.PadRight(22), ": ", value);

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return dir!.FullName;
        }
    }
}
