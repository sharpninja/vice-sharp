namespace ViceSharp.TestHarness.Vic20;

using System.Reflection;
using Xunit;

/// <summary>
/// FR-SNP-005 AC3 and TEST-SNP-002 AC4 (the state-level comparator; the managed TR-CYCLE-001 AC4 evidence
/// belongs to Phase E and is not claimed here: PLAN-NATIVERESTORE-001 D2, round 3b F10).
/// Acceptance: the comparator reports the first differing field with the exact half-cycle
/// even when the architectural registers match.
/// </summary>
public sealed class Vic20HalfCycleComparatorTests
{
    /// <summary>
    /// Phase 3 Slice 3c red fixture: equal registers cannot hide a different
    /// bus address. The existing <see cref="LockstepValidator"/> owns the only
    /// comparator and must identify the exact half-cycle and differing values.
    /// </summary>
    [Fact]
    public void RegistersMatch_BusAddressDiffers_ReportsExactHalfCycle()
    {
        var stateType = typeof(LockstepValidator).Assembly.GetType(
            "ViceSharp.TestHarness.HalfCycleState");
        Assert.NotNull(stateType);

        var compare = typeof(LockstepValidator).GetMethod(
            "CompareHalfCycleStates",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(compare);

        var managed = CreateState(stateType!, busAddress: 0x1000);
        var native = CreateState(stateType!, busAddress: 0x1001);

        var mismatch = compare!.Invoke(null, [managed, native]);

        Assert.NotNull(mismatch);
        Assert.Equal(37L, GetProperty<long>(mismatch!, "HalfCycle"));
        Assert.Equal("BusAddress", GetProperty<string>(mismatch!, "Field"));
        Assert.Equal("$1000", GetProperty<string>(mismatch!, "Managed"));
        Assert.Equal("$1001", GetProperty<string>(mismatch!, "Native"));
        Assert.Contains(
            "half-cycle 37",
            GetProperty<string>(mismatch!, "Message"),
            StringComparison.OrdinalIgnoreCase);
    }

    private static object CreateState(Type stateType, ushort busAddress)
    {
        var state = Activator.CreateInstance(stateType);
        Assert.NotNull(state);

        SetProperty(state!, "HalfCycle", 37L);
        SetProperty(state!, "A", (byte)0x11);
        SetProperty(state!, "X", (byte)0x22);
        SetProperty(state!, "Y", (byte)0x33);
        SetProperty(state!, "S", (byte)0xF8);
        SetProperty(state!, "P", (byte)0x24);
        SetProperty(state!, "PC", (ushort)0xC123);
        SetProperty(state!, "Opcode", (byte)0xAD);
        SetProperty(state!, "MicroOpPhase", 2);
        SetProperty(state!, "HasBusEvent", true);
        SetProperty(state!, "BusAddress", busAddress);
        SetProperty(state!, "BusData", (byte)0x5A);
        SetEnumProperty(state!, "BusAccess", "Read");
        SetProperty(state!, "IsDummy", false);
        SetProperty(state!, "IrqStage", 0u);
        SetProperty(state!, "NmiStage", 0u);
        SetProperty(state!, "BaState", 0u);
        return state!;
    }

    private static void SetProperty(object target, string name, object value)
    {
        var property = target.GetType().GetProperty(name);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    private static void SetEnumProperty(object target, string name, string value)
    {
        var property = target.GetType().GetProperty(name);
        Assert.NotNull(property);
        property!.SetValue(target, Enum.Parse(property.PropertyType, value));
    }

    private static T GetProperty<T>(object target, string name)
    {
        var property = target.GetType().GetProperty(name);
        Assert.NotNull(property);
        return Assert.IsType<T>(property!.GetValue(target));
    }
}
