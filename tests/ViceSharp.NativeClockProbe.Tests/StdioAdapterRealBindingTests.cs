using System.Diagnostics;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Phase C step 2: the 19 StdioAdapterFixtures cases bound to the real, unmodified adapter
/// (tools/immutable-vice/gate-observer.c and clock-gate.c), compiled with a force-included recording seam
/// (Fixtures/stdio-probe-io.h) whose results the fixture scripts. Same inputs, literal expectations and
/// assertion as StdioAdapterContractTests; nothing is weakened. FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001
/// AC2/AC3/AC5; TEST-VICE-001 AC2/AC4. See STDIO-ADAPTER-SLICE.md.
/// </summary>
public sealed class StdioAdapterRealBindingTests
{
    static readonly CompiledCFixture Scripted = new("stdio-adapter-scripted",
        ["stdio-probe-driver.c", "gate-observer.c", "clock-gate.c"],
        "-include", Path.Combine(AppContext.BaseDirectory, "Fixtures", "stdio-probe-io.h"));

    /// <summary>Runs the real adapter once per input and parses the recorded trace.</summary>
    sealed class ScriptedProbe : StdioAdapterFixtures.IStdioAdapterProbe
    {
        public StdioAdapterFixtures.AdapterObservation Run(StdioAdapterFixtures.AdapterInput input)
        {
            var (exit, output, error) = Scripted.Run([input.Clock.ToString(), input.PublishResult.ToString(),
                input.FlushResult.ToString(), input.ReadResult.ToString(), input.ErrorFlag.ToString(),
                input.StopWriteResult.ToString(), input.StopFlushResult.ToString()]);
            Assert.True(exit == 0, $"driver exit {exit}: {error}");
            var events = new List<StdioAdapterFixtures.StdioEvent>();
            bool? returned = null;
            int? status = null;
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')))
            {
                var parts = line.Split(' ');
                if (parts[0] == "EV")
                    events.Add(new(parts[1], parts[2], parts[3] == "-" ? "" :
                        new string(Convert.FromHexString(parts[3]).Select(b => (char)b).ToArray())));
                else if (parts[0] == "END" && parts[1] == "returned") returned = true;
                else if (parts[0] == "END" && parts[1] == "exit") { returned = false; status = int.Parse(parts[2]); }
                else throw new InvalidOperationException($"unexpected driver line: {line}");
            }
            Assert.NotNull(returned);
            return new(returned.Value, status, [.. events]);
        }
    }

    [Theory]
    [MemberData(nameof(StdioAdapterFixtures.Cases), MemberType = typeof(StdioAdapterFixtures))]
    public void Real_adapter_matches_the_mock_contract_case(string name) =>
        StdioAdapterFixtures.AssertAdapter(new ScriptedProbe(), name);
}

/// <summary>
/// Phase C step 2 real-stdio contract: the unmodified adapter on real pipes (Fixtures/stdio-real-driver.c).
/// Expected stderr bytes and exit statuses come from the gate protocol in STDIO-ADAPTER-SLICE.md; the C runtime's
/// text-mode line ending is part of the expectation (CRLF under the Windows MSVCRT, LF elsewhere), never normalized.
/// Native stdout after a gate stop is measured, not asserted (C11 7.22.4.5 leaves flushing on _Exit
/// implementation-defined); the measurement is written to the evidence folder.
/// </summary>
public static class RealStdioFixtures
{
    public sealed record RealCase(string Name, bool Wait, string Input, bool CloseStderr);
    /// <summary>
    /// Stderr null means the test closed the pipe and observes only the exit status. ExitCodes lists every status
    /// the protocol allows for the case; ForbiddenStdout, when set, must not appear (it proves no return to native).
    /// </summary>
    public sealed record RealExpected(int[] ExitCodes, string? Stderr, string? Stdout, string? ForbiddenStdout = null);
    public sealed record RealObservation(int ExitCode, string? Stderr, string Stdout);
    public interface IRealStdioProbe { RealObservation Run(RealCase c); }

    public static string Nl => OperatingSystem.IsWindows() ? "\r\n" : "\n";

    public static IEnumerable<object[]> Cases => new[]
    {
        "continue-both", "continue-then-quit", "eof-first", "uppercase-first", "broken-stderr"
    }.Select(n => new object[] { n });

