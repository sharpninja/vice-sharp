using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Tape;
using Xunit;

namespace ViceSharp.TestHarness;

public sealed class StorageRuntimeTests
{
    [Fact]
    public void D64_Attach_And_ReadSector_Returns_Deterministic_Bytes()
    {
        var imageData = new byte[D64Image.DiskSize35Track];
        var sectorOffset = Track18Sector1Offset();
        for (var i = 0; i < 256; i++)
        {
            imageData[sectorOffset + i] = (byte)(255 - i);
        }

        Assert.True(IecD64Attachment.TryAttach(8, imageData, out var attachment));

        var sector = new byte[256];
        Assert.True(attachment!.TryReadSector(18, 1, sector));
        Assert.Equal(255, sector[0]);
        Assert.Equal(128, sector[127]);
        Assert.Equal(0, sector[255]);
    }

    [Fact]
    public void D64_Attach_Rejects_Invalid_Size()
    {
        Assert.False(IecD64Attachment.TryAttach(8, new byte[D64Image.DiskSize35Track - 1], out var attachment));
        Assert.Null(attachment);
    }

    [Fact]
    public void Tap_Attach_And_Datasette_ReadPulse_Requires_Motor_And_Play()
    {
        var tap = CreateTapImage(1, [10, 0, 0x40, 0x1F, 0x00]);

        Assert.True(TapImage.TryAttach(tap, out var image));

        var datasette = new Datasette();
        Assert.True(datasette.Attach(image!));
        Assert.True(datasette.HasTape);

        Assert.False(datasette.TryReadNextPulse(out _));

        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        Assert.True(datasette.TryReadNextPulse(out var shortPulse));
        Assert.Equal(80, shortPulse);

        Assert.True(datasette.TryReadNextPulse(out var overflowPulse));
        Assert.Equal(8000, overflowPulse);

        Assert.False(datasette.TryReadNextPulse(out _));
    }

    [Fact]
    public void Tap_V0_OverflowPulse_Reads_ThreeByteCycleCount()
    {
        var tap = CreateTapImage(0, [0, 0x40, 0x1F, 0x00]);

        Assert.True(TapImage.TryAttach(tap, out var image));

        var reader = image!.CreatePulseReader();
        Assert.True(reader.TryReadNextPulse(out var cycles));
        Assert.Equal(8000, cycles);
        Assert.False(reader.TryReadNextPulse(out _));
    }

    [Fact]
    public void Tap_Attach_Rejects_Truncated_Pulse_Data()
    {
        var tap = CreateTapImage(1, [10, 20]);
        tap[16] = 3;

        Assert.False(TapImage.TryAttach(tap, out var image));
        Assert.Null(image);
    }

    private static byte[] CreateTapImage(byte version, byte[] pulseData)
    {
        var image = new byte[20 + pulseData.Length];
        "C64-TAPE-RAW"u8.CopyTo(image);
        image[12] = version;
        image[16] = (byte)pulseData.Length;
        image[17] = (byte)(pulseData.Length >> 8);
        image[18] = (byte)(pulseData.Length >> 16);
        image[19] = (byte)(pulseData.Length >> 24);
        pulseData.CopyTo(image.AsSpan(20));
        return image;
    }

    private static int Track18Sector1Offset()
    {
        var offset = 0;
        for (var track = 1; track < 18; track++)
        {
            offset += 21 * 256;
        }

        return offset + 256;
    }
}
