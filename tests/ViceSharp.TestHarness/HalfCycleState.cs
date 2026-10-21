namespace ViceSharp.TestHarness;

/// <summary>
/// The CPU bus access of one half-cycle as the Phase 4 field set names it (PLAN-VICE-IMMUTABLE-001).
/// </summary>
public enum HalfCycleBusAccess
{
    /// <summary>No CPU access on this half-cycle.</summary>
    None,
    /// <summary>The CPU read the bus.</summary>
    Read,
    /// <summary>The CPU wrote the bus.</summary>
    Write
}

/// <summary>
/// One half-cycle of the compared field set (PLAN-VICE-IMMUTABLE-001 Phase 4; PLAN-NATIVERESTORE-001 D2, R13 B):
/// architectural registers, opcode and micro-op phase, the bus event, the interrupt pipeline stages and the BA state.
/// Plain mutable properties so a fixture can build a state by name; no behavior.
/// </summary>
public sealed class HalfCycleState
{
    /// <summary>Half-cycle index since the start of the compared window.</summary>
    public long HalfCycle { get; set; }
    /// <summary>Accumulator.</summary>
    public byte A { get; set; }
    /// <summary>X index register.</summary>
    public byte X { get; set; }
    /// <summary>Y index register.</summary>
    public byte Y { get; set; }
    /// <summary>Stack pointer.</summary>
    public byte S { get; set; }
    /// <summary>Status register.</summary>
    public byte P { get; set; }
    /// <summary>Program counter.</summary>
    public ushort PC { get; set; }
    /// <summary>Opcode of the instruction in progress.</summary>
    public byte Opcode { get; set; }
    /// <summary>Micro-op phase within the instruction.</summary>
    public int MicroOpPhase { get; set; }
    /// <summary>Whether the CPU drove a bus event on this half-cycle.</summary>
    public bool HasBusEvent { get; set; }
    /// <summary>Bus address of the event.</summary>
    public ushort BusAddress { get; set; }
    /// <summary>Bus data of the event.</summary>
    public byte BusData { get; set; }
    /// <summary>Direction of the event.</summary>
    public HalfCycleBusAccess BusAccess { get; set; }
    /// <summary>Whether the event is a dummy access.</summary>
    public bool IsDummy { get; set; }
    /// <summary>IRQ pipeline stage.</summary>
    public uint IrqStage { get; set; }
    /// <summary>NMI pipeline stage.</summary>
    public uint NmiStage { get; set; }
    /// <summary>Stolen-cycle or BA state.</summary>
    public uint BaState { get; set; }
}

/// <summary>
/// The first difference between two half-cycle states: which half-cycle, which field, both values formatted the way the
/// comparator formats them, and a message that names the half-cycle.
/// </summary>
public sealed class HalfCycleMismatch
{
    /// <summary>Half-cycle index of the difference.</summary>
    public long HalfCycle { get; init; }
    /// <summary>Name of the first differing field in the fixed comparison order.</summary>
    public string Field { get; init; } = "";
    /// <summary>Managed value, formatted.</summary>
    public string Managed { get; init; } = "";
    /// <summary>Native value, formatted.</summary>
    public string Native { get; init; } = "";
    /// <summary>Human-readable summary naming the half-cycle, the field and both values.</summary>
    public string Message { get; init; } = "";
}
