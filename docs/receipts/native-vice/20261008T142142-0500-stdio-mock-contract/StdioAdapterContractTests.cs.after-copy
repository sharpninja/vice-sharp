using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Shared, independent fixtures for the diagnostic stdio boundary, not native stdout retention.
/// FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001 AC2/AC3/AC5; TEST-VICE-001 AC2/AC4.
/// See STDIO-ADAPTER-SLICE.md for the mock-first order and mandatory real-binding gate.
/// </summary>
public static class StdioAdapterFixtures
{
    public sealed record AdapterInput(ulong Clock = 7, int PublishResult = 12,
        int FlushResult = 0, int ReadResult = 99, int ErrorFlag = 0,
        int StopWriteResult = 18, int StopFlushResult = 0);

    /// <summary>Observed gate protocol interaction; not an emulator bus event.</summary>
    public sealed record StdioEvent(string Operation, string Stream, string Value = "");
    public sealed record AdapterObservation(bool ReturnedToNative, int? ExitStatus, StdioEvent[] Events);
    public sealed record AdapterFixture(AdapterInput Input, AdapterObservation Expected);
    public interface IStdioAdapterProbe { AdapterObservation Run(AdapterInput input); }

    public static IEnumerable<object[]> Cases => new[]
    {
        "continue-zero-write", "continue-positive-write", "maximum-clock", "unused-error-flag",
        "publish-negative", "publish-other-negative", "flush-negative", "flush-positive",
        "eof", "input-error-positive", "input-error-negative", "quit", "uppercase", "newline",
        "nul", "high-byte", "stop-write-failure", "stop-flush-failure", "stop-both-failures"
    }.Select(name => new object[] { name });

    /// <summary>Literal expectations derived from the approved gate protocol, never provider output.</summary>
    public static AdapterFixture GetFixture(string name) => name switch
    {
        "continue-zero-write" => new(new(Clock: 0, PublishResult: 0), new(true, null,
            [new("fprintf", "stderr", "VICE_GATE,0\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("return", "native")])),
        "continue-positive-write" => new(new(PublishResult: 123), new(true, null,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("return", "native")])),
        "maximum-clock" => new(new(Clock: ulong.MaxValue), new(true, null,
            [new("fprintf", "stderr", "VICE_GATE,18446744073709551615\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("return", "native")])),
        "unused-error-flag" => new(new(ErrorFlag: 1), new(true, null,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("return", "native")])),
        "publish-negative" => new(new(PublishResult: -1), new(false, 64,
            [new("fprintf", "stderr", "VICE_GATE,7\n"),
             new("fprintf", "stderr", "VICE_GATE_STOP,64\n"), new("fflush", "stderr"),
             new("_Exit", "process", "64")])),
        "publish-other-negative" => new(new(PublishResult: -7), new(false, 64,
            [new("fprintf", "stderr", "VICE_GATE,7\n"),
             new("fprintf", "stderr", "VICE_GATE_STOP,64\n"), new("fflush", "stderr"),
             new("_Exit", "process", "64")])),
        "flush-negative" => new(new(FlushResult: -1), new(false, 65,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fprintf", "stderr", "VICE_GATE_STOP,65\n"), new("fflush", "stderr"),
             new("_Exit", "process", "65")])),
        "flush-positive" => new(new(FlushResult: 1), new(false, 65,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fprintf", "stderr", "VICE_GATE_STOP,65\n"), new("fflush", "stderr"),
             new("_Exit", "process", "65")])),
        "eof" => new(new(ReadResult: -1), new(false, 66,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("ferror", "stdin"),
             new("fprintf", "stderr", "VICE_GATE_STOP,66\n"), new("fflush", "stderr"),
             new("_Exit", "process", "66")])),
        "input-error-positive" => new(new(ReadResult: -1, ErrorFlag: 1), new(false, 70,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("ferror", "stdin"),
             new("fprintf", "stderr", "VICE_GATE_STOP,70\n"), new("fflush", "stderr"),
             new("_Exit", "process", "70")])),
        "input-error-negative" => new(new(ReadResult: -1, ErrorFlag: -7), new(false, 70,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("ferror", "stdin"),
             new("fprintf", "stderr", "VICE_GATE_STOP,70\n"), new("fflush", "stderr"),
             new("_Exit", "process", "70")])),
        "quit" => new(new(ReadResult: 113), new(false, 67,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("fprintf", "stderr", "VICE_GATE_STOP,67\n"),
             new("fflush", "stderr"), new("_Exit", "process", "67")])),
        "uppercase" => new(new(ReadResult: 67), new(false, 68,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("fprintf", "stderr", "VICE_GATE_STOP,68\n"),
             new("fflush", "stderr"), new("_Exit", "process", "68")])),
        "newline" => new(new(ReadResult: 10), new(false, 68,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("fprintf", "stderr", "VICE_GATE_STOP,68\n"),
             new("fflush", "stderr"), new("_Exit", "process", "68")])),
        "nul" => new(new(ReadResult: 0), new(false, 68,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("fprintf", "stderr", "VICE_GATE_STOP,68\n"),
             new("fflush", "stderr"), new("_Exit", "process", "68")])),
        "high-byte" => new(new(ReadResult: 255), new(false, 68,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("fprintf", "stderr", "VICE_GATE_STOP,68\n"),
             new("fflush", "stderr"), new("_Exit", "process", "68")])),
        "stop-write-failure" => new(new(ReadResult: -1, StopWriteResult: -1), new(false, 66,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("ferror", "stdin"),
             new("fprintf", "stderr", "VICE_GATE_STOP,66\n"), new("fflush", "stderr"),
             new("_Exit", "process", "66")])),
        "stop-flush-failure" => new(new(ReadResult: 113, StopFlushResult: 1), new(false, 67,
            [new("fprintf", "stderr", "VICE_GATE,7\n"), new("fflush", "stderr"),
             new("fgetc", "stdin"), new("fprintf", "stderr", "VICE_GATE_STOP,67\n"),
             new("fflush", "stderr"), new("_Exit", "process", "67")])),
        "stop-both-failures" => new(new(PublishResult: -1, StopWriteResult: -2, StopFlushResult: -1),
            new(false, 64,
            [new("fprintf", "stderr", "VICE_GATE,7\n"),
             new("fprintf", "stderr", "VICE_GATE_STOP,64\n"), new("fflush", "stderr"),
             new("_Exit", "process", "64")])),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    /// <summary>Reuse without weakening when binding these same fixtures to the real adapter.</summary>
    public static void AssertAdapter(IStdioAdapterProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var actual = probe.Run(fixture.Input);
        Assert.NotNull(actual);
        Assert.Equal(fixture.Expected.ReturnedToNative, actual.ReturnedToNative);
        Assert.Equal(fixture.Expected.ExitStatus, actual.ExitStatus);
        Assert.NotNull(actual.Events);
        Assert.Equal(fixture.Expected.Events, actual.Events);
    }
}

