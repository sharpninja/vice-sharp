using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// Internal post-validation representation of an ad-hoc machine. All numeric
/// values are resolved and all references are checked before this plan is
/// produced.
/// </summary>
internal sealed class AdhocMachinePlan
{
    public required IReadOnlyList<AdhocMemoryRegionPlan> Regions { get; init; }
    public required IReadOnlyList<AdhocChipPlan> Chips { get; init; }
    public required IReadOnlyList<AdhocInterruptLinePlan> InterruptLines { get; init; }
}

internal sealed class AdhocMemoryRegionPlan
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required AdhocMemoryKind Kind { get; init; }
    public required ushort Start { get; init; }
    public required ushort End { get; init; }
}

internal sealed class AdhocChipPlan
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required AdhocChipType Type { get; init; }
    public DeviceRole? Role { get; init; }
    public ushort? BaseAddress { get; init; }
    public string? IrqLineId { get; init; }
    public string? NmiLineId { get; init; }
}

internal sealed class AdhocInterruptLinePlan
{
    public required string Id { get; init; }
    public required InterruptType Type { get; init; }
}

internal enum AdhocMemoryKind
{
    Ram,
    Rom,
}

internal enum AdhocChipType
{
    Mos6502,
    Mos6526,
    Mos6569,
    Sid6581,
}
