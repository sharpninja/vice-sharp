namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Input;
using Xunit;

[Collection("NativeVice")]
public sealed class C64VkmKeyboardTests
{
    public static TheoryData<string, string> VkmKeyboardParityCases
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var profile in C64MachineProfiles.All)
            {
                data.Add(profile.Id, "Space");
                data.Add(profile.Id, "Left");
            }

            return data;
        }
    }

    [Fact]
    public void Load_Gtk3PosVkm_ResolvesRowsColumnsAndShiftFlags()
    {
        var result = C64VkmParser.Load(FindGtk3PosVkm());

        Assert.False(result.HasErrors, FormatDiagnostics(result.Diagnostics));
        Assert.True(result.KeyboardMap.TryResolve("Space", out var space));
        Assert.Equal([0x3C], space.ToArray());
        Assert.True(result.KeyboardMap.TryResolve("D1", out var one));
        Assert.Equal([0x38], one.ToArray());
        Assert.True(result.KeyboardMap.TryResolve("Left", out var left));
        Assert.Equal([0x34, 0x02], left.ToArray());
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == C64VkmDiagnosticSeverity.Warning &&
                diagnostic.Message.Contains("joystick/keypad", StringComparison.OrdinalIgnoreCase) &&
                diagnostic.Message.Contains("KP_7", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Load_SupportsClearIncludeUndefAndInlineComments()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ViceSharpVkmTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var includePath = Path.Combine(directory, "included.vkm");
            var rootPath = Path.Combine(directory, "root.vkm");

            File.WriteAllText(
                includePath,
                """
                B 3 4 8
                Gone 1 1 8
                JoyKey -1 0
                """);
            File.WriteAllText(
                rootPath,
                """
                # root map
                !CLEAR
                A 1 2 8 # inline comment
                !INCLUDE included.vkm
                !UNDEF Gone
                Left 0 2 1
                """);

            var result = C64VkmParser.Load(rootPath);

            Assert.False(result.HasErrors, FormatDiagnostics(result.Diagnostics));
            Assert.True(result.KeyboardMap.TryResolve("A", out var a));
            Assert.Equal([0x0A], a.ToArray());
            Assert.True(result.KeyboardMap.TryResolve("B", out var b));
            Assert.Equal([0x1C], b.ToArray());
            Assert.False(result.KeyboardMap.TryResolve("Gone", out _));
            Assert.True(result.KeyboardMap.TryResolve("Left", out var left));
            Assert.Equal([0x34, 0x02], left.ToArray());
            Assert.Contains(
                result.Diagnostics,
                diagnostic => diagnostic.Message.Contains("joystick/keypad", StringComparison.OrdinalIgnoreCase) &&
                    diagnostic.Message.Contains("JoyKey", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DefaultFallbackMap_PreservesExistingHostKeyMappings()
    {
        Assert.True(C64HostKeyboardMapper.TryMap("Left", out var left));
        Assert.Equal([0x0F, 0x02], left);
        Assert.True(C64HostKeyboardMapper.TryMap("D1", out var one));
        Assert.Equal([0x38], one);
        Assert.True(C64HostKeyboardMapper.TryMap("Space", out var space));
        Assert.Equal([0x3C], space);
    }

    [Fact]
    public void SelectedVkmMap_AppliesCiaMatrixLines()
    {
        var result = C64VkmParser.Load(FindGtk3PosVkm());
        var machine = MachineTestFactory.CreateC64Machine();
        var mapSelection = machine.Devices.All.OfType<IKeyboardInputMapSelection>().Single();
        var keyboardInput = machine.Devices.All.OfType<IMachineKeyboardInput>().Single();

        mapSelection.SelectKeyboardMap(result.KeyboardMap);

        Assert.True(keyboardInput.SetKeyState("Left", pressed: true));
        machine.Bus.Write(0xDC01, 0xFB);
        Assert.Equal(0, machine.Bus.Read(0xDC00) & 0x01);
        machine.Bus.Write(0xDC01, 0xEF);
        Assert.Equal(0, machine.Bus.Read(0xDC00) & 0x40);

        Assert.True(keyboardInput.SetKeyState("Left", pressed: false));
        machine.Bus.Write(0xDC01, 0xFB);
        Assert.Equal(0x01, machine.Bus.Read(0xDC00) & 0x01);
        machine.Bus.Write(0xDC01, 0xEF);
        Assert.Equal(0x40, machine.Bus.Read(0xDC00) & 0x40);
    }

    [ViceTheory]
    [MemberData(nameof(VkmKeyboardParityCases))]
    public void SelectedVkmMap_CiaMatrixScanMatchesNativeX64Sc(string modelSelector, string key)
    {
        var result = C64VkmParser.Load(FindGtk3PosVkm());
        Assert.False(result.HasErrors, FormatDiagnostics(result.Diagnostics));
        Assert.True(result.KeyboardMap.TryResolve(key, out var keyCodes));

        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var mapSelection = machine.Devices.All.OfType<IKeyboardInputMapSelection>().Single();
        var keyboardInput = machine.Devices.All.OfType<IMachineKeyboardInput>().Single();
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        mapSelection.SelectKeyboardMap(result.KeyboardMap);

        try
        {
            ViceNativeBridge.ResetMachine(native);

            foreach (var keyCode in keyCodes)
                SetNativeKeyboardKey(native, keyCode, pressed: true);

            var applied = keyboardInput.SetKeyState(key, pressed: true);
            Assert.Equal(profile.KeyboardEnabled, applied);

            foreach (var keyCode in keyCodes)
                AssertCia1ColumnScanMatchesNative(machine, native, keyCode, expectedPressed: profile.KeyboardEnabled);

            foreach (var keyCode in keyCodes)
                SetNativeKeyboardKey(native, keyCode, pressed: false);

            Assert.Equal(profile.KeyboardEnabled, keyboardInput.SetKeyState(key, pressed: false));

            foreach (var keyCode in keyCodes)
                AssertCia1ColumnScanMatchesNative(machine, native, keyCode, expectedPressed: false);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    private static string FindGtk3PosVkm()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "native", "vice", "vice", "data", "C64", "gtk3_pos.vkm");
            if (File.Exists(path))
                return path;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate native/vice/vice/data/C64/gtk3_pos.vkm from test output path.");
    }

    private static string FormatDiagnostics(IEnumerable<C64VkmDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Path}:{diagnostic.LineNumber}: {diagnostic.Message}"));
    }

    private static void SetNativeKeyboardKey(IntPtr native, byte keyCode, bool pressed)
    {
        var row = keyCode >> 3;
        var column = keyCode & 0x07;
        ViceNativeBridge.SetKeyboardMatrixKey(native, row, column, pressed);
    }

    private static void AssertCia1ColumnScanMatchesNative(
        IMachine machine,
        IntPtr native,
        byte keyCode,
        bool expectedPressed)
    {
        var row = keyCode >> 3;
        var column = keyCode & 0x07;
        var columnMask = (byte)~(1 << column);

        machine.Bus.Write(0xDC02, 0x00);
        machine.Bus.Write(0xDC03, 0xFF);
        machine.Bus.Write(0xDC01, columnMask);

        ViceNativeBridge.StoreCia1Register(native, 0x02, 0x00);
        ViceNativeBridge.StoreCia1Register(native, 0x03, 0xFF);
        ViceNativeBridge.StoreCia1Register(native, 0x01, columnMask);

        var managed = machine.Bus.Read(0xDC00);
        var nativeValue = ViceNativeBridge.ReadCia1Register(native, 0x00);
        var rowBit = 1 << row;

        Assert.Equal(nativeValue, managed);
        Assert.Equal(expectedPressed ? 0 : rowBit, managed & rowBit);
    }
}
