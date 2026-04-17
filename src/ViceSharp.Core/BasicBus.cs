using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class BasicBus : IBus
{
    private readonly List<IAddressSpace> _devices = new();

    public byte Read(ushort address)
    {
        foreach (var device in _devices)
        {
            if (device.HandlesAddress(address))
            {
                return device.Read(address);
            }
        }
        return 0xFF;
    }

    public void Write(ushort address, byte value)
    {
        foreach (var device in _devices)
        {
            if (device.HandlesAddress(address))
            {
                device.Write(address, value);
                return;
            }
        }
    }

    public byte Peek(ushort address)
    {
        foreach (var device in _devices)
        {
            if (device.HandlesAddress(address))
            {
                return device.Peek(address);
            }
        }
        return 0xFF;
    }

    public void RegisterDevice(IAddressSpace device)
    {
        _devices.Insert(0, device);
    }

    public void UnregisterDevice(IAddressSpace device)
    {
        _devices.Remove(device);
    }
}