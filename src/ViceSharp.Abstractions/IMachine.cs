using System;
using System.Collections.Generic;

namespace ViceSharp.Abstractions;

/// <summary>
/// One CPU's entry in a machine's per-CPU roster: a display label, that CPU's own
/// executed-cycle counter, and its clock rate (so a per-CPU speed = ExecutedCycles delta
/// over wall time ÷ ClockHz can be derived for the status surface).
/// </summary>
public readonly record struct CpuInfo(string Label, long ExecutedCycles, long ClockHz);

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

    /// <summary>Resets the machine to initial power-on state.</summary>
    void Reset();

    /// <summary>
    /// The machine's pub/sub bus for diagnostic / debug events (CPU instruction boundaries,
    /// memory writes). Null on machines that do not wire one; the time-travel debugger's
    /// tick-history recorder subscribes to it when present.
    /// </summary>
    IPubSub? PubSub => null;

    /// <summary>
    /// This machine's primary CPU - the one whose own executed-cycle rate is the headline
    /// emulation speed (<see cref="ICpu.ExecutedCycles"/> over wall time ÷ its clock). Null on
    /// machines without a CPU. A rig with more than one CPU (a coordinator's host plus each
    /// drive, or the C128's 8502 + Z80) exposes the others through their own systems; this is
    /// the principal one for the status headline.
    /// </summary>
    ICpu? PrimaryCpu => null;

    /// <summary>
    /// The machine's per-CPU roster - one <see cref="CpuInfo"/> per CPU so the status surface
    /// can list each CPU distinctly. A plain single-CPU machine reports just its primary CPU;
    /// a multi-system rig (a coordinator's host plus each drive, or the C128's 8502 + Z80)
    /// overrides this to list the host first then each peripheral CPU. Empty on a machine with
    /// no CPU.
    /// </summary>
    IReadOnlyList<CpuInfo> CpuInfos => PrimaryCpu is { } cpu
        ? new[] { new CpuInfo(Architecture.MachineName, cpu.ExecutedCycles, Clock.FrequencyHz) }
        : Array.Empty<CpuInfo>();
}
