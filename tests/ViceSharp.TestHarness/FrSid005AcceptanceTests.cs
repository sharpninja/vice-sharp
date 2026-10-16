namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-SID-005 acceptance tests. Iteration 2. The 8580 filter is the reSID
/// model selected by the machine profile.
/// </summary>
public sealed class FrSid005AcceptanceTests
{
    /// <summary>
    /// FR: FR-SID-005.
    /// Use case: The 8580 cutoff DAC is the linear reSID f0_dac scale. The
    /// 6581 path is the kinked voltage curve.
    /// Acceptance: On an 8580, FilterNDac at the midpoint cutoff is closer
    /// to the linear midpoint than the 6581 voltage term is.
    /// </summary>
    [Fact]
    public void CutoffMapping_8580IsCloserToLinearThan6581()
    {
        var oldChip = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        var newChip = new Sid8580(new BasicBus()) { BaseAddress = 0xD400 };

        WriteCutoff(oldChip, 0x000);
        WriteCutoff(newChip, 0x000);
        var oldLow = oldChip.FilterVddt_Vw_2;
        var newLow = newChip.FilterNDac;

        WriteCutoff(oldChip, 0x400);
        WriteCutoff(newChip, 0x400);
        var oldMid = oldChip.FilterVddt_Vw_2;
        var newMid = newChip.FilterNDac;

        WriteCutoff(oldChip, 0x7FF);
        WriteCutoff(newChip, 0x7FF);
        var oldHigh = oldChip.FilterVddt_Vw_2;
        var newHigh = newChip.FilterNDac;

        var oldError = Math.Abs(oldMid - (oldLow + ((oldHigh - oldLow) / 2)));
        var newError = Math.Abs(newMid - (newLow + ((newHigh - newLow) / 2)));
        Assert.True(newError < oldError);
    }

    /// <summary>
    /// FR: FR-SID-005.
    /// Use case: 8580 resonance is milder than 6581 at the same extreme
    /// setting.
    /// Acceptance: The shipped reSID coefficients are on one scale.
    /// 6581 n at resonance 15 is 0 (filter8580new.cc). 8580 resGain[15]
    /// stays above 0. Raw FilterVbp samples are not that scale.
    /// </summary>
    [Fact]
    public void Resonance_IsLessExtremeOn8580()
    {
        var oldChip = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        var newChip = new Sid8580(new BasicBus()) { BaseAddress = 0xD400 };
        oldChip.Write(0xD417, 0xF0);
        newChip.Write(0xD417, 0xF0);
        oldChip.Tick();
        newChip.Tick();

        Assert.True(
            newChip.LiveResonanceGain > oldChip.LiveResonanceGain,
            $"8580 resonance gain {newChip.LiveResonanceGain}, 6581 resonance gain {oldChip.LiveResonanceGain}");
    }

    /// <summary>
    /// FR: FR-SID-005.
    /// Use case: The 8580 does not use the 6581 high-resonance clip curve.
    /// Acceptance: IsMos8580FilterSeam is false on Sid6581 and true on Sid8580.
    /// </summary>
    [Fact]
    public void ChipModel_SelectsTheFilterImplementation()
    {
        var oldChip = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        var newChip = new Sid8580(new BasicBus()) { BaseAddress = 0xD400 };

        Assert.False(oldChip.IsMos8580FilterSeam);
        Assert.True(newChip.IsMos8580FilterSeam);
    }

    /// <summary>
    /// FR: FR-SID-005.
    /// Use case: The active machine profile chooses 6581 or 8580.
    /// Acceptance: The default C64 profile builds a Sid6581. The c64c
    /// profile builds a Sid8580.
    /// </summary>
    [Fact]
    public void MachineProfile_Selects6581Or8580()
    {
        var pal = MachineTestFactory.CreateC64Machine();
        var c64c = MachineTestFactory.CreateC64Machine("c64c");

        Assert.IsType<Sid6581>(pal.Devices.GetByRole(DeviceRole.AudioChip));
        Assert.IsNotType<Sid8580>(pal.Devices.GetByRole(DeviceRole.AudioChip));
        Assert.IsType<Sid8580>(c64c.Devices.GetByRole(DeviceRole.AudioChip));
    }

    /// <summary>
    /// FR: FR-SID-005.
    /// Use case: Configuration can switch the running machine between the
    /// 6581 and 8580 filter models.
    /// Acceptance: The live audio device exposes a switch, and after the
    /// switch the device is the other model.
    /// </summary>
    [Fact]
    public void RuntimeConfiguration_SwitchesFilterModel()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var sid = Assert.IsAssignableFrom<Sid6581>(machine.Devices.GetByRole(DeviceRole.AudioChip));
        var switchMethod = sid.GetType().GetMethod("SetSidModel")
            ?? sid.GetType().GetMethod("SwitchFilterModel");

        Assert.NotNull(switchMethod);
        switchMethod!.Invoke(sid, ["8580"]);
        var current = Assert.IsType<Sid8580>(machine.Devices.GetByRole(DeviceRole.AudioChip));
        machine.Bus.Write(0xD418, 0x05);
        Assert.Equal(0x05, current.Peek(0xD418));
        Assert.Equal(0x00, sid.Peek(0xD418));
    }

    private static void WriteCutoff(Sid6581 sid, int cutoff)
    {
        sid.Write(0xD415, (byte)(cutoff & 0x07));
        sid.Write(0xD416, (byte)((cutoff >> 3) & 0xFF));
    }
}
