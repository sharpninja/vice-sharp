using System.Globalization;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 (R13 B) and PLAN-VICE-IMMUTABLE-001 task 5: the state-level half-cycle comparator the
/// Vic20HalfCycleComparatorTests fixture names. It compares every field of the Phase 4 set in a fixed order and
/// reports the first difference with the exact half-cycle and both values. Binding this comparator to real observer
/// traces is Phase E item 1; nothing here touches a native library.
/// </summary>
public sealed partial class LockstepValidator
{
    private static readonly (string Name, Func<HalfCycleState, object> Get)[] HalfCycleFields =
    [
        ("A", s => s.A), ("X", s => s.X), ("Y", s => s.Y), ("S", s => s.S), ("P", s => s.P), ("PC", s => s.PC),
        ("Opcode", s => s.Opcode), ("MicroOpPhase", s => s.MicroOpPhase), ("HasBusEvent", s => s.HasBusEvent),
        ("BusAddress", s => s.BusAddress), ("BusData", s => s.BusData), ("BusAccess", s => s.BusAccess),
        ("IsDummy", s => s.IsDummy), ("IrqStage", s => s.IrqStage), ("NmiStage", s => s.NmiStage), ("BaState", s => s.BaState)
    ];

    /// <summary>
    /// Compares two states field by field in the fixed Phase 4 order and returns the first difference, or null when
    /// every field is equal. The half-cycle index of the mismatch is the managed state's.
    /// </summary>
    public static HalfCycleMismatch? CompareHalfCycleStates(HalfCycleState managed, HalfCycleState native)
    {
        ArgumentNullException.ThrowIfNull(managed);
        ArgumentNullException.ThrowIfNull(native);
        foreach (var (name, get) in HalfCycleFields)
        {
            var left = get(managed);
            var right = get(native);
            if (Equals(left, right)) continue;
            var leftText = FormatHalfCycleValue(left);
            var rightText = FormatHalfCycleValue(right);
            return new HalfCycleMismatch
            {
                HalfCycle = managed.HalfCycle,
                Field = name,
                Managed = leftText,
                Native = rightText,
                Message = string.Create(CultureInfo.InvariantCulture, $"Half-cycle {managed.HalfCycle}: {name} differs (managed {leftText}, native {rightText}).")
            };
        }
        return null;
    }

    private static string FormatHalfCycleValue(object value) => value switch
    {
        ushort u => "$" + u.ToString("X4", CultureInfo.InvariantCulture),
        byte b => "$" + b.ToString("X2", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        Enum e => e.ToString(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };
}
