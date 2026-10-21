using System.Diagnostics;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// PLAN-OBSERVER-001 slice S2a: the bus adapter that carries the observer's events to the host ahead of the
/// gate record for the same cycle, without modifying the retained gate adapter (gate-observer.c bytes preserved):
/// tools/immutable-vice/bus-adapter.c wraps vice_sharp_observe_clock (a cross-object call from clock-observer.c),
/// runs the observer's per-cycle hook first, then the real gate; its sink writes "VICE_BUS,&lt;kind&gt;,&lt;detail&gt;\n"
/// to stderr, the stream the gate already publishes on and flushes before reading stdin. A sink write failure stops
/// with status 72 (VICE_BUS_STOP,72) without returning to native code. FR-VICE-001 AC2/AC4, TR-VICE-ORACLE-001
/// AC2/AC3/AC5, TEST-VICE-001 AC2/AC4. Output-channel choice is plan item 6 (operator-confirm item 3).
/// </summary>
public static class BusAdapterFixtures
{
    public sealed record AdapterCase(string Name, bool Wait, string Input, bool CloseStderr);
    public sealed record AdapterExpected(int[] ExitCodes, string? Stderr, string? Stdout, string? ForbiddenStdout = null);
    public sealed record AdapterObservation(int ExitCode, string? Stderr, string Stdout);
    public interface IBusAdapterProbe { AdapterObservation Run(AdapterCase c); }

    public static string Nl => RealStdioFixtures.Nl;

    // S4b: with VICESHARP_BUS_RAW=1 the raw access line stays; the adapter then composes the two VICE_OBS
    // lines of the cycle (the test glue obs-fields-double.c reports x64sc rules, BA high, mask 0, no PHI1
    // value, not stolen) and only then runs the real gate adapter, so every line of a cycle precedes its gate record.
    private static string Cycle(int clock) =>
        $"VICE_BUS,access,read,4660,145,{clock}{Nl}VICE_BUS,cycle,{clock}{Nl}" +
        $"VICE_OBS,1,{(clock - 7) * 2},PHI1,0,1,idle,0,,0,0,{clock}{Nl}VICE_OBS,1,{(clock - 7) * 2 + 1},PHI2,1,1,read,4660,145,0,0,{clock}{Nl}" +
        $"VICE_GATE,{clock}{Nl}";
    private static string Init => $"VICE_BUS,repatch,514,1{Nl}";

    public static IEnumerable<object[]> Cases => new[]
    {
        "continue-both", "continue-then-quit", "eof-first", "broken-stderr"
    }.Select(n => new object[] { n });

