namespace ViceSharp.TestHarness;

using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

/// <summary>
/// TEST-CPUTICK-001 (AC3 display) / FR-CPUTICK-001. The per-CPU rate roster must survive the
/// gRPC status boundary so a remote/GUI client can list each CPU distinctly: the managed
/// EmulatorStatusDto.PerCpuRates maps onto the wire's repeated per_cpu_rates field.
/// </summary>
public sealed class PerCpuRateMappingTests
{
    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC3 display).
    /// Use case: the host serves status to the GUI over gRPC; the per-CPU rates (host + each
    ///   drive) must cross the wire so the status bar can render a row per CPU.
    /// Acceptance: GrpcHostMapping.Map copies every PerCpuRateDto (label, Hz, percent)
    ///   into the gRPC message's repeated per_cpu_rates field, in order.
    /// </summary>
    [Fact]
    public void Map_PreservesPerCpuRatesOntoTheWire()
    {
        var dto = new EmulatorStatusDto(
            "session-1",
            "Commodore 64",
            EmulatorRunState.Running,
            0,
            new MachineStateDto(0, 0, 0, 0, 0, 0, 0))
        {
            PerCpuRates = new[]
            {
                new PerCpuRateDto("Commodore 64", 970_000d, 98.5d),
                new PerCpuRateDto("C1541", 1_000_000d, 100.0d),
            },
        };

        var grpc = GrpcHostMapping.Map(dto);

        Assert.NotNull(grpc);
        Assert.Equal(2, grpc!.PerCpuRates.Count);
        Assert.Equal("Commodore 64", grpc.PerCpuRates[0].Label);
        Assert.Equal(970_000d, grpc.PerCpuRates[0].EffectiveClockHz);
        Assert.Equal(98.5d, grpc.PerCpuRates[0].EffectiveClockPercent);
        Assert.Equal("C1541", grpc.PerCpuRates[1].Label);
        Assert.Equal(100.0d, grpc.PerCpuRates[1].EffectiveClockPercent);
    }

    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC3 display).
    /// Use case: a single-CPU C64 (no drive) must map cleanly with an empty roster, not throw.
    /// Acceptance: a status DTO with no PerCpuRates maps to a gRPC message with an empty
    ///   per_cpu_rates collection.
    /// </summary>
    [Fact]
    public void Map_EmptyPerCpuRates_ProducesEmptyRepeatedField()
    {
        var dto = new EmulatorStatusDto(
            "session-2",
            "Commodore 64",
            EmulatorRunState.Running,
            0,
            new MachineStateDto(0, 0, 0, 0, 0, 0, 0));

        var grpc = GrpcHostMapping.Map(dto);

        Assert.NotNull(grpc);
        Assert.Empty(grpc!.PerCpuRates);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: the IEC monitor panel reads live line states from the host status over gRPC, so
    ///   each line's signal, level, and pullers must survive the wire mapping.
    /// Acceptance: GrpcHostMapping.Map copies every IecBusLineDto into the gRPC message's repeated
    ///   iec_bus_lines field, preserving signal, level, and pullers.
    /// </summary>
    [Fact]
    public void Map_PreservesIecBusLinesOntoTheWire()
    {
        var dto = new EmulatorStatusDto(
            "session-3",
            "Commodore 64",
            EmulatorRunState.Running,
            0,
            new MachineStateDto(0, 0, 0, 0, 0, 0, 0))
        {
            IecBusLines = new[]
            {
                new IecBusLineDto("ATN", IsHigh: false, Pullers: "c64"),
                new IecBusLineDto("DATA", IsHigh: true, Pullers: ""),
            },
        };

        var grpc = GrpcHostMapping.Map(dto);

        Assert.NotNull(grpc);
        Assert.Equal(2, grpc!.IecBusLines.Count);
        Assert.Equal("ATN", grpc.IecBusLines[0].Signal);
        Assert.False(grpc.IecBusLines[0].IsHigh);
        Assert.Equal("c64", grpc.IecBusLines[0].Pullers);
        Assert.True(grpc.IecBusLines[1].IsHigh);
    }
}
