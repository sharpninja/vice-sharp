using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.Chips.UserPort;

/// <summary>
/// VIC-1011A RS232 user-port cartridge mapper. Wraps an IBusEndpoint on a
/// <see cref="UserPortInterSystemBus"/> and exposes RS232 logical lines
/// (TXD, RXD, RTS, CTS, DTR, DSR, DCD, RI) backed by the user-port pins.
///
/// Line mapping (matches the VIC-1011A wiring as documented by Commodore):
///   TXD (out, host -> peer)  = PA2
///   RXD (in,  peer -> host)  = PB0
///   RTS (out, host -> peer)  = PB1
///   DTR (out, host -> peer)  = PB2
///   RI  (in,  peer -> host)  = PB3
///   DCD (in,  peer -> host)  = PB4
///   CTS (in,  peer -> host)  = PB6
///   DSR (in,  peer -> host)  = PB7
///
/// Voltage inversion (RS232 +/-12V vs TTL 0/5V) is handled by the host
/// firmware; this mapper is a pure pin remapper. Bit-banged transmit
/// timing tied to CIA2 SP/CNT lines is a later slice.
/// </summary>
public sealed class Vic1011Extension
{
    private readonly IBusEndpoint _userPortEndpoint;

    public Vic1011Extension(IBusEndpoint userPortEndpoint)
    {
        _userPortEndpoint = userPortEndpoint;
    }

    /// <summary>Drive TXD (PA2) to the line state requested (true = idle high / mark).</summary>
    public void WriteTxd(bool high) => _userPortEndpoint.Pull(UserPortInterSystemBus.Pa2, low: !high);

    /// <summary>Drive RTS (PB1) to the line state requested.</summary>
    public void WriteRts(bool high) => _userPortEndpoint.Pull(UserPortInterSystemBus.Pb1, low: !high);

    /// <summary>Drive DTR (PB2) to the line state requested.</summary>
    public void WriteDtr(bool high) => _userPortEndpoint.Pull(UserPortInterSystemBus.Pb2, low: !high);

    /// <summary>Read RXD (PB0) state as seen on the bus (true = high / mark).</summary>
    public bool ReadRxd() => _userPortEndpoint.ReadLine(UserPortInterSystemBus.Pb0);

    /// <summary>Read RI (PB3) state.</summary>
    public bool ReadRi() => _userPortEndpoint.ReadLine(UserPortInterSystemBus.Pb3);

    /// <summary>Read DCD (PB4) state.</summary>
    public bool ReadDcd() => _userPortEndpoint.ReadLine(UserPortInterSystemBus.Pb4);

    /// <summary>Read CTS (PB6) state.</summary>
    public bool ReadCts() => _userPortEndpoint.ReadLine(UserPortInterSystemBus.Pb6);

    /// <summary>Read DSR (PB7) state.</summary>
    public bool ReadDsr() => _userPortEndpoint.ReadLine(UserPortInterSystemBus.Pb7);

    /// <summary>Drive an incoming line from the peer side. Used by tests + peer-link drivers to simulate remote-asserted state on RXD/CTS/etc.</summary>
    public void DriveIncoming(string signal, bool high) => _userPortEndpoint.Pull(signal, low: !high);
}
