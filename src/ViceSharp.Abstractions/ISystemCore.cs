namespace ViceSharp.Abstractions;

/// <summary>
/// Describes board-level machine policy applied by the architecture builder.
/// </summary>
public interface ISystemCoreDefinition
{
    /// <summary>Stable definition identifier used by profiles and tests.</summary>
    string Id { get; }

    /// <summary>Human-readable core name.</summary>
    string DisplayName { get; }

    /// <summary>Emulator family this system core belongs to.</summary>
    string Family { get; }

    /// <summary>Board wiring policy selected by the machine variant.</summary>
    string BoardPolicy { get; }

    /// <summary>Address decoding or PLA policy selected by the machine variant.</summary>
    string AddressDecoderPolicy { get; }

    /// <summary>Shared bus arbitration and contention policy selected by the machine variant.</summary>
    string BusPolicy { get; }

    /// <summary>True when the core connects the normal keyboard matrix to machine I/O.</summary>
    bool KeyboardMatrixConnected { get; }

    /// <summary>True when the model normally requires cartridge boot wiring.</summary>
    bool CartridgeBootExpected { get; }

    /// <summary>Additional named policy traits for architecture-specific behavior.</summary>
    IReadOnlyDictionary<string, string> Traits { get; }
}

/// <summary>
/// Runtime system core selected by the profile and assembled with chips by the architecture builder.
/// </summary>
public interface ISystemCore : IDevice
{
    /// <summary>Definition that selected this runtime system core.</summary>
    ISystemCoreDefinition Definition { get; }
}
