namespace ViceSharp.Abstractions;

/// <summary>
/// Common interface for Complex Interface Adapter chips (CIA 6526).
/// </summary>
public interface ICiaChip : IClockedDevice, IInterruptSource
{
    /// <summary>Port A input register</summary>
    byte PortA { get; set; }
    
    /// <summary>Port B input register</summary>
    byte PortB { get; set; }
    
    /// <summary>Port A data direction register</summary>
    byte DdrA { get; set; }
    
    /// <summary>Port B data direction register</summary>
    byte DdrB { get; set; }

    /// <summary>Timer A counter value</summary>
    ushort TimerA { get; set; }
    
    /// <summary>Timer B counter value</summary>
    ushort TimerB { get; set; }
}