namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-CIA-005 acceptance tests for the CIA serial shift register.
/// Iteration 2. Output mode is the timer path. Input mode and the SP/CNT
/// pins are not on the chip surface.
/// </summary>
public sealed class FrCia005AcceptanceTests
{
    /// <summary>
    /// FR: FR-CIA-005.
    /// Use case: Output mode clocks one serial byte from Timer A. VICE
    /// ciacore clears sr_bits and sdr_valid when CRA bit 6 turns on, so an
    /// SDR write before that switch does not arm a transfer. An SDR write
    /// after output mode is selected arms one byte. Completion sets ICR
    /// bit 3. The same byte is not sent again without a new SDR write.
    /// Acceptance: SDR then CRA leaves bit 3 clear. CRA $41 then SDR $A5
    /// sets bit 3 once inside a bounded tick window, and the same window
    /// again leaves bit 3 clear.
    /// </summary>
    [Fact]
    public void OutputMode_EightTimerUnderflows_SetTheSdrInterrupt()
    {
        var cleared = NewCia();
        cleared.Write(0xDC04, 0x02);
        cleared.Write(0xDC05, 0x00);
        cleared.Write(0xDC0C, 0xA5);
        cleared.Write(0xDC0E, 0x41);
        for (var i = 0; i < 80; i++)
            cleared.Tick();

        Assert.Equal(0x00, cleared.Read(0xDC0D) & 0x08);

        var cia = NewCia();
        cia.Write(0xDC04, 0x02);
        cia.Write(0xDC05, 0x00);
        cia.Write(0xDC0E, 0x41);
        cia.Write(0xDC0C, 0xA5);

        var sawSerialIrq = false;
        var ticksToIrq = 0;
        for (var i = 0; i < 80; i++)
        {
            cia.Tick();
            ticksToIrq++;
            if ((cia.Read(0xDC0D) & 0x08) != 0)
            {
                sawSerialIrq = true;
                break;
            }
        }

        Assert.True(sawSerialIrq);

        for (var i = 0; i < ticksToIrq; i++)
        {
            cia.Tick();
            Assert.Equal(0x00, cia.Read(0xDC0D) & 0x08);
        }
    }

    /// <summary>
    /// FR: FR-CIA-005.
    /// Use case: CRA bit 6 is the shift direction. Clear is input and the
    /// clock is the CNT pin, not Timer A.
    /// Acceptance: With bit 6 clear, the same Timer A run does not set ICR
    /// bit 3.
    /// </summary>
    [Fact]
    public void InputMode_DoesNotShiftOnTimerA()
    {
        var cia = NewCia();
        cia.Write(0xDC04, 0x02);
        cia.Write(0xDC05, 0x00);
        cia.Write(0xDC0C, 0xA5);
        cia.Write(0xDC0E, 0x01);

        for (var i = 0; i < 400; i++)
            cia.Tick();

        var icr = cia.Read(0xDC0D);
        Assert.Equal(0x00, icr & 0x08);
    }

    /// <summary>
    /// FR: FR-CIA-005.
    /// Use case: In input mode, bits shift on each external CNT edge.
    /// Acceptance: The chip exposes a CNT edge, and eight edges set ICR bit 3.
    /// </summary>
    [Fact]
    public void InputMode_ShiftsOnCntEdges()
    {
        var cia = NewCia();
        cia.Write(0xDC0E, 0x00);
        var cnt = cia.GetType().GetMethod("TriggerCntEdge")
            ?? cia.GetType().GetMethod("OnCntPin");

        Assert.NotNull(cnt);
        var dataIn = cia.GetType().GetProperty("SerialDataIn");
        Assert.NotNull(dataIn);
        foreach (var bit in new[] { true, false, true, false, false, true, false, true })
        {
            dataIn!.SetValue(cia, bit);
            cnt!.Invoke(cia, null);
        }

        Assert.Equal(0xA5, cia.Read(0xDC0C));
        Assert.Equal(0x08, cia.Read(0xDC0D) & 0x08);
    }

    /// <summary>
    /// FR: FR-CIA-005.
    /// Use case: A partial CNT input count must not survive reset.
    /// Acceptance: Three CNT edges, then Reset, then bits 1,0,1,0,0,1,0,1
    /// latch SDR $A5. VICE ciacore_reset clears sr_bits. The fifth edge
    /// after reset must not latch a short byte.
    /// </summary>
    [Fact]
    public void Reset_ClearsAPartialSerialInputCount()
    {
        var cia = NewCia();
        cia.Write(0xDC0E, 0x00);
        var cnt = cia.GetType().GetMethod("TriggerCntEdge");
        var dataIn = cia.GetType().GetProperty("SerialDataIn");
        Assert.NotNull(cnt);
        Assert.NotNull(dataIn);

        for (var i = 0; i < 3; i++)
        {
            dataIn!.SetValue(cia, true);
            cnt!.Invoke(cia, null);
        }

        cia.Reset();

        var bits = new[] { true, false, true, false, false, true, false, true };
        for (var i = 0; i < 7; i++)
        {
            dataIn!.SetValue(cia, bits[i]);
            cnt!.Invoke(cia, null);
            Assert.Equal(0x00, cia.Read(0xDC0C));
            Assert.Equal(0x00, cia.Read(0xDC0D) & 0x08);
        }

        dataIn!.SetValue(cia, bits[7]);
        cnt!.Invoke(cia, null);
        Assert.Equal(0xA5, cia.Read(0xDC0C));
        Assert.Equal(0x08, cia.Read(0xDC0D) & 0x08);
    }

    /// <summary>
    /// FR: FR-CIA-005.
    /// Use case: Output bits are visible on SP and the shift clock is
    /// visible on CNT.
    /// Acceptance: After one output bit, SP matches the MSB that was loaded
    /// and CNT has produced a clock edge.
    /// </summary>
    [Fact]
    public void ShiftedBit_IsVisibleOnSpAndCnt()
    {
        var cia = NewCia();
        cia.Write(0xDC04, 0x02);
        cia.Write(0xDC05, 0x00);
        cia.Write(0xDC0E, 0x41);
        cia.Write(0xDC0C, 0x80);
        // Latch 2 plus the start pipeline underflows on tick 4. That is
        // the first shifted bit, the MSB. Forty ticks walk past it.
        for (var i = 0; i < 4; i++)
            cia.Tick();

        var sp = cia.GetType().GetProperty("SerialPortPin");
        var cnt = cia.GetType().GetProperty("SerialClockPin");
        Assert.NotNull(sp);
        Assert.NotNull(cnt);
        Assert.Equal(true, sp!.GetValue(cia));

        var sawHigh = (bool)cnt!.GetValue(cia)!;
        var sawLowAfterHigh = false;
        for (var i = 0; i < 6; i++)
        {
            cia.Tick();
            if ((bool)cnt.GetValue(cia)!)
                sawHigh = true;
            else if (sawHigh)
                sawLowAfterHigh = true;
        }

        Assert.True(sawHigh);
        Assert.True(sawLowAfterHigh);
    }

    private static Mos6526 NewCia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6526(bus, irq) { BaseAddress = 0xDC00 };
    }
}
