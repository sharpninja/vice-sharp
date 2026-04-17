namespace ViceSharp.Abstractions;

/// <summary>
/// Serial port interface for RS-232 communication.
/// </summary>
public interface ISerialPort
{
    /// <summary>Data bits per character</summary>
    int DataBits { get; set; }
    
    /// <summary>Baud rate in bits per second</summary>
    int BaudRate { get; set; }

    /// <summary>Write a single byte to the serial port</summary>
    void WriteByte(byte value);
    
    /// <summary>Read a single byte from the serial port</summary>
    byte ReadByte();
}