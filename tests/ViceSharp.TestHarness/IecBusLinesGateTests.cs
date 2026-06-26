namespace ViceSharp.TestHarness;

using System;
using ViceSharp.Abstractions;
using ViceSharp.Host.Services;
using Xunit;

/// <summary>
/// FR-IECMON-001. The IEC monitor only surfaces line state when a peripheral actually shares the
/// bus. A single-system C64 has just the host endpoint, so the panel must hide rather than show
/// idle ghost ATN/CLK/DATA/SRQ lines.
/// </summary>
public sealed class IecBusLinesGateTests
{
    private static BusSnapshot Snapshot(params string[] endpoints)
    {
        var lines = new[]
        {
            new BusLineSnapshot("ATN", true, Array.Empty<string>()),
            new BusLineSnapshot("CLK", true, Array.Empty<string>()),
            new BusLineSnapshot("DATA", true, Array.Empty<string>()),
            new BusLineSnapshot("SRQ", true, Array.Empty<string>()),
        };
        return new BusSnapshot("IEC", lines, endpoints);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: a bare C64 (only the host CIA2 on the bus) must not show the IEC panel.
    /// Acceptance: a snapshot with fewer than two endpoints maps to no lines.
    /// </summary>
    [Fact]
    public void SingleEndpoint_YieldsNoLines_PanelHides()
    {
        Assert.Empty(HostProtocolMapper.BuildIecBusLines(Snapshot("c64")));
        Assert.Empty(HostProtocolMapper.BuildIecBusLines(Snapshot()));
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: once a drive shares the bus, the monitor must surface every line so the panel
    ///   shows live state.
    /// Acceptance: a snapshot with host + a drive maps one DTO per line.
    /// </summary>
    [Fact]
    public void HostPlusDrive_YieldsAllLines_PanelShows()
    {
        var lines = HostProtocolMapper.BuildIecBusLines(Snapshot("c64", "drive-8"));

        Assert.Equal(4, lines.Count);
        Assert.Equal("ATN", lines[0].Signal);
        Assert.True(lines[0].IsHigh);
    }
}