    public static (RealCase Case, RealExpected Expected) Get(string name) => name switch
    {
        "continue-both" => (new(name, false, "cc", false), new([0], $"VICE_GATE,7{Nl}VICE_GATE,8{Nl}",
            $"NATIVE_BEFORE{Nl}NATIVE_AFTER,7{Nl}NATIVE_AFTER,8{Nl}")),
        "continue-then-quit" => (new(name, false, "cq", false), new([67], $"VICE_GATE,7{Nl}VICE_GATE,8{Nl}VICE_GATE_STOP,67{Nl}", null)),
        "eof-first" => (new(name, false, "", false), new([66], $"VICE_GATE,7{Nl}VICE_GATE_STOP,66{Nl}", null)),
        "uppercase-first" => (new(name, false, "C", false), new([68], $"VICE_GATE,7{Nl}VICE_GATE_STOP,68{Nl}", null)),
        // Publication to a closed stderr must fail closed: the protocol stops with 64 when fprintf reports the
        // failure and 65 when the C runtime buffers it and fflush reports it (observed 65 under the Windows MSVCRT,
        // 2026-10-19). Either way execution never returns to native, so no NATIVE_AFTER line may appear.
        "broken-stderr" => (new(name, true, "gc", true), new([64, 65], null, null, "NATIVE_AFTER")),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    /// <summary>Shared assertion for mocks and the real process. Stdout is asserted only where Expected.Stdout is set.</summary>
    public static void AssertReal(IRealStdioProbe probe, string name)
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

/// <summary>Mocks first: the real-stdio assertion discriminates before any process runs.</summary>
public sealed class RealStdioContractTests
{
    [Theory]
    [MemberData(nameof(RealStdioFixtures.Cases), MemberType = typeof(RealStdioFixtures))]
    public void Correct_mock_passes_and_wrong_observations_are_rejected(string name)
    {
        var (c, e) = RealStdioFixtures.Get(name);
        var probe = Substitute.For<RealStdioFixtures.IRealStdioProbe>();
        foreach (var code in e.ExitCodes)
        {
            var ok = new RealStdioFixtures.RealObservation(code, e.Stderr, e.Stdout ?? "measured");
            probe.Run(c).Returns(ok);
            RealStdioFixtures.AssertReal(probe, name);
        }
        var good = new RealStdioFixtures.RealObservation(e.ExitCodes[0], e.Stderr, e.Stdout ?? "measured");

        var wrongs = new List<RealStdioFixtures.RealObservation?>
        {
            null, good with { ExitCode = 0 }, good with { ExitCode = 71 }, good with { Stderr = (good.Stderr ?? "") + "x" },
        };
        if (name == "continue-both") wrongs.Remove(wrongs[1]);
        if (e.ForbiddenStdout is not null) wrongs.Add(good with { Stdout = $"NATIVE_BEFORE{RealStdioFixtures.Nl}NATIVE_AFTER,7{RealStdioFixtures.Nl}" });
        if (e.Stderr is not null)
        {
            wrongs.Add(good with { Stderr = null });
            wrongs.Add(good with { Stderr = e.Stderr.Replace("\r\n", "\n").Replace("\n", "\r\n\r\n") });
            wrongs.Add(good with { Stderr = e.Stderr[..e.Stderr.LastIndexOf("VICE_GATE", StringComparison.Ordinal)] });
        }
        if (e.Stdout is not null) wrongs.Add(good with { Stdout = e.Stdout + "NATIVE_AFTER,9" + RealStdioFixtures.Nl });
        foreach (var wrong in wrongs)
        {
            probe.Run(c).Returns(wrong!);
            Assert.ThrowsAny<XunitException>(() => RealStdioFixtures.AssertReal(probe, name));
        }
    }
}

/// <summary>The same real-stdio cases against the real process.</summary>
public sealed class StdioAdapterRealProcessTests
{
    static readonly CompiledCFixture Real = new("stdio-adapter-real", ["stdio-real-driver.c", "gate-observer.c", "clock-gate.c"]);

    sealed class ProcessProbe : RealStdioFixtures.IRealStdioProbe
    {
        public RealStdioFixtures.RealObservation Run(RealStdioFixtures.RealCase c)
        {
            var start = new ProcessStartInfo(Real.ExecutablePath)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Real.EvidenceDirectory
            };
            if (c.Wait) start.ArgumentList.Add("wait");
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
            var observed = new RealStdioFixtures.RealObservation(process.ExitCode,
                errBytes is null ? null : new string(errBytes.Select(b => (char)b).ToArray()),
                new string(outBytes.Select(b => (char)b).ToArray()));
            File.AppendAllText(Path.Combine(Real.EvidenceDirectory, "real-stdio-observations.log"),
                $"{DateTimeOffset.Now:O} case={c.Name} exit={observed.ExitCode} stderrHex={(errBytes is null ? "closed" : Convert.ToHexString(errBytes))} stdoutHex={Convert.ToHexString(outBytes)}\n");
            return observed;
        }
    }

    [Theory]
    [MemberData(nameof(RealStdioFixtures.Cases), MemberType = typeof(RealStdioFixtures))]
    public void Real_adapter_on_real_pipes_matches_the_contract(string name) =>
        RealStdioFixtures.AssertReal(new ProcessProbe(), name);
}

static class StreamCopy
{
    public static async Task<byte[]> CopyToMemoryAsync(this Stream s)
    {
        using var m = new MemoryStream();
        await s.CopyToAsync(m);
        return m.ToArray();
    }
}
