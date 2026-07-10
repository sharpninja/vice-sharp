using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S10: DIVERGENT (red-now) remediation tests for
/// FR-SID-POT (AC-01..04) and FR-SID-DATABUS (AC-01..10) in
/// artifacts/vice-parity-requirements/requirements.yaml.
///
/// The spec is reSID (native/vice/vice/src/resid: sid.cc read/write, pot.cc),
/// reached bit-exactly through the single-cycle vice_sid_exact_* oracle (MOS
/// 6581 engine). Read semantics: reg $19/$1A latch 0xff (paddle not modeled),
/// $1B/$1C latch OSC3/ENV3, and every other read returns the shared fading
/// data bus (bus_value), not the per-register file. The reliable oracle
/// observable for the bus is the read path (SidExactRead, which returns
/// bus_value); the exported SidExactGetState().BusValue is not a dependable
/// snapshot of the live latch, so the managed bus seams (DataBusValue /
/// DataBusValueTtl) are checked against the reSID spec constants directly. All
/// assertions are exact equality; no tolerances.
///
/// DATABUS-01/04/05/06/08 are remediated locks: the write-side bus latch,
/// aging, ttl, and reset-clear landed in S1 and pass immediately; they are
/// authored here (matching the artifact tag) so the audit-time red-now state is
/// captured under a passing regression test. DATABUS-07 (8580 databus_ttl) is
/// quarantined pending S11's per-model DataBusTtl override.
/// </summary>
[Collection("NativeVice")]
public sealed class SidDatabusPotDivergentParityTests
{
    private const ushort PotX = 0xD419;
    private const ushort PotY = 0xD41A;
    private const ushort Osc3 = 0xD41B;
    private const ushort Env3 = 0xD41C;
    private const int DataBusTtl6581 = 0x1D00;

    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>
    /// FR: FR-SID-POT AC-01 (DIVERGENT, finding 22), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-POT-01.
    /// Use case: reSID readPOT() returns 0xff because the paddle is not modeled
    /// (pot.cc:25-29); the managed chip returned _registers[0x19] (0).
    /// Acceptance: managed $D419 read equals the oracle read($19) equals 0xFF.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-POT-01", ParityTag.Divergent, pending: false)]
    public void PotX_Returns0xFF_MatchingOracle()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            var oracle = ViceNativeBridge.SidExactRead(native, 0x19);
            Assert.Equal((byte)0xFF, oracle);
            Assert.Equal(oracle, sid.Read(PotX));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-POT AC-02 (DIVERGENT, finding 22), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-POT-02.
    /// Use case: reading $D419 both returns 0xff and latches it onto the shared
    /// data bus with a full ttl (sid.cc read $19); the managed chip left the bus
    /// untouched.
    /// Acceptance: the oracle read($19) is 0xFF, and after the managed read the
    /// managed bus seam holds 0xFF with a full 6581 ttl (0x1D00).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-POT-02", ParityTag.Divergent, pending: false)]
    public void PotX_Read_LatchesBusValueAndTtl()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            Assert.Equal((byte)0xFF, ViceNativeBridge.SidExactRead(native, 0x19));
            _ = sid.Read(PotX);

