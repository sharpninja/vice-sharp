namespace ViceSharp.TestHarness;

using ViceSharp.Core.Input;
using Xunit;

/// <summary>
/// FR-CFG-004 acceptance tests for hotkey files. Iteration 2.
/// Calls <see cref="HotkeyBindings"/> directly. A scan of already loaded
/// assemblies misses the loader when this class runs first.
/// </summary>
public sealed class FrCfg004AcceptanceTests
{
    /// <summary>
    /// FR: FR-CFG-004.
    /// Use case: A hotkey file maps a key to an emulator action.
    /// Acceptance: Loading a file with "reset: F12" returns that mapping.
    /// </summary>
    [Fact]
    public void HotkeyFile_DefinesActionMappings()
    {
        var loaded = Load("reset: F12");

        Assert.NotNull(loaded);
        Assert.Contains("reset", loaded!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("F12", loaded, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-CFG-004.
    /// Use case: The hotkey invokes a host command rather than a UI-local
    /// operation.
    /// Acceptance: Dispatch of the loaded reset mapping records a host
    /// command named reset.
    /// </summary>
    [Fact]
    public void Hotkey_DispatchesAHostCommand()
    {
        var loaded = Load("reset: F12");
        var dispatched = Dispatch(loaded, "F12");
        var machine = MachineTestFactory.CreateC64Machine();
        machine.StepInstruction();
        var moved = machine.GetState().PC;
        var session = new ViceSharp.Host.Runtime.EmulatorRuntimeSession("hotkey", machine.Architecture, machine);
        session.LoadHotkeyText(loaded!);
        var executed = session.ExecuteHotkey("F12", "emulation");
        var vector = (ushort)(machine.Bus.Read(0xFFFC) | (machine.Bus.Read(0xFFFD) << 8));

        Assert.Contains("host:reset", dispatched, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("host:reset", executed);
        Assert.NotEqual(moved, machine.GetState().PC);
        Assert.Equal(vector, machine.GetState().PC);
    }

    /// <summary>
    /// FR: FR-CFG-004.
    /// Use case: One bad line is reported and the other mappings stay.
    /// Acceptance: A file with a bad line and a snapshot line reports the
    /// bad line and still dispatches snapshot.
    /// </summary>
    [Fact]
    public void InvalidDirective_DoesNotDropOtherMappings()
    {
        var loaded = Load("not a directive\nsnapshot: F11");
        var dispatched = Dispatch(loaded, "F11");

        Assert.Contains("not a directive", loaded, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("host:snapshot", dispatched, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(HotkeyBindings.Diagnostics, item => item.Contains("not a directive", StringComparison.Ordinal));
    }

    /// <summary>
    /// FR: FR-CFG-004.
    /// Use case: Monitor text entry is not treated as a hotkey.
    /// Acceptance: The same key in monitor scope does not dispatch.
    /// </summary>
    [Fact]
    public void MonitorScope_DoesNotDispatchTheHotkey()
    {
        var loaded = Load("reset: F12");
        var dispatched = Dispatch(loaded, "F12", scope: "monitor");

        Assert.Equal(string.Empty, dispatched);
    }

    private static string Load(string text)
    {
        return HotkeyBindings.LoadHotkeys(text);
    }

    private static string Dispatch(string? loaded, string key, string scope = "emulation")
    {
        if (loaded is null)
            return string.Empty;

        return HotkeyBindings.DispatchHotkey(loaded, key, scope);
    }
}
