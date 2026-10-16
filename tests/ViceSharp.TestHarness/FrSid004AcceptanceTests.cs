namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-SID-004 acceptance tests for the 6581 filter registers and the
/// reSID cutoff curve. Iteration 1.
/// </summary>
public sealed class FrSid004AcceptanceTests
{
    /// <summary>
    /// FR: FR-SID-004.
    /// Use case: $D415 holds the low 3 bits of the 11-bit cutoff and $D416
    /// holds the high 8 bits.
    /// Acceptance: Writing $07 then $FF leaves the 11-bit cutoff at $7FF,
    /// and a later $D415 write keeps the high bits.
    /// </summary>
    [Fact]
    public void Cutoff_IsAn11BitValueFromD415AndD416()
    {
        var sid = NewSid();

        sid.Write(0xD415, 0x07);
        sid.Write(0xD416, 0xFF);
        var full = sid.FilterVddt_Vw_2;

        sid.Write(0xD415, 0x01);
        Assert.NotEqual(full, sid.FilterVddt_Vw_2);
        Assert.Equal(0x01, sid.Peek(0xD415) & 0x07);
        Assert.Equal(0xFF, sid.Peek(0xD416));
    }

    /// <summary>
    /// FR: FR-SID-004.
    /// Use case: Filter resonance is the upper 4 bits of $D417.
    /// Acceptance: Writing $F0 stores resonance 15 in the register file.
    /// Writing $0F does not.
    /// </summary>
    [Fact]
    public void Resonance_IsTheUpperNibbleOfD417()
    {
        var sid = NewSid();

        sid.Write(0xD417, 0xF0);
        Assert.Equal(0xF0, sid.Peek(0xD417) & 0xF0);

        sid.Write(0xD417, 0x0F);
        Assert.Equal(0x00, sid.Peek(0xD417) & 0xF0);
    }

    /// <summary>
    /// FR: FR-SID-004.
    /// Use case: Low-pass, band-pass, and high-pass are $D418 bits 4-6 and
    /// may be combined.
    /// Acceptance: Writing $70 sets bits 4-6 together. The register read
    /// returns those bits.
    /// </summary>
    [Fact]
    public void FilterModes_AreD418Bits4Through6AndCombine()
    {
        var sid = NewSid();

        sid.Write(0xD418, 0x70);

        Assert.Equal(0x70, sid.Peek(0xD418) & 0x70);
    }

    /// <summary>
    /// FR: FR-SID-004.
    /// Use case: Each voice is routed through the filter by $D417 bits 0-2.
    /// Acceptance: Writing $07 sets the three voice route bits.
    /// </summary>
    [Fact]
    public void VoiceFilterRouting_IsD417Bits0Through2()
    {
        var sid = NewSid();

        sid.Write(0xD417, 0x07);

        Assert.Equal(0x07, sid.Peek(0xD417) & 0x07);
    }

    /// <summary>
    /// FR: FR-SID-004.
    /// Use case: External audio is routed through the filter by $D417 bit 3.
    /// Acceptance: Writing $08 sets bit 3 and leaves the voice route bits clear.
    /// </summary>
    [Fact]
    public void ExternalInput_IsD417Bit3()
    {
        var sid = NewSid();

        sid.Write(0xD417, 0x08);

        Assert.Equal(0x08, sid.Peek(0xD417) & 0x0F);
    }

    /// <summary>
    /// FR: FR-SID-004.
    /// Use case: The 6581 cutoff mapping is the kinked reSID f0_dac curve,
    /// not a straight line from the 11-bit register.
    /// Acceptance: The filter voltage term at cutoff $000, $400, and $7FF
    /// is not the linear midpoint of the two ends.
    /// </summary>
    [Fact]
    public void CutoffMapping_IsNotLinear()
    {
        var sid = NewSid();

        sid.Write(0xD416, 0x00);
        sid.Write(0xD415, 0x00);
        var low = sid.FilterVddt_Vw_2;

        sid.Write(0xD416, 0x80);
        var mid = sid.FilterVddt_Vw_2;

        sid.Write(0xD416, 0xFF);
        sid.Write(0xD415, 0x07);
        var high = sid.FilterVddt_Vw_2;

        var linearMid = low + ((high - low) / 2);
        Assert.NotEqual(linearMid, mid);
    }

    /// <summary>
    /// FR: FR-SID-004.
    /// Use case: High resonance on the 6581 produces the analog soft-clip
    /// path rather than a second copy of the resonance-zero filter state.
    /// Acceptance: The same voice and cutoff leave a different band-pass
    /// state at resonance 15 than at resonance 0.
    /// </summary>
    [Fact]
    public void HighResonance_ChangesThe6581FilterState()
    {
        var quiet = RunFilter(resonance: 0x00);
        var loud = RunFilter(resonance: 0xF0);

        Assert.NotEqual(quiet, loud);
    }

    private static int RunFilter(byte resonance)
    {
        var sid = NewSid();
        sid.Write(0xD401, 0x10);
        sid.Write(0xD404, 0x11);
        sid.Write(0xD406, 0xF0);
        sid.Write(0xD416, 0x40);
        sid.Write(0xD417, (byte)(resonance | 0x01));
        sid.Write(0xD418, 0x2F);
        for (var i = 0; i < 2000; i++)
            sid.Tick();
        return sid.FilterVbp;
    }

    private static Sid6581 NewSid() => new(new BasicBus()) { BaseAddress = 0xD400 };
}