            Assert.Equal((byte)0xFF, sid.DataBusValue);
            Assert.Equal(DataBusTtl6581, sid.DataBusValueTtl);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-POT AC-03 (DIVERGENT, finding 22), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-POT-03.
    /// Use case: $D41A (POTY) behaves identically to POTX.
    /// Acceptance: the oracle read($1A) is 0xFF, the managed read matches, and
    /// the managed bus seam latches 0xFF with a full ttl.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-POT-03", ParityTag.Divergent, pending: false)]
    public void PotY_Returns0xFFAndLatchesBus()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            var oracleValue = ViceNativeBridge.SidExactRead(native, 0x1A);
            Assert.Equal((byte)0xFF, oracleValue);
            Assert.Equal(oracleValue, sid.Read(PotY));
            Assert.Equal((byte)0xFF, sid.DataBusValue);
            Assert.Equal(DataBusTtl6581, sid.DataBusValueTtl);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-POT AC-04 (DIVERGENT, finding 22), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-POT-04.
    /// Use case: the exact-equality target for the POT registers is a constant
    /// 0xff regardless of writes or elapsed cycles (paddle timing lives in the
    /// CIA/host, not the SID).
    /// Acceptance: after arbitrary register writes and 1000 lockstep cycles,
    /// both POTX and POTY read exactly 0xFF on both sides.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-POT-04", ParityTag.Divergent, pending: false)]
    public void PotReads_Constant0xFF_AcrossWritesAndCycles()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            foreach (var (reg, val) in new (ushort, byte)[] { (0x00, 0x81), (0x01, 0x42), (0x04, 0x21) })
            {
                ViceNativeBridge.SidExactWrite(native, reg, val);
                sid.Write((ushort)(0xD400 + reg), val);
            }

            for (var cycle = 1; cycle <= 1000; cycle++)
            {
                ViceNativeBridge.SidExactClock(native, 1);
                sid.Tick();
            }