    public static (AdapterCase Case, AdapterExpected Expected) Get(string name) => name switch
    {
        "continue-both" => (new(name, false, "cc", false),
            new([0], Init + Cycle(7) + Cycle(8), $"init,0{Nl}NATIVE_AFTER,7{Nl}NATIVE_AFTER,8{Nl}")),
        "continue-then-quit" => (new(name, false, "cq", false),
            new([67], Init + Cycle(7) + Cycle(8) + $"VICE_GATE_STOP,67{Nl}", null, "NATIVE_AFTER,8")),
        "eof-first" => (new(name, false, "", false),
            new([66], Init + Cycle(7) + $"VICE_GATE_STOP,66{Nl}", null, "NATIVE_AFTER")),
        // The first sink write (repatch after the rebuild) hits the closed pipe: stop 72, nothing returns to native.
        "broken-stderr" => (new(name, true, "cc", true), new([72], null, null, "NATIVE_AFTER")),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    public static void AssertAdapter(IBusAdapterProbe probe, string name)
    {
        var (c, e) = Get(name);
        var actual = probe.Run(c);
        Assert.NotNull(actual);
        Assert.Contains(actual.ExitCode, e.ExitCodes);
        Assert.Equal(e.Stderr, actual.Stderr);
        if (e.Stdout is not null) Assert.Equal(e.Stdout, actual.Stdout);
        if (e.ForbiddenStdout is not null) Assert.DoesNotContain(e.ForbiddenStdout, actual.Stdout, StringComparison.Ordinal);
    }
}

/// <summary>Mocks first: the adapter assertion discriminates before any process runs.</summary>
public sealed class BusAdapterContractTests
{
    [Theory]
    [MemberData(nameof(BusAdapterFixtures.Cases), MemberType = typeof(BusAdapterFixtures))]
    public void Correct_mock_passes_and_wrong_observations_are_rejected(string name)
    {
        var (c, e) = BusAdapterFixtures.Get(name);
        var probe = Substitute.For<BusAdapterFixtures.IBusAdapterProbe>();
        var good = new BusAdapterFixtures.AdapterObservation(e.ExitCodes[0], e.Stderr, e.Stdout ?? "measured");
        probe.Run(c).Returns(good);
        BusAdapterFixtures.AssertAdapter(probe, name);

        var nl = BusAdapterFixtures.Nl;
        var wrongs = new List<BusAdapterFixtures.AdapterObservation?>
        {
            null, good with { ExitCode = 71 }, good with { Stderr = (good.Stderr ?? "") + "x" },
        };
        if (name != "continue-both") wrongs.Add(good with { ExitCode = 0 });
        if (e.ForbiddenStdout is not null) wrongs.Add(good with { Stdout = $"init,0{nl}{e.ForbiddenStdout}{nl}" });
        if (e.Stderr is not null)
        {
            wrongs.Add(good with { Stderr = null });
            // Bus events after the gate record of their cycle are wrong: the host reads the record before permitting the next cycle.
            wrongs.Add(good with { Stderr = e.Stderr
                .Replace($"VICE_OBS,1,1,PHI2,1,1,read,4660,145,0,0,7{nl}VICE_GATE,7{nl}", $"VICE_OBS,1,1,PHI2,1,1,read,4660,145,0,0,7{nl}")
                .Replace($"VICE_BUS,access,read,4660,145,7{nl}VICE_BUS,cycle,7{nl}", $"VICE_GATE,7{nl}VICE_BUS,access,read,4660,145,7{nl}VICE_BUS,cycle,7{nl}") });
            // The VICE_OBS pair must precede the gate record and carry the cycle's access; dropped or guessed is wrong.
            wrongs.Add(good with { Stderr = e.Stderr.Replace($"VICE_OBS,1,1,PHI2,1,1,read,4660,145,0,0,7{nl}VICE_GATE,7{nl}", $"VICE_GATE,7{nl}VICE_OBS,1,1,PHI2,1,1,read,4660,145,0,0,7{nl}") });
            wrongs.Add(good with { Stderr = e.Stderr.Replace($"VICE_OBS,1,0,PHI1,0,1,idle,0,,0,0,7{nl}", "") });
            wrongs.Add(good with { Stderr = e.Stderr.Replace($"VICE_OBS,1,0,PHI1,0,1,idle,0,,0,0,7{nl}", $"VICE_OBS,1,0,PHI1,0,1,idle,0,0,0,0,7{nl}") });
            // Dropped access, dropped cycle, dropped repatch.
            wrongs.Add(good with { Stderr = e.Stderr.Replace($"VICE_BUS,access,read,4660,145,7{nl}", "") });
            wrongs.Add(good with { Stderr = e.Stderr.Replace($"VICE_BUS,cycle,7{nl}", "") });
            wrongs.Add(good with { Stderr = e.Stderr.Replace($"VICE_BUS,repatch,514,1{nl}", "") });
            // CRLF where the platform writes LF (or doubled line ends) is a framing fault.
            wrongs.Add(good with { Stderr = e.Stderr.Replace("\r\n", "\n").Replace("\n", "\r\n\r\n") });
        }
        if (e.Stdout is not null) wrongs.Add(good with { Stdout = e.Stdout + "NATIVE_AFTER,9" + nl });
        foreach (var wrong in wrongs)
        {
            probe.Run(c).Returns(wrong!);
            Assert.ThrowsAny<XunitException>(() => BusAdapterFixtures.AssertAdapter(probe, name));
        }
    }
}

/// <summary>The same cases against the real process: observer, adapter, unmodified gate adapter and gate on real pipes.</summary>
public sealed class BusAdapterRealProcessTests
{
    private static readonly CompiledCFixture Real = new("bus-adapter-real",
        ["bus-real-driver.c", "native-bus-double.c", "clock-observer.c", "bus-observer.c", "bus-adapter.c", "obs-emitter.c", "obs-fields-double.c", "gate-observer.c", "clock-gate.c", "snapshot-checkpoint-stub.c"],
        "-Wl,--wrap=vic_cycle", "-Wl,--wrap=mem_initialize_memory", "-Wl,--wrap=mem_mmu_translate",
        "-Wl,--wrap=mem_toggle_watchpoints", "-Wl,--wrap=vice_sharp_observe_clock");

    private sealed class ProcessProbe : BusAdapterFixtures.IBusAdapterProbe
    {
        public BusAdapterFixtures.AdapterObservation Run(BusAdapterFixtures.AdapterCase c)
        {
            var start = new ProcessStartInfo(Real.ExecutablePath)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Real.EvidenceDirectory
            };
            if (c.Wait) start.ArgumentList.Add("wait");
            start.Environment["VICESHARP_BUS_RAW"] = "1";
            using var process = Process.Start(start) ?? throw new InvalidOperationException("driver did not start");
            var stdout = process.StandardOutput.BaseStream.CopyToMemoryAsync();
            Task<byte[]>? stderr = null;
            if (c.CloseStderr) process.StandardError.Close(); else stderr = process.StandardError.BaseStream.CopyToMemoryAsync();
            var input = process.StandardInput.BaseStream;
            input.Write(c.Input.Select(ch => (byte)ch).ToArray());
            input.Flush();
            process.StandardInput.Close();
            if (!process.WaitForExit(30_000)) { process.Kill(true); throw new TimeoutException($"{c.Name} timed out; not a passing gate"); }
            var outBytes = stdout.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            var errBytes = stderr?.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            var observed = new BusAdapterFixtures.AdapterObservation(process.ExitCode,
                errBytes is null ? null : new string(errBytes.Select(b => (char)b).ToArray()),
                new string(outBytes.Select(b => (char)b).ToArray()));
            File.AppendAllText(Path.Combine(Real.EvidenceDirectory, "bus-adapter-observations.log"),
                $"{DateTimeOffset.Now:O} case={c.Name} exit={observed.ExitCode} stderrHex={(errBytes is null ? "closed" : Convert.ToHexString(errBytes))} stdoutHex={Convert.ToHexString(outBytes)}\n");
            return observed;
        }
    }

    [Theory]
    [MemberData(nameof(BusAdapterFixtures.Cases), MemberType = typeof(BusAdapterFixtures))]
    public void Real_adapter_on_real_pipes_matches_the_contract(string name) =>
        BusAdapterFixtures.AssertAdapter(new ProcessProbe(), name);
}
