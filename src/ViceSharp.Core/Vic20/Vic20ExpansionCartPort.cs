using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// Runtime attach port for FE3 / Ultimem / Mega-Cart on a VIC-20 machine bus.
/// </summary>
public sealed class Vic20ExpansionCartPort : IVic20ExpansionCartPort, IDevice
{
    private readonly BasicBus _bus;
    private IAddressSpace? _attached;
    private Vic20ExpansionCartKind _kind;
    private string _configPresetId = "start";

    public Vic20ExpansionCartPort(BasicBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        Id = new DeviceId(0x0C0F);
    }

    public DeviceId Id { get; }
    public string Name => "VIC-20 expansion cart port";
    public Vic20ExpansionCartKind AttachedKind => _kind;
    public bool WriteBack { get; set; }
    public string ConfigPresetId
    {
        get => _configPresetId;
        set => _configPresetId = value ?? "start";
    }

    public void Reset()
    {
        switch (_attached)
        {
            case FinalExpansion3Cartridge fe3:
                fe3.Reset();
                fe3.ApplyConfigPreset(_configPresetId);
                break;
            case UltimemCartridge um:
                um.Reset();
                um.ApplyConfigPreset(_configPresetId);
                break;
            case MegaCartCartridge mc:
                mc.Reset();
                mc.ApplyConfigPreset(_configPresetId);
                break;
        }
    }

    public void AttachFinalExpansion3(ReadOnlySpan<byte> flashImage)
    {
        Eject();
        var cart = new FinalExpansion3Cartridge(flashImage) { WriteBack = WriteBack };
        cart.ApplyConfigPreset(_configPresetId);
        _attached = cart;
        _kind = Vic20ExpansionCartKind.FinalExpansion3;
        _bus.RegisterDevice(cart);
    }

    public void AttachUltimem(ReadOnlySpan<byte> image)
    {
        Eject();
        var cart = new UltimemCartridge(image) { WriteBack = WriteBack };
        cart.ApplyConfigPreset(_configPresetId);
        _attached = cart;
        _kind = Vic20ExpansionCartKind.Ultimem;
        _bus.RegisterDevice(cart);
    }

    public void AttachMegaCart(ReadOnlySpan<byte> romImage, ReadOnlySpan<byte> nvram = default)
    {
        Eject();
        var cart = new MegaCartCartridge(romImage, nvram) { WriteBack = WriteBack };
        cart.ApplyConfigPreset(_configPresetId);
        _attached = cart;
        _kind = Vic20ExpansionCartKind.MegaCart;
        _bus.RegisterDevice(cart);
    }

    public void Eject()
    {
        if (_attached is null)
            return;
        _bus.UnregisterDevice(_attached);
        _attached = null;
        _kind = Vic20ExpansionCartKind.None;
    }

    public void ApplyConfigPreset(string presetId)
    {
        ConfigPresetId = presetId;
        switch (_attached)
        {
            case FinalExpansion3Cartridge fe3:
                fe3.ApplyConfigPreset(presetId);
                break;
            case UltimemCartridge um:
                um.ApplyConfigPreset(presetId);
                break;
            case MegaCartCartridge mc:
                mc.ApplyConfigPreset(presetId);
                break;
            default:
                break;
        }
    }

    public byte[]? FlushImage()
    {
        return _attached switch
        {
            FinalExpansion3Cartridge { WriteBack: true, FlashDirty: true } fe3 => fe3.GetFlashImage(),
            UltimemCartridge { WriteBack: true, ImageDirty: true } um => um.GetImage(),
            MegaCartCartridge { WriteBack: true } mc => mc.GetRomImage(),
            _ => null,
        };
    }

    public byte[]? FlushNvram()
    {
        if (_attached is MegaCartCartridge { NvramDirty: true } mc)
        {
            var data = mc.GetNvram();
            mc.ClearNvramDirty();
            return data;
        }

        return null;
    }

    public byte PeekIo(ushort address)
        => _attached?.Peek(address) ?? 0xFF;

    public void PokeIo(ushort address, byte value)
        => _attached?.Write(address, value);

    public bool TryReadMapped(ushort address, out byte value)
    {
        value = 0;
        if (_attached is null || !_attached.HandlesAddress(address))
            return false;
        value = _attached.Read(address);
        return true;
    }

    public bool TryWriteMapped(ushort address, byte value)
    {
        if (_attached is null || !_attached.HandlesAddress(address))
            return false;
        _attached.Write(address, value);
        return true;
    }
}