            Assert.Equal((byte)0xFF, ViceNativeBridge.SidExactRead(native, 0x19));
            Assert.Equal((byte)0xFF, sid.Read(PotX));
            Assert.Equal((byte)0xFF, ViceNativeBridge.SidExactRead(native, 0x1A));
            Assert.Equal((byte)0xFF, sid.Read(PotY));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-01 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-01.
    /// Use case: every register write drives the shared data bus and reloads the
    /// ttl (reSID SID::write). Remediated in S1; authored as a regression lock.
    /// Acceptance: after each write in a short program the managed DataBusValue
    /// holds the written byte with a full 6581 ttl, and a following write-only
    /// read reports it on the oracle read path.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-01", ParityTag.Divergent, pending: false)]
    public void AnyWrite_SetsBusValueAndTtl_MatchingOracle()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            foreach (var (reg, val) in new (ushort, byte)[] { (0x00, 0x12), (0x07, 0xAB), (0x18, 0x0F) })
            {
                ViceNativeBridge.SidExactWrite(native, reg, val);
                sid.Write((ushort)(0xD400 + reg), val);

                Assert.Equal(val, sid.DataBusValue);
                Assert.Equal(DataBusTtl6581, sid.DataBusValueTtl);
                // The write is observable on the oracle read path as the shared bus.
                Assert.Equal(val, ViceNativeBridge.SidExactRead(native, 0x02));
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-02 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-02.
    /// Use case: reads of $19/$1A/$1B/$1C both return their value and latch the
    /// shared bus (reSID SID::read); the managed chip returned the value for
    /// $1B/$1C without latching and 0 for $19/$1A.
    /// Acceptance: after driving voice 3, each of the four reads returns the
    /// oracle's value bit-exactly and leaves the managed bus seam equal to the
    /// value just returned.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-02", ParityTag.Divergent, pending: false)]
    public void ReadOf19Through1C_SetsBusAndReturnsBusValue()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            DriveVoice3(native, sid);

            foreach (var reg in new ushort[] { 0x19, 0x1A, 0x1B, 0x1C })
            {
                var addr = (ushort)(0xD400 + reg);
                var oracleValue = ViceNativeBridge.SidExactRead(native, reg);
                var managedValue = sid.Read(addr);
                Assert.Equal(oracleValue, managedValue);
                Assert.Equal(managedValue, sid.DataBusValue);
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-03 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-03.
    /// Use case: a read of a write-only register returns the shared fading
    /// bus_value (the last write to any register), not a per-register byte; the
    /// managed chip returned _registers[reg].
    /// Acceptance: after writing 0x42 to $D400, reading $D40F returns 0x42 on
    /// both sides; after an OSC3 read latches a new value, a $D405 read returns
    /// that OSC3 byte on both sides.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-03", ParityTag.Divergent, pending: false)]
    public void WriteOnlyRegisterRead_ReturnsSharedBusValue()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            ViceNativeBridge.SidExactWrite(native, 0x00, 0x42);
            sid.Write(0xD400, 0x42);

            var oracleWo = ViceNativeBridge.SidExactRead(native, 0x0F);
            Assert.Equal((byte)0x42, oracleWo);
            Assert.Equal(oracleWo, sid.Read(0xD40F));

            DriveVoice3(native, sid);
            var osc3Oracle = ViceNativeBridge.SidExactRead(native, 0x1B);
            var osc3Managed = sid.Read(Osc3);
            Assert.Equal(osc3Oracle, osc3Managed);
            Assert.Equal(osc3Oracle, ViceNativeBridge.SidExactRead(native, 0x05));
            Assert.Equal(osc3Managed, sid.Read(0xD405));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-04 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-04.
    /// Use case: the shared bus ages one cycle at a time and zeroes exactly at
    /// ttl expiry (reSID sid.h). Remediated in S1; authored as a regression
    /// lock. Managed dispatches only single-cycle ticks.
    /// Acceptance: after a write the value survives ttl-1 cycles and the bus
    /// reads zero on the ttl-th cycle.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-04", ParityTag.Divergent, pending: false)]
    public void BusAging_OneCycleDecrement_ZeroesAtTtlExpiry()
    {
        var sid = BuildSid();

        sid.Write(0xD400, 0x5A);
        Assert.Equal((byte)0x5A, sid.DataBusValue);

        for (var i = 0; i < DataBusTtl6581 - 1; i++)
        {
            sid.Tick();
        }
        Assert.Equal((byte)0x5A, sid.DataBusValue);
        Assert.Equal(1, sid.DataBusValueTtl);

        sid.Tick();
        Assert.Equal((byte)0x00, sid.DataBusValue);
        Assert.Equal(0, sid.DataBusValueTtl);
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-05 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-05.
    /// Use case: reSID's buffered path ages the bus by a whole delta_t at once
    /// (bus_value_ttl -= delta_t). STRUCTURAL: the managed chip has no batched
    /// clock entry point (and none is planned); the observable single-cycle
    /// contract ttl(after N ticks) == ttl0 - N is the per-cycle equivalent.
    /// Acceptance: after a write, N single ticks (for several N below the ttl)
    /// leave DataBusValueTtl == 0x1D00 - N.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-05", ParityTag.Divergent, pending: false)]
    public void BusAging_NCycles_EquivalentToBatchedDecrement()
    {
        var sid = BuildSid();
        foreach (var n in new[] { 1, 7, 100, 0x1000 })
        {
            sid.Write(0xD400, 0x33);
            Assert.Equal(DataBusTtl6581, sid.DataBusValueTtl);
            for (var i = 0; i < n; i++)
            {
                sid.Tick();
            }
            Assert.Equal(DataBusTtl6581 - n, sid.DataBusValueTtl);
        }
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-06 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-06.
    /// Use case: the 6581 databus ttl is 0x1d00 (reSID sid.cc). Remediated in
    /// S1; authored as a regression lock.
    /// Acceptance: managed DataBusValueTtl right after a write equals 0x1D00.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-06", ParityTag.Divergent, pending: false)]
    public void DataBusTtl6581_Is0x1D00()
    {
        var sid = BuildSid();
        sid.Write(0xD400, 0x77);
        Assert.Equal(DataBusTtl6581, sid.DataBusValueTtl);
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-07 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-002,
    /// TEST: TEST-SID-DATABUS-07.
    /// Use case: the 8580 databus fade ttl is 0xa2000 cycles (sid.cc:119), vs
    /// 0x1d00 on the 6581; a register write reloads the bus ttl to that value.
    /// Acceptance: Sid8580 DataBusTtl == 0xA2000 and a write latches
    /// DataBusValueTtl == 0xA2000 (unblocked by the PLAN-VICEPARITY-001 S11
    /// per-model DataBusTtl override).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-DATABUS-07", ParityTag.Divergent, pending: false)]
    public void DataBusTtl8580_Is0xA2000()
    {
        var sid = new Sid8580(new BasicBus()) { BaseAddress = 0xD400 };
        Assert.Equal(0xA2000, sid.DataBusTtlSeam);
        sid.Write(0xD400, 0x77);
        Assert.Equal(0xA2000, sid.DataBusValueTtl);
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-08 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-08.
    /// Use case: reset() clears bus_value and bus_value_ttl (reSID sid.cc).
    /// Remediated in S1; authored as a regression lock.
    /// Acceptance: after a write then Reset, both DataBusValue and
    /// DataBusValueTtl are 0.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-08", ParityTag.Divergent, pending: false)]
    public void Reset_ClearsBusValueAndTtl()
    {
        var sid = BuildSid();
        sid.Write(0xD400, 0x99);
        Assert.Equal((byte)0x99, sid.DataBusValue);

        sid.Reset();
        Assert.Equal((byte)0x00, sid.DataBusValue);
        Assert.Equal(0, sid.DataBusValueTtl);
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-09 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-09.
    /// Use case: an OSC3 ($1B) read latches the returned value onto the shared
    /// bus, so a following write-only read reports it (reSID sid.cc).
    /// Acceptance: after driving voice 3, an OSC3 read followed by a $D400 read
    /// returns the same OSC3 byte on both sides.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-09", ParityTag.Divergent, pending: false)]
    public void Osc3Read_LatchesBus()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            DriveVoice3(native, sid);
            var osc3Oracle = ViceNativeBridge.SidExactRead(native, 0x1B);
            var osc3Managed = sid.Read(Osc3);
            Assert.Equal(osc3Oracle, osc3Managed);

            Assert.Equal(osc3Oracle, ViceNativeBridge.SidExactRead(native, 0x00));
            Assert.Equal(osc3Managed, sid.Read(0xD400));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-DATABUS AC-10 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-DATABUS-10.
    /// Use case: an ENV3 ($1C) read latches the returned value onto the shared
    /// bus (reSID sid.cc).
    /// Acceptance: after driving voice 3, an ENV3 read followed by a $D400 read
    /// returns the same ENV3 byte on both sides.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-DATABUS-10", ParityTag.Divergent, pending: false)]
    public void Env3Read_LatchesBus()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            DriveVoice3(native, sid);
            var env3Oracle = ViceNativeBridge.SidExactRead(native, 0x1C);
            var env3Managed = sid.Read(Env3);
            Assert.Equal(env3Oracle, env3Managed);

            Assert.Equal(env3Oracle, ViceNativeBridge.SidExactRead(native, 0x00));
            Assert.Equal(env3Managed, sid.Read(0xD400));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// Drive voice 3 (sawtooth, FREQ $4000, gate on) on both sides and clock a
    /// few hundred cycles so OSC3 and ENV3 hold nonzero, deterministic values.
    /// </summary>
    private static void DriveVoice3(IntPtr native, Sid6581 sid)
    {
        void Both(ushort reg, byte val)
        {
            ViceNativeBridge.SidExactWrite(native, reg, val);
            sid.Write((ushort)(0xD400 + reg), val);
        }

        Both(0x0E, 0x00); // freq lo
        Both(0x0F, 0x40); // freq hi -> $4000
        Both(0x13, 0x00); // attack/decay = 0 (fast)
        Both(0x14, 0xF0); // sustain full
        Both(0x12, 0x21); // sawtooth + gate

        for (var i = 0; i < 400; i++)
        {
            ViceNativeBridge.SidExactClock(native, 1);
            sid.Tick();
        }
    }
}