/// <summary>Mocks prove all assertions discriminate. These tests do not execute the C adapter.</summary>
public sealed class StdioAdapterContractTests
{
    [Theory]
    [MemberData(nameof(StdioAdapterFixtures.Cases), MemberType = typeof(StdioAdapterFixtures))]
    public void Correct_mock_passes_and_wrong_stdio_observations_are_rejected(string name)
    {
        var fixture = StdioAdapterFixtures.GetFixture(name);
        var expected = fixture.Expected;
        var correct = Substitute.For<StdioAdapterFixtures.IStdioAdapterProbe>();
        correct.Run(fixture.Input).Returns(expected);
        StdioAdapterFixtures.AssertAdapter(correct, name);
        correct.Received(1).Run(fixture.Input);

        var rejected = 0;
        void Reject(StdioAdapterFixtures.AdapterObservation observation)
        {
            var wrong = Substitute.For<StdioAdapterFixtures.IStdioAdapterProbe>();
            wrong.Run(fixture.Input).Returns(observation);
            Assert.ThrowsAny<XunitException>(() => StdioAdapterFixtures.AssertAdapter(wrong, name));
            wrong.Received(1).Run(fixture.Input);
            rejected++;
        }

        Reject(null!);
        Reject(expected with { ReturnedToNative = !expected.ReturnedToNative });
        Reject(expected with { ExitStatus = expected.ExitStatus is int status ? status ^ 1 : 71 });
        Reject(expected with { Events = null! });
        Reject(expected with { Events = [.. expected.Events, new("unexpected", "unexpected")] });
        for (var i = 0; i < expected.Events.Length; i++)
        {
            var changed = expected.Events.ToArray();
            changed[i] = changed[i] with { Operation = "wrong-operation" };
            Reject(expected with { Events = changed });
            changed = expected.Events.ToArray();
            changed[i] = changed[i] with { Stream = "wrong-stream" };
            Reject(expected with { Events = changed });
            changed = expected.Events.ToArray();
            changed[i] = changed[i] with { Value = changed[i].Value + "!" };
            Reject(expected with { Events = changed });
            Reject(expected with { Events = expected.Events.Where((_, index) => index != i).ToArray() });
            Reject(expected with { Events = [.. expected.Events.Take(i), expected.Events[i], .. expected.Events.Skip(i)] });
            if (i + 1 < expected.Events.Length)
            {
                changed = expected.Events.ToArray();
                (changed[i], changed[i + 1]) = (changed[i + 1], changed[i]);
                Reject(expected with { Events = changed });
            }
        }
        Assert.Equal(6 * expected.Events.Length + 4, rejected);
    }
}
