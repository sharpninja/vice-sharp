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
public sealed class ClockThroughputBenchmarkTests
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
