namespace ViceSharp.Abstractions;

/// <summary>
/// A fully-assembled emulated machine. Created by IArchitectureBuilder
/// from an IArchitectureDescriptor. Owns the bus, clock, and all
/// registered devices.
/// </summary>
public interface IMachine
{
    /// <summary>The system bus for this machine.</summary>
    IBus Bus { get; }

    /// <summary>The master clock for this machine.</summary>
    IClock Clock { get; }

    /// <summary>Registry of all devices in this machine.</summary>
    IDeviceRegistry Devices { get; }

    /// <summary>The architecture descriptor this machine was built from.</summary>
    IArchitectureDescriptor Architecture { get; }

    /// <summary>Executes one frame (all cycles for one video frame).</summary>
    void RunFrame();

    /// <summary>Executes a single CPU instruction (variable cycle count).</summary>
    void StepInstruction();

    /// <summary>Gets the current full machine state snapshot.</summary>
    MachineState GetState();
}
