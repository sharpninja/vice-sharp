namespace ViceSharp.TestHarness.C1541;

using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-002 / TR-IEC-EDGE-001.
/// Use case: The managed IecBus state machine correctly implements the IEC
/// serial bus ATN-response and bit-clock protocol timing as specified by
/// the Commodore IEC serial bus standard.
///
/// ATN-response timing (Tat): A device must assert CLK low (and DATA low)
/// within Tat = 1ms of ATN going low. At the C64 PAL clock (985,248 Hz)
/// this is 985 cycles. The C64-side IecBus manager (the host) asserts CLK
/// simultaneously with ATN as part of the ATN send sequence, releasing CLK
/// to signal "ready to send" after all devices have responded. This test
/// validates that IecBus.Tick() transitions CLK and DATA to the correct
/// asserted (low = false) state within the spec window when ATN is set low.
///
/// VICE reference: iecbus.c:247-266 (ATN change -> VIA1 CA1 signal for 1541
/// ATN response), serial-iec-bus.c (IEC protocol state machine),
/// drive/iec/iecieee.c (IEC device-side ATN handling).
/// Commodore IEC spec: Tat (ATN turnaround) = 0-1000 microseconds.
/// </summary>
public sealed class IecTimingTests
{
    // Maximum cycles allowed for ATN response per IEC spec.
    // Tat = 1ms, at PAL C64 985,248 Hz = 985 cycles. We use 985 as the
    // window (conservative; actual VICE ROM response is typically &lt; 100 cycles).
    private const int AtnResponseWindowCycles = 985;

    /// <summary>
    /// TR-IEC-EDGE-001 / FR-1541.
    /// Use case: IecBus is in its initial state; all IEC bus lines (CLK, DATA,
    /// ATN) must be in their idle (high = true) state before any signal is
    /// asserted. This validates the initial condition for subsequent timing tests.
    /// Acceptance: After ResetBus(), Clock = true, Data = true, Atn = true.
    /// VICE iecbus.c: idle state is all-high for an open-collector bus.
    /// </summary>
    [Fact]
    public void FreshBus_AllSignalLinesIdle_High()
    {
        var bus = new IecBus();
        bus.ResetBus();

        Assert.True(bus.Clock, "CLK must be idle high after ResetBus");
        Assert.True(bus.Data, "DATA must be idle high after ResetBus");
        Assert.True(bus.Atn, "ATN must be idle high after ResetBus");
    }

    /// <summary>
    /// TR-IEC-EDGE-001 / FR-1541.
    /// Use case: When the C64 software asserts ATN (Atn = false), the IecBus
    /// protocol state machine must assert CLK low within Tat = 985 cycles as
    /// part of the ATN send sequence. The host asserts CLK simultaneously with
    /// ATN to signal it is about to send a command byte.
    /// Acceptance: After setting Atn = false and calling Tick() at most 985
    /// times, Clock == false.
    /// VICE serial-iec-bus.c: ATN assert -> CLK held low by sender until
    /// data bit transfer begins.
    /// </summary>
    [Fact]
    public void AtnAsserted_ClkGoesLow_WithinSpecWindow()
    {
        var bus = new IecBus();
        bus.ResetBus();

        // Assert ATN (active low = false).
        bus.Atn = false;

        bool clkWentLow = false;
        for (var cycle = 0; cycle < AtnResponseWindowCycles; cycle++)
        {
            bus.Tick();
            if (!bus.Clock)
            {
                clkWentLow = true;
                break;
            }
        }

        Assert.True(clkWentLow,
            $"IecBus.Tick() must assert CLK low within {AtnResponseWindowCycles} cycles " +
            "of ATN going low (IEC Tat spec; VICE iecbus.c ATN send sequence).");
    }

    /// <summary>
    /// TR-IEC-EDGE-001 / FR-1541.
    /// Use case: When the C64 software asserts ATN (Atn = false), the IecBus
    /// protocol state machine must also assert DATA low within Tat = 985 cycles.
    /// The device-side DATA response is the IEC ATN acknowledge signal.
    /// Acceptance: After setting Atn = false and calling Tick() at most 985
    /// times, Data == false.
    /// VICE serial-iec-bus.c: device pulls DATA low in response to ATN.
    /// </summary>
    [Fact]
    public void AtnAsserted_DataGoesLow_WithinSpecWindow()
    {
        var bus = new IecBus();
        bus.ResetBus();

        bus.Atn = false;

        bool dataWentLow = false;
        for (var cycle = 0; cycle < AtnResponseWindowCycles; cycle++)
        {
            bus.Tick();
            if (!bus.Data)
            {
                dataWentLow = true;
                break;
            }
        }

        Assert.True(dataWentLow,
            $"IecBus.Tick() must assert DATA low within {AtnResponseWindowCycles} cycles " +
            "of ATN going low (IEC ATN acknowledge; VICE iecbus.c ATN response).");
    }

    /// <summary>
    /// TR-IEC-EDGE-001 / FR-1541.
    /// Use case: When ATN is released (Atn = true) after having been asserted,
    /// the IecBus state machine must release CLK high within Tat = 985 cycles.
    /// This signals the end of the ATN command sequence.
    /// Acceptance: After asserting ATN (Clock goes low), releasing ATN
    /// (Atn = true) and calling Tick() at most 985 times, Clock == true.
    /// VICE serial-iec-bus.c: ATN release -> CLK and DATA released by sender.
    /// </summary>
    [Fact]
    public void AtnReleased_ClkReturnsHigh_WithinSpecWindow()
    {
        var bus = new IecBus();
        bus.ResetBus();

        // Assert ATN and advance until Clock is low.
        bus.Atn = false;
        for (var i = 0; i < AtnResponseWindowCycles && bus.Clock; i++)
            bus.Tick();

        Assert.False(bus.Clock, "CLK must be low after ATN assert (precondition for release test).");

        // Now release ATN.
        bus.Atn = true;

        bool clkWentHigh = false;
        for (var cycle = 0; cycle < AtnResponseWindowCycles; cycle++)
        {
            bus.Tick();
            if (bus.Clock)
            {
                clkWentHigh = true;
                break;
            }
        }

        Assert.True(clkWentHigh,
            $"IecBus.Tick() must release CLK high within {AtnResponseWindowCycles} cycles " +
            "of ATN going high (IEC end-of-ATN; VICE serial-iec-bus.c release sequence).");
    }

    /// <summary>
    /// TR-IEC-EDGE-001 / FR-1541.
    /// Use case: When ATN is released after having been asserted, DATA must
    /// also return high within the spec window.
    /// Acceptance: After ATN release, Data == true within 985 Tick() calls.
    /// </summary>
    [Fact]
    public void AtnReleased_DataReturnsHigh_WithinSpecWindow()
    {
        var bus = new IecBus();
        bus.ResetBus();

        bus.Atn = false;
        for (var i = 0; i < AtnResponseWindowCycles && bus.Data; i++)
            bus.Tick();

        Assert.False(bus.Data, "DATA must be low after ATN assert (precondition).");

        bus.Atn = true;

        bool dataWentHigh = false;
        for (var cycle = 0; cycle < AtnResponseWindowCycles; cycle++)
        {
            bus.Tick();
            if (bus.Data)
            {
                dataWentHigh = true;
                break;
            }
        }

        Assert.True(dataWentHigh,
            $"IecBus.Tick() must release DATA high within {AtnResponseWindowCycles} cycles " +
            "of ATN going high (IEC ATN release; VICE iecbus.c DATA release).");
    }
}
