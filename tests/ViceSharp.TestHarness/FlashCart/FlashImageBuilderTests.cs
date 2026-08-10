namespace ViceSharp.TestHarness.FlashCart;

using ViceSharp.Core.FlashCarts;
using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-FLASHCART-001. Byrd Slice A: head-agnostic flash/ROM image builder.
/// Use case: compose FE3 / Ultimem / Mega-Cart / EasyFlash-layout images from bank fills.
/// Acceptance: sizes match cart constants; bank writes land at documented offsets; empty fill is profile EmptyFill.
/// </summary>
public sealed class FlashImageBuilderTests
{
    [Fact]
    public void Fe3Profile_MatchesFinalExpansion3FlashSize_And64BanksOf8K()
    {
        var p = FlashCartProfiles.Fe3;
        Assert.Equal("fe3", p.Id);
        Assert.Equal(FinalExpansion3Cartridge.FlashSize, p.ImageSizeBytes);
        Assert.Equal(0x2000, p.BankSizeBytes);
        Assert.Equal(64, p.BankCount);
        Assert.Equal(0xFF, p.EmptyFill);
        Assert.Equal(0, p.SecondarySizeBytes);
    }

    [Fact]
    public void UltimemProfile_Default1MiB_8KBanks()
    {
        var p = FlashCartProfiles.Ultimem;
        Assert.Equal(UltimemCartridge.DefaultImageSize, p.ImageSizeBytes);
        Assert.Equal(0x2000, p.BankSizeBytes);
        Assert.Equal(UltimemCartridge.DefaultImageSize / 0x2000, p.BankCount);
    }

    [Fact]
    public void MegaCartProfile_1MiBRom_Plus8KNvramSecondary()
    {
        var p = FlashCartProfiles.MegaCart;
        Assert.Equal(MegaCartCartridge.LowRomSize, p.ImageSizeBytes);
        Assert.Equal(MegaCartCartridge.NvramSize, p.SecondarySizeBytes);
        Assert.Equal(0x2000, p.BankSizeBytes);
    }

    [Fact]
    public void EasyFlashProfile_8KBanks_StructureForC64Reuse()
    {
        var p = FlashCartProfiles.EasyFlash;
        Assert.Equal("easyflash", p.Id);
        Assert.Equal(0x2000, p.BankSizeBytes);
        Assert.True(p.BankCount >= 1);
        Assert.True(p.ImageSizeBytes == p.BankSizeBytes * p.BankCount);
    }

    [Fact]
    public void BuildEmpty_Fe3_IsAllEmptyFill()
    {
        var b = new FlashImageBuilder(FlashCartProfiles.Fe3);
        var img = b.Build();
        Assert.Equal(FinalExpansion3Cartridge.FlashSize, img.Length);
        Assert.All(img, x => Assert.Equal(0xFF, x));
    }

    [Fact]
    public void WriteRaw_Bank0_LandsAtOffsetZero()
    {
        var b = new FlashImageBuilder(FlashCartProfiles.Fe3);
        var payload = new byte[0x2000];
        for (var i = 0; i < payload.Length; i++)
            payload[i] = (byte)(i & 0xFF);

        b.WriteRaw(0, payload);
        var img = b.Build();
        Assert.Equal(payload, img.AsSpan(0, 0x2000).ToArray());
        Assert.Equal(0xFF, img[0x2000]);
        Assert.Equal("Bank 0", b.Banks[0].Label[..6]);
        Assert.NotNull(b.Banks[0].SourceName);
    }

    [Fact]
    public void WritePrg_StripsLoadAddress_PlacesPayload()
    {
        var b = new FlashImageBuilder(FlashCartProfiles.Fe3);
        // PRG: load $2000, then 4 data bytes
        var prg = new byte[] { 0x00, 0x20, 0xAA, 0xBB, 0xCC, 0xDD };
        b.WritePrg(1, prg, sourceName: "test.prg");
        var img = b.Build();
        var off = 0x2000; // bank 1
        Assert.Equal(0xAA, img[off]);
        Assert.Equal(0xBB, img[off + 1]);
        Assert.Equal(0xCC, img[off + 2]);
        Assert.Equal(0xDD, img[off + 3]);
        Assert.Equal(0xFF, img[off + 4]);
        Assert.Equal(FlashBankContentKind.Prg, b.Banks[1].ContentKind);
        Assert.Equal("test.prg", b.Banks[1].SourceName);
    }

    [Fact]
    public void WriteRaw_Oversize_Throws()
    {
        var b = new FlashImageBuilder(FlashCartProfiles.Fe3);
        var tooBig = new byte[0x2001];
        Assert.Throws<ArgumentException>(() => b.WriteRaw(0, tooBig));
    }

    [Fact]
    public void ClearBank_RestoresEmptyFill()
    {
        var b = new FlashImageBuilder(FlashCartProfiles.Fe3);
        b.WriteRaw(0, new byte[] { 1, 2, 3, 4 });
        b.ClearBank(0);
        var img = b.Build();
        Assert.Equal(0xFF, img[0]);
        Assert.Equal(FlashBankContentKind.Empty, b.Banks[0].ContentKind);
    }

    [Fact]
    public void MegaCart_BuildSecondary_Is8KEmptyFill()
    {
        var b = new FlashImageBuilder(FlashCartProfiles.MegaCart);
        var sec = b.BuildSecondary();
        Assert.NotNull(sec);
        Assert.Equal(MegaCartCartridge.NvramSize, sec!.Length);
        Assert.All(sec, x => Assert.Equal(0xFF, x));
    }

    [Fact]
    public void Fe3_Build_AcceptsAsFinalExpansion3FlashImage()
    {
        var b = new FlashImageBuilder(FlashCartProfiles.Fe3);
        b.WriteRaw(0, new byte[] { 0x60 }); // RTS
        var img = b.Build();
        var cart = new FinalExpansion3Cartridge(img);
        Assert.Equal(img.Length, cart.GetFlashImage().Length);
        Assert.Equal(0x60, cart.GetFlashImage()[0]);
    }
}
