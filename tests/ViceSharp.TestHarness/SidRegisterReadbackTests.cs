namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

public sealed class SidRegisterReadbackTests
{
    [Fact]
    public void Voice3Readback_UsesOsc3AndEnv3Addresses()
    {
        var sid = new Sid6581(new BasicBus());

        sid.Write(0xD40E, 0x00);
        sid.Write(0xD40F, 0x80);
        sid.Write(0xD412, 0x01);
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD41B, 0xAA);
        sid.Write(0xD41C, 0xBB);

        for (var i = 0; i < 512; i++)
            sid.Tick();

        Assert.Equal(0x01, sid.Read(0xD41B));
        Assert.NotEqual(0xBB, sid.Read(0xD41C));
        Assert.True(sid.Read(0xD41C) > 0);
    }
}
