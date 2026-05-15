using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cartridges;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Input;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

[Collection("NativeVice")]
public sealed class X64ScVariantLockstepTests
{
    private const ushort BasicScreenStart = 0x0400;
    private const int BasicScreenLength = 1000;
    private const int BasicReadyMaxFrames = 180;

    private static string[] RequiredSelectors => C64MachineProfiles.All.Select(profile => profile.Id).ToArray();
    private static string[] NoCartridgeBootSelectors => C64MachineProfiles.All
        .Where(profile => !profile.CartridgeBootExpected)
        .Select(profile => profile.Id)
        .ToArray();
    private static string[] BasicReadyPromptSelectors => C64MachineProfiles.All
        .Where(profile => !profile.CartridgeBootExpected && profile.KeyboardEnabled)
        .Select(profile => profile.Id)
        .ToArray();

    public static TheoryData<string> RequiredModelSelectors
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var selector in RequiredSelectors)
                data.Add(selector);

            return data;
        }
    }

    public static TheoryData<string> NoCartridgeBootModelSelectors
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var selector in NoCartridgeBootSelectors)
                data.Add(selector);

            return data;
        }
    }

    public static TheoryData<string> BasicReadyPromptModelSelectors
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var selector in BasicReadyPromptSelectors)
                data.Add(selector);

            return data;
        }
    }

    public static TheoryData<string, CartridgeMappingMode, int> StandardCartridgeAutostartCases
    {
        get
        {
            var data = new TheoryData<string, CartridgeMappingMode, int>();

            foreach (var selector in NoCartridgeBootSelectors)
            {
                data.Add(selector, CartridgeMappingMode.Standard8K, StandardCartridgeImage.RomBankSize);
                data.Add(selector, CartridgeMappingMode.Standard16K, StandardCartridgeImage.Rom16KSize);
            }

            return data;
        }
    }

    [Fact]
    public void RequiredModelSelectors_CoverEveryDefinedX64ScProfile()
    {
        RequiredSelectors.Should().HaveCount(14);
        RequiredSelectors.Should().BeEquivalentTo(C64MachineProfiles.All.Select(profile => profile.Id));
        C64MachineProfiles.All.Should().OnlyContain(profile => profile.Family == "x64sc");
    }

    [Fact]
    public void NoCartridgeBootSelectors_CoverEveryDeterministicNoCartridgeVariant()
    {
        NoCartridgeBootSelectors.Should().HaveCount(12);
        NoCartridgeBootSelectors.Should().NotContain("ultimax");
        NoCartridgeBootSelectors.Should().NotContain("c64gs");
        C64MachineProfiles.Resolve("ultimax").SystemCore.AddressDecoderPolicy.Should().Be(C64PlaPolicy.Ultimax.ToString());
        C64MachineProfiles.Resolve("c64gs").SystemCore.AddressDecoderPolicy.Should().Be(C64PlaPolicy.CartridgeRequired.ToString());
    }

    [Fact]
    public void BasicReadyPromptSelectors_CoverEveryNoCartridgeKeyboardVariant()
    {
        BasicReadyPromptSelectors.Should().HaveCount(12);
        BasicReadyPromptSelectors.Should().BeEquivalentTo(NoCartridgeBootSelectors);
        BasicReadyPromptSelectors.Should().NotContain("ultimax");
        BasicReadyPromptSelectors.Should().NotContain("c64gs");
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ResetStateMatches_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        using var validator = new LockstepValidator(modelSelector);

        var report = validator.Run(0);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)}");
    }

    [ViceTheory]
    [MemberData(nameof(NoCartridgeBootModelSelectors))]
    public void FirstProfileScanlineMatches_ForEveryDeterministicNoCartridgeX64ScVariant(string modelSelector)
    {
        var cycles = C64MachineProfiles.Resolve(modelSelector).CyclesPerLine;
        using var validator = new LockstepValidator(modelSelector);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [MemberData(nameof(NoCartridgeBootModelSelectors))]
    public void FirstTwoProfileScanlinesMatch_ForEveryDeterministicNoCartridgeX64ScVariant(string modelSelector)
    {
        var cycles = C64MachineProfiles.Resolve(modelSelector).CyclesPerLine * 2;
        using var validator = new LockstepValidator(modelSelector);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [MemberData(nameof(NoCartridgeBootModelSelectors))]
    public void FirstProfileFrameMatches_ForEveryDeterministicNoCartridgeX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines;
        using var validator = new LockstepValidator(modelSelector);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}{Environment.NewLine}{validator.FormatRecentTrace()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [MemberData(nameof(NoCartridgeBootModelSelectors))]
    public void D64Attach_FirstProfileFrameMatches_ForEveryDeterministicNoCartridgeX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines;
        var diskImage = CreateDeterministicD64Image();
        var diskPath = WriteTempD64Image(diskImage);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachDiskToManagedDrive(machine, 8, diskImage);
            ViceNativeBridge.AttachDisk(native, 8, 0, diskPath);

            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            RunCyclesWithCpuLockstep(machine, native, cycles, modelSelector, "D64 attached first frame");
            AssertSelectedRamWindowsMatch(machine, native, modelSelector, "D64 attached first frame");
        }
        finally
        {
            try
            {
                ViceNativeBridge.DetachDisk(native, 8, 0);
            }
            catch
            {
                // Best-effort cleanup; the native machine is destroyed below.
            }

            ViceNativeBridge.DestroyMachine(native);

            if (File.Exists(diskPath))
                File.Delete(diskPath);
        }
    }

    [ViceTheory]
    [MemberData(nameof(BasicReadyPromptModelSelectors))]
    public void HeldSpace_FirstProfileFrameMatches_ForEveryKeyboardEnabledNoCartridgeX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines;
        var result = C64VkmParser.Load(FindGtk3PosVkm());
        result.HasErrors.Should().BeFalse(FormatVkmDiagnostics(result.Diagnostics));
        result.KeyboardMap.TryResolve("Space", out var keyCodes).Should().BeTrue("gtk3_pos.vkm should map Space");

        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            var mapSelection = machine.Devices.GetAll<IKeyboardInputMapSelection>().Single();
            var keyboardInput = machine.Devices.GetAll<IMachineKeyboardInput>().Single();
            mapSelection.SelectKeyboardMap(result.KeyboardMap);

            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            foreach (var keyCode in keyCodes)
                SetNativeKeyboardKey(native, keyCode, pressed: true);

            keyboardInput.SetKeyState("Space", pressed: true).Should().BeTrue($"{modelSelector}: Space should apply to the managed keyboard matrix");

            RunCyclesWithCpuLockstep(machine, native, cycles, modelSelector, "held Space first frame");
            AssertSelectedRamWindowsMatch(machine, native, modelSelector, "held Space first frame");
        }
        finally
        {
            foreach (var keyCode in keyCodes)
            {
                try
                {
                    SetNativeKeyboardKey(native, keyCode, pressed: false);
                }
                catch
                {
                    // Best-effort cleanup; the native machine is destroyed below.
                }
            }

            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [MemberData(nameof(NoCartridgeBootModelSelectors))]
    public void FirstTwoProfileFramesMatch_ForEveryDeterministicNoCartridgeX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines * 2;
        using var validator = new LockstepValidator(modelSelector);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [MemberData(nameof(BasicReadyPromptModelSelectors))]
    public void BasicReadyPromptVisibilityMatchesNative_ForEveryNoCartridgeBasicX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cyclesPerFrame = profile.CyclesPerLine * profile.RasterLines;
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            for (var frame = 1; frame <= BasicReadyMaxFrames; frame++)
            {
                RunCycles(machine, native, cyclesPerFrame);

                var managedReadyOffset = FindBasicReadyPromptOffset(machine);
                var nativeReadyOffset = FindNativeBasicReadyPromptOffset(native);
                managedReadyOffset.HasValue.Should().Be(
                    nativeReadyOffset.HasValue,
                    $"{modelSelector} frame {frame}: BASIC READY visibility should match native x64sc. managed=[{ReadScreenText(machine)}], native=[{ReadNativeScreenText(native)}]");

                if (!managedReadyOffset.HasValue)
                    continue;

                managedReadyOffset.Value.Should().Be(
                    nativeReadyOffset!.Value,
                    $"{modelSelector} frame {frame}: BASIC READY screen offset should match native x64sc");
                return;
            }

            Assert.Fail(
                $"{modelSelector}: BASIC READY prompt not observed within {BasicReadyMaxFrames} frames. managed=[{ReadScreenText(machine)}], native=[{ReadNativeScreenText(native)}]");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [MemberData(nameof(BasicReadyPromptModelSelectors))]
    public void D64Attach_BasicReadyPromptVisibilityMatchesNative_ForEveryNoCartridgeBasicX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cyclesPerFrame = profile.CyclesPerLine * profile.RasterLines;
        var diskImage = CreateDeterministicD64Image();
        var diskPath = WriteTempD64Image(diskImage);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachDiskToManagedDrive(machine, 8, diskImage);
            ViceNativeBridge.AttachDisk(native, 8, 0, diskPath);

            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            for (var frame = 1; frame <= BasicReadyMaxFrames; frame++)
            {
                RunCycles(machine, native, cyclesPerFrame);

                var managedReadyOffset = FindBasicReadyPromptOffset(machine);
                var nativeReadyOffset = FindNativeBasicReadyPromptOffset(native);
                managedReadyOffset.HasValue.Should().Be(
                    nativeReadyOffset.HasValue,
                    $"{modelSelector} D64 attached frame {frame}: BASIC READY visibility should match native x64sc. managed=[{ReadScreenText(machine)}], native=[{ReadNativeScreenText(native)}]");

                if (!managedReadyOffset.HasValue)
                    continue;

                managedReadyOffset.Value.Should().Be(
                    nativeReadyOffset!.Value,
                    $"{modelSelector} D64 attached frame {frame}: BASIC READY screen offset should match native x64sc");
                return;
            }

            Assert.Fail(
                $"{modelSelector}: BASIC READY prompt with D64 attached not observed within {BasicReadyMaxFrames} frames. managed=[{ReadScreenText(machine)}], native=[{ReadNativeScreenText(native)}]");
        }
        finally
        {
            try
            {
                ViceNativeBridge.DetachDisk(native, 8, 0);
            }
            catch
            {
                // Best-effort cleanup; the native machine is destroyed below.
            }

            ViceNativeBridge.DestroyMachine(native);

            if (File.Exists(diskPath))
                File.Delete(diskPath);
        }
    }

    [ViceTheory]
    [MemberData(nameof(StandardCartridgeAutostartCases))]
    public void FirstProfileFrameMatches_ForStandardCartridgeAutostart(
        string modelSelector,
        CartridgeMappingMode mappingMode,
        int imageSize)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines;
        var cartridge = CreateDeterministicStandardCartridge(imageSize);
        using var validator = new LockstepValidator(modelSelector, cartridge, mappingMode);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector} {mappingMode}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}{Environment.NewLine}{validator.FormatRecentTrace()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [MemberData(nameof(StandardCartridgeAutostartCases))]
    public void FirstTwoProfileFramesMatch_ForStandardCartridgeAutostart(
        string modelSelector,
        CartridgeMappingMode mappingMode,
        int imageSize)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines * 2;
        var cartridge = CreateDeterministicStandardCartridge(imageSize);
        using var validator = new LockstepValidator(modelSelector, cartridge, mappingMode);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector} {mappingMode}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}{Environment.NewLine}{validator.FormatRecentTrace()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("ultimax")]
    [InlineData("max")]
    public void FirstProfileScanlineMatches_ForUltimaxWithDeterministicCartridge(string modelSelector)
    {
        var cycles = C64MachineProfiles.Resolve(modelSelector).CyclesPerLine;
        var cartridge = CreateDeterministicUltimaxCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.Ultimax);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("ultimax")]
    [InlineData("max")]
    public void FirstTwoProfileScanlinesMatch_ForUltimaxWithDeterministicCartridge(string modelSelector)
    {
        var cycles = C64MachineProfiles.Resolve(modelSelector).CyclesPerLine * 2;
        var cartridge = CreateDeterministicUltimaxCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.Ultimax);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("ultimax")]
    [InlineData("max")]
    public void FirstProfileFrameMatches_ForUltimaxWithDeterministicCartridge(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines;
        var cartridge = CreateDeterministicUltimaxCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.Ultimax);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("ultimax")]
    [InlineData("max")]
    public void FirstTwoProfileFramesMatch_ForUltimaxWithDeterministicCartridge(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines * 2;
        var cartridge = CreateDeterministicUltimaxCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.Ultimax);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}{Environment.NewLine}{validator.FormatRecentTrace()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("c64gs")]
    [InlineData("gs")]
    public void FirstProfileScanlineMatches_ForC64GsWithDeterministicGameSystemCartridge(string modelSelector)
    {
        var cycles = C64MachineProfiles.Resolve(modelSelector).CyclesPerLine;
        var cartridge = CreateDeterministicGameSystemCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.GameSystem);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("c64gs")]
    [InlineData("gs")]
    public void FirstTwoProfileScanlinesMatch_ForC64GsWithDeterministicGameSystemCartridge(string modelSelector)
    {
        var cycles = C64MachineProfiles.Resolve(modelSelector).CyclesPerLine * 2;
        var cartridge = CreateDeterministicGameSystemCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.GameSystem);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("c64gs")]
    [InlineData("gs")]
    public void FirstProfileFrameMatches_ForC64GsWithDeterministicGameSystemCartridge(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines;
        var cartridge = CreateDeterministicGameSystemCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.GameSystem);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [InlineData("c64gs")]
    [InlineData("gs")]
    public void FirstTwoProfileFramesMatch_ForC64GsWithDeterministicGameSystemCartridge(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines * 2;
        var cartridge = CreateDeterministicGameSystemCartridge();
        using var validator = new LockstepValidator(modelSelector, cartridge, CartridgeMappingMode.GameSystem);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}{Environment.NewLine}{validator.FormatRecentTrace()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void SelectedRamWindowsMatchNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        using var native = ViceNative.CreateInstance(modelSelector);

        AttachRequiredDeterministicCartridge(profile, machine, native);

        machine.Reset();
        native.Reset();

        AssertSelectedRamWindowsMatch(machine, native, modelSelector, "reset");

        for (var cycle = 0; cycle < profile.CyclesPerLine; cycle++)
        {
            machine.Clock.Step();
            native.Step();
        }

        AssertSelectedRamWindowsMatch(machine, native, modelSelector, "first scanline");
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void SelectedRamWindowsMatchNative_AfterFirstProfileFrame_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        using var native = ViceNative.CreateInstance(modelSelector);

        AttachRequiredDeterministicCartridge(profile, machine, native);
        machine.Reset();
        native.Reset();

        RunCycles(machine, native, profile.CyclesPerLine * profile.RasterLines);

        AssertSelectedRamWindowsMatch(machine, native, modelSelector, "first frame");
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ViciiRasterTimingMatchesNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a VIC-II device.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachRequiredDeterministicCartridge(profile, machine, native);
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            AssertVicRasterTimingMatches(vic, native, profile, "reset");

            for (var cycle = 0; cycle < profile.CyclesPerLine; cycle++)
            {
                machine.Clock.Step();
                ViceNativeBridge.StepCycle(native);
            }

            AssertVicRasterTimingMatches(vic, native, profile, "first scanline");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ViciiRegisterCheckpointsMatchNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a VIC-II device.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachRequiredDeterministicCartridge(profile, machine, native);
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            for (var cycle = 0; cycle < profile.CyclesPerLine; cycle++)
            {
                machine.Clock.Step();
                ViceNativeBridge.StepCycle(native);
            }

            AssertViciiRegistersMatch(vic, native, modelSelector, "first scanline");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [InlineData("ultimax")]
    [InlineData("max")]
    public void AbsentCia2IoReadsMatchNative_ForUltimax(string modelSelector)
    {
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);
            machine.Bus.Write(0x0001, 0x37);
            ViceNativeBridge.WriteMemory(native, 0x0001, 0x37);
            machine.Clock.Step();
            ViceNativeBridge.StepCycle(native);

            foreach (var address in Cia2IoProbeAddresses)
            {
                var managedValue = machine.Bus.Read(address);
                var nativeValue = ViceNativeBridge.ReadMemory(native, address);

                managedValue.Should().Be(
                    nativeValue,
                    FormatAbsentCia2Phi1Diagnostic(machine, native, modelSelector, address, 0, managedValue, nativeValue));
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [InlineData("c64")]
    [InlineData("c64gs")]
    public void ConnectedCia2IoWritesAndReadsMatchNative_ForC64Family(string modelSelector)
    {
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);
            machine.Bus.Write(0x0001, 0x37);
            ViceNativeBridge.WriteMemory(native, 0x0001, 0x37);
            machine.Bus.Write(0xDD02, 0xFF);
            ViceNativeBridge.WriteMemory(native, 0xDD02, 0xFF);
            machine.Bus.Write(0xDD00, 0xA5);
            ViceNativeBridge.WriteMemory(native, 0xDD00, 0xA5);

            foreach (var address in Cia2IoProbeAddresses)
            {
                machine.Bus.Read(address).Should().Be(
                    ViceNativeBridge.ReadMemory(native, address),
                    $"{modelSelector} ${address:X4}: connected CIA2 reads should match native x64sc after writes");
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ViciiRasterTimingMatchesNative_AfterFirstProfileFrame_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a VIC-II device.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachRequiredDeterministicCartridge(profile, machine, native);
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            RunCycles(machine, native, profile.CyclesPerLine * profile.RasterLines);

            AssertVicRasterTimingMatches(vic, native, profile, "first frame");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ChipRegisterCheckpointsMatchNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var cia1 = machine.Devices.GetByRole(DeviceRole.Cia1) as Mos6526
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose CIA1.");
        var cia2 = machine.Devices.GetByRole(DeviceRole.Cia2) as Mos6526;
        var sid = machine.Devices.GetByRole(DeviceRole.AudioChip) as Sid6581
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a SID.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachRequiredDeterministicCartridge(profile, machine, native);
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            AssertCiaCheckpointMatches(cia1, native, 0, modelSelector, "reset");
            AssertCia2CheckpointMatchesIfConnected(profile, cia2, native, modelSelector, "reset");
            AssertSidRegistersMatch(sid, native, modelSelector, "reset");
            AssertInterruptStateMatches(cia1, cia2, native, modelSelector, "reset");

            for (var cycle = 0; cycle < profile.CyclesPerLine; cycle++)
            {
                machine.Clock.Step();
                ViceNativeBridge.StepCycle(native);
            }

            AssertCiaCheckpointMatches(cia1, native, 0, modelSelector, "first scanline");
            AssertCia2CheckpointMatchesIfConnected(profile, cia2, native, modelSelector, "first scanline");
            AssertSidRegistersMatch(sid, native, modelSelector, "first scanline");
            AssertInterruptStateMatches(cia1, cia2, native, modelSelector, "first scanline");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ChipRegisterCheckpointsMatchNative_AfterFirstProfileFrame_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var cia1 = machine.Devices.GetByRole(DeviceRole.Cia1) as Mos6526
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose CIA1.");
        var cia2 = machine.Devices.GetByRole(DeviceRole.Cia2) as Mos6526;
        var sid = machine.Devices.GetByRole(DeviceRole.AudioChip) as Sid6581
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a SID.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachRequiredDeterministicCartridge(profile, machine, native);
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            RunCycles(machine, native, profile.CyclesPerLine * profile.RasterLines);

            AssertCiaCheckpointMatches(cia1, native, 0, modelSelector, "first frame");
            AssertCia2CheckpointMatchesIfConnected(profile, cia2, native, modelSelector, "first frame");
            AssertSidRegistersMatch(sid, native, modelSelector, "first frame");
            AssertInterruptStateMatches(cia1, cia2, native, modelSelector, "first frame");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ResetAfterActivityMatchesNative_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines;
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a VIC-II device.");
        var cia1 = machine.Devices.GetByRole(DeviceRole.Cia1) as Mos6526
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose CIA1.");
        var cia2 = machine.Devices.GetByRole(DeviceRole.Cia2) as Mos6526;
        var sid = machine.Devices.GetByRole(DeviceRole.AudioChip) as Sid6581
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a SID.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            AttachRequiredDeterministicCartridge(profile, machine, native);
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            RunCycles(machine, native, cycles);

            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            AssertCpuStateMatches(machine, native, modelSelector, "reset after activity", comparePc: false);
            AssertSelectedRamWindowsMatch(machine, native, modelSelector, "reset after activity");
            AssertVicRasterTimingMatches(vic, native, profile, "reset after activity");
            AssertViciiRegistersMatch(vic, native, modelSelector, "reset after activity");
            AssertCiaCheckpointMatches(cia1, native, 0, modelSelector, "reset after activity");
            AssertCia2CheckpointMatchesIfConnected(profile, cia2, native, modelSelector, "reset after activity");
            AssertSidRegistersMatch(sid, native, modelSelector, "reset after activity");
            AssertInterruptStateMatches(cia1, cia2, native, modelSelector, "reset after activity");

            RunCyclesWithCpuLockstep(machine, native, cycles, modelSelector, "post-reset frame");

            AssertSelectedRamWindowsMatch(machine, native, modelSelector, "post-reset frame");
            AssertVicRasterTimingMatches(vic, native, profile, "post-reset frame");
            AssertViciiRegistersMatch(vic, native, modelSelector, "post-reset frame");
            AssertCiaCheckpointMatches(cia1, native, 0, modelSelector, "post-reset frame");
            AssertCia2CheckpointMatchesIfConnected(profile, cia2, native, modelSelector, "post-reset frame");
            AssertSidRegistersMatch(sid, native, modelSelector, "post-reset frame");
            AssertInterruptStateMatches(cia1, cia2, native, modelSelector, "post-reset frame");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    private static string FormatReport(ValidationReport report)
    {
        if (report.Success || report.Mismatch is null)
            return "No mismatch captured.";

        return
            $"Mismatch at cycle {report.FirstMismatchCycle}: " +
            $"actual cycle={report.Mismatch.Value.Actual.Cycle}, expected cycle={report.Mismatch.Value.Expected.Cycle}; " +
            $"actual [A=${report.Mismatch.Value.Actual.A:X2}, X=${report.Mismatch.Value.Actual.X:X2}, Y=${report.Mismatch.Value.Actual.Y:X2}, S=${report.Mismatch.Value.Actual.S:X2}, P=${report.Mismatch.Value.Actual.P:X2}, PC=${report.Mismatch.Value.Actual.PC:X4}] " +
            $"expected [A=${report.Mismatch.Value.Expected.A:X2}, X=${report.Mismatch.Value.Expected.X:X2}, Y=${report.Mismatch.Value.Expected.Y:X2}, S=${report.Mismatch.Value.Expected.S:X2}, P=${report.Mismatch.Value.Expected.P:X2}, PC=${report.Mismatch.Value.Expected.PC:X4}].";
    }

    private static string FormatVkmDiagnostics(IEnumerable<C64VkmDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Path}:{diagnostic.LineNumber}: {diagnostic.Message}"));
    }

    private static void AssertSelectedRamWindowsMatch(IMachine machine, IViceNative native, string modelSelector, string checkpoint)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("Machine system RAM does not expose IMemory.");
        var mismatches = new List<string>();

        foreach (var address in SelectedRamWindowAddresses)
        {
            var actual = memory.Span[address];
            var expected = native.PeekRam(address);

            if (actual != expected)
                mismatches.Add($"${address:X4}: managed=${actual:X2}, native=${expected:X2}");
        }

        mismatches.Should().BeEmpty($"{modelSelector} {checkpoint}: selected physical RAM windows should match native x64sc");
    }

    private static void AssertSelectedRamWindowsMatch(IMachine machine, IntPtr native, string modelSelector, string checkpoint)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("Machine system RAM does not expose IMemory.");
        var mismatches = new List<string>();

        foreach (var address in SelectedRamWindowAddresses)
        {
            var actual = memory.Span[address];
            var expected = ViceNativeBridge.PeekRam(native, address);

            if (actual != expected)
                mismatches.Add($"${address:X4}: managed=${actual:X2}, native=${expected:X2}");
        }

        mismatches.Should().BeEmpty($"{modelSelector} {checkpoint}: selected physical RAM windows should match native x64sc");
    }

    private static void AssertBasicScreenRamMatchesNative(IMachine machine, IntPtr native, string modelSelector, string checkpoint)
    {
        var managed = ReadScreenCodes(machine);
        var nativeScreen = ReadNativeScreenCodes(native);
        var mismatches = new List<string>();

        for (var offset = 0; offset < managed.Length; offset++)
        {
            if (managed[offset] != nativeScreen[offset])
                mismatches.Add($"${BasicScreenStart + offset:X4}: managed=${managed[offset]:X2}, native=${nativeScreen[offset]:X2}");
        }

        mismatches.Should().BeEmpty($"{modelSelector} {checkpoint}: BASIC screen RAM should match native x64sc");
    }

    private static bool ContainsBasicReadyPrompt(IMachine machine)
        => ContainsBasicReadyPrompt(ReadScreenCodes(machine));

    private static bool ContainsNativeBasicReadyPrompt(IntPtr native)
        => ContainsBasicReadyPrompt(ReadNativeScreenCodes(native));

    private static bool ContainsBasicReadyPrompt(byte[] screenCodes)
        => FindBasicReadyPromptOffset(screenCodes) >= 0;

    private static int? FindBasicReadyPromptOffset(IMachine machine)
    {
        var offset = FindBasicReadyPromptOffset(ReadScreenCodes(machine));
        return offset >= 0 ? offset : null;
    }

    private static int? FindNativeBasicReadyPromptOffset(IntPtr native)
    {
        var offset = FindBasicReadyPromptOffset(ReadNativeScreenCodes(native));
        return offset >= 0 ? offset : null;
    }

    private static int FindBasicReadyPromptOffset(byte[] screenCodes)
    {
        ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
        ReadOnlySpan<byte> asciiReady = "READY"u8;
        var screenCodeOffset = screenCodes.AsSpan().IndexOf(screenCodeReady);
        if (screenCodeOffset >= 0)
            return screenCodeOffset;

        return screenCodes.AsSpan().IndexOf(asciiReady);
    }

    private static byte[] ReadScreenCodes(IMachine machine)
    {
        var buffer = new byte[BasicScreenLength];
        for (var offset = 0; offset < buffer.Length; offset++)
            buffer[offset] = machine.Bus.Peek((ushort)(BasicScreenStart + offset));

        return buffer;
    }

    private static byte[] ReadNativeScreenCodes(IntPtr native)
    {
        var buffer = new byte[BasicScreenLength];
        for (var offset = 0; offset < buffer.Length; offset++)
            buffer[offset] = ViceNativeBridge.PeekRam(native, (ushort)(BasicScreenStart + offset));

        return buffer;
    }

    private static string ReadScreenText(IMachine machine)
        => DecodeScreenText(ReadScreenCodes(machine));

    private static string ReadNativeScreenText(IntPtr native)
        => DecodeScreenText(ReadNativeScreenCodes(native));

    private static string DecodeScreenText(byte[] screenCodes)
    {
        var chars = new char[screenCodes.Length];
        for (var i = 0; i < screenCodes.Length; i++)
            chars[i] = DecodeScreenCode(screenCodes[i]);

        return new string(chars);
    }

    private static char DecodeScreenCode(byte code)
    {
        if (code == 0x20)
            return ' ';

        if (code is >= 1 and <= 26)
            return (char)('A' + code - 1);

        if (code is >= 0x30 and <= 0x39)
            return (char)code;

        return code switch
        {
            0x00 => '@',
            0x2E => '.',
            0x2A => '*',
            0x3A => ':',
            _ => '?'
        };
    }

    private static void AssertCpuStateMatches(
        IMachine machine,
        IntPtr native,
        string modelSelector,
        string checkpoint,
        bool comparePc)
    {
        var managedState = machine.GetState();
        var nativeA = ViceNative.GetA(native);
        var nativeX = ViceNative.GetX(native);
        var nativeY = ViceNative.GetY(native);
        var nativeS = ViceNative.GetS(native);
        var nativeP = ViceNative.GetP(native);
        var nativePc = comparePc ? ViceNative.GetPC(native) : (ushort)0;

        if (managedState.A == nativeA &&
            managedState.X == nativeX &&
            managedState.Y == nativeY &&
            managedState.S == nativeS &&
            managedState.P == nativeP &&
            (!comparePc || managedState.PC == nativePc))
        {
            return;
        }

        managedState.A.Should().Be(nativeA, $"{modelSelector} {checkpoint}: A should match native x64sc");
        managedState.X.Should().Be(nativeX, $"{modelSelector} {checkpoint}: X should match native x64sc");
        managedState.Y.Should().Be(nativeY, $"{modelSelector} {checkpoint}: Y should match native x64sc");
        managedState.S.Should().Be(nativeS, $"{modelSelector} {checkpoint}: stack pointer should match native x64sc");
        managedState.P.Should().Be(nativeP, $"{modelSelector} {checkpoint}: processor status should match native x64sc");

        if (comparePc)
            managedState.PC.Should().Be(nativePc, $"{modelSelector} {checkpoint}: PC should match native x64sc");
    }

    private static void AttachRequiredDeterministicCartridge(C64MachineProfile profile, IMachine machine, IViceNative native)
    {
        if (!TryCreateRequiredDeterministicCartridge(profile, out var cartridge, out var mappingMode))
            return;

        AttachRequiredDeterministicCartridge(machine, cartridge, mappingMode);
        native.AttachCartridge(cartridge, mappingMode);
    }

    private static void AttachRequiredDeterministicCartridge(C64MachineProfile profile, IMachine machine, IntPtr native)
    {
        if (!TryCreateRequiredDeterministicCartridge(profile, out var cartridge, out var mappingMode))
            return;

        AttachRequiredDeterministicCartridge(machine, cartridge, mappingMode);
        ViceNativeBridge.AttachCartridge(native, cartridge, mappingMode);
    }

    private static void AttachRequiredDeterministicCartridge(IMachine machine, byte[] cartridge, CartridgeMappingMode mappingMode)
    {
        var cartridgePort = machine.Devices.GetAll<ICartridgePort>().Single();
        cartridgePort.AttachCartridge(cartridge, mappingMode);
    }

    private static void AttachDiskToManagedDrive(IMachine machine, byte driveNumber, ReadOnlySpan<byte> diskImage)
    {
        var drive = machine.Devices.GetAll<IFloppyDrive>().SingleOrDefault(drive => drive.DriveNumber == driveNumber)
            ?? throw new InvalidOperationException($"Machine does not expose drive {driveNumber}.");

        drive.InsertDisk(diskImage);
    }

    private static void SetNativeKeyboardKey(IntPtr native, byte keyCode, bool pressed)
    {
        var row = keyCode >> 3;
        var column = keyCode & 0x07;
        ViceNativeBridge.SetKeyboardMatrixKey(native, row, column, pressed);
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

    private static byte[] CreateDeterministicD64Image()
    {
        var image = new D64Image();
        image.Format();

        for (var offset = 0; offset < 256; offset++)
            image.WriteSectorByte(18, 1, offset, (byte)(255 - offset));

        return image.ToArray();
    }

    private static string WriteTempD64Image(byte[] diskImage)
    {
        var path = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, diskImage);
        return path;
    }

    private static bool TryCreateRequiredDeterministicCartridge(
        C64MachineProfile profile,
        out byte[] cartridge,
        out CartridgeMappingMode mappingMode)
    {
        if (profile.Id == "ultimax")
        {
            cartridge = CreateDeterministicUltimaxCartridge();
            mappingMode = CartridgeMappingMode.Ultimax;
            return true;
        }

        if (profile.Id == "c64gs")
        {
            cartridge = CreateDeterministicGameSystemCartridge();
            mappingMode = CartridgeMappingMode.GameSystem;
            return true;
        }

        cartridge = Array.Empty<byte>();
        mappingMode = CartridgeMappingMode.Auto;
        return false;
    }

    private static void RunCycles(IMachine machine, IViceNative native, long cycles)
    {
        for (var cycle = 0L; cycle < cycles; cycle++)
        {
            machine.Clock.Step();
            native.Step();
        }
    }

    private static void RunCycles(IMachine machine, IntPtr native, long cycles)
    {
        for (var cycle = 0L; cycle < cycles; cycle++)
        {
            machine.Clock.Step();
            ViceNativeBridge.StepCycle(native);
        }
    }

    private static void RunCyclesWithCpuLockstep(
        IMachine machine,
        IntPtr native,
        long cycles,
        string modelSelector,
        string checkpoint)
    {
        for (var cycle = 0L; cycle < cycles; cycle++)
        {
            machine.Clock.Step();
            ViceNativeBridge.StepCycle(native);
            AssertCpuStateMatches(
                machine,
                native,
                modelSelector,
                $"{checkpoint} cycle {cycle + 1}",
                comparePc: cycle > 1);
        }
    }

    private static void AssertVicRasterTimingMatches(
        Mos6569 vic,
        IntPtr native,
        C64MachineProfile profile,
        string checkpoint)
    {
        var nativeState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(native, ref nativeState);

        vic.CyclesPerLine.Should().Be(profile.CyclesPerLine, $"{profile.Id} {checkpoint}: profile timing should drive the VIC-II");
        vic.TotalLines.Should().Be(profile.RasterLines, $"{profile.Id} {checkpoint}: profile timing should drive the VIC-II");
        vic.CurrentRasterLine.Should().Be(nativeState.RasterLine, $"{profile.Id} {checkpoint}: raster line should match native x64sc");
        vic.CurrentCycle.Should().Be(nativeState.RasterCycle, $"{profile.Id} {checkpoint}: raster cycle should match native x64sc");
        vic.IsBadLine.Should().Be(nativeState.BadLine != 0, $"{profile.Id} {checkpoint}: badline state should match native x64sc");
    }

    private static string FormatAbsentCia2Phi1Diagnostic(
        IMachine machine,
        IntPtr native,
        string modelSelector,
        ushort address,
        int loopCycle,
        byte managedValue,
        byte nativeValue)
    {
        var vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569;
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory;
        var nativeState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(native, ref nativeState);

        var managedVic = vic is null
            ? "managed VIC unavailable"
            : $"managedVic(line={vic.CurrentRasterLine},cycle={vic.CurrentCycle},bank={vic.VicBank},lastPhi1=${vic.LastReadPhi1:X2},d018=${vic.Peek(0xD018):X2})";
        var nativeVic = $"nativeVic(line={nativeState.RasterLine},cycle={nativeState.RasterCycle},d018=${nativeState.Registers[0x18]:X2})";
        var managedPhi1 = vic is null || memory is null
            ? "managedPhi1 unavailable"
            : FormatManagedPhi1Memory(vic, memory);

        return
            $"{modelSelector} ${address:X4} loopCycle={loopCycle}: absent CIA2 reads should follow native x64sc phi1 open bus; " +
            $"managed=${managedValue:X2}, native=${nativeValue:X2}; {managedVic}; {nativeVic}; {managedPhi1}";
    }

    private static string FormatManagedPhi1Memory(Mos6569 vic, IMemory memory)
    {
        var idleAddress = vic.TranslateVicAddress(0x3FFF);
        var pointer3Address = vic.TranslateVicAddress(0x03FB);
        var pointer4Address = vic.TranslateVicAddress(0x03FC);
        var refreshAddress = vic.TranslateVicAddress((ushort)(0x3F00 + (byte)(0xFF - vic.CurrentRasterLine * 5)));

        return
            $"managedPhi1(idle=${idleAddress:X4}:${memory.Span[idleAddress]:X2},ptr3=${pointer3Address:X4}:${memory.Span[pointer3Address]:X2}," +
            $"ptr4=${pointer4Address:X4}:${memory.Span[pointer4Address]:X2},refresh0=${refreshAddress:X4}:${memory.Span[refreshAddress]:X2})";
    }

    private static void AssertCiaCheckpointMatches(
        Mos6526 cia,
        IntPtr native,
        int ciaIndex,
        string modelSelector,
        string checkpoint)
    {
        var nativeState = new ViceNativeBridge.ViceCiaState();
        ViceNativeBridge.GetCiaState(native, ciaIndex, ref nativeState);
        var baseAddress = cia.BaseAddress;

        cia.Read((ushort)(baseAddress + 0x02)).Should().Be(nativeState.DdrA, $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: DDRA should match native x64sc");
        cia.Read((ushort)(baseAddress + 0x03)).Should().Be(nativeState.DdrB, $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: DDRB should match native x64sc");
        cia.Read((ushort)(baseAddress + 0x04)).Should().Be((byte)nativeState.TimerA, $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: timer A low should match native x64sc");
        cia.Read((ushort)(baseAddress + 0x05)).Should().Be((byte)(nativeState.TimerA >> 8), $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: timer A high should match native x64sc");
        cia.Read((ushort)(baseAddress + 0x06)).Should().Be((byte)nativeState.TimerB, $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: timer B low should match native x64sc");
        cia.Read((ushort)(baseAddress + 0x07)).Should().Be((byte)(nativeState.TimerB >> 8), $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: timer B high should match native x64sc");
        cia.Read((ushort)(baseAddress + 0x0E)).Should().Be(nativeState.Cra, $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: CRA should match native x64sc");
        cia.Read((ushort)(baseAddress + 0x0F)).Should().Be(nativeState.Crb, $"{modelSelector} CIA{ciaIndex + 1} {checkpoint}: CRB should match native x64sc");
    }

    private static void AssertCia2CheckpointMatchesIfConnected(
        C64MachineProfile profile,
        Mos6526? cia2,
        IntPtr native,
        string modelSelector,
        string checkpoint)
    {
        if (!profile.SystemCore.Cia2Connected)
        {
            cia2.Should().BeNull($"{modelSelector} {checkpoint}: the Ultimax/MAX system core should not expose CIA2");
            return;
        }

        cia2.Should().NotBeNull($"{modelSelector} {checkpoint}: this x64sc profile should expose CIA2");
        AssertCiaCheckpointMatches(cia2!, native, 1, modelSelector, checkpoint);
    }

    private static void AssertSidRegistersMatch(Sid6581 sid, IntPtr native, string modelSelector, string checkpoint)
    {
        var nativeState = new ViceNativeBridge.ViceSidState();
        ViceNativeBridge.GetSidState(native, ref nativeState);
        var mismatches = new List<string>();

        foreach (var register in SidRegisterCheckpointOffsets)
        {
            var actual = sid.Peek((ushort)(0xD400 + register));
            var expected = nativeState.Registers[register];

            if (actual != expected)
                mismatches.Add($"${0xD400 + register:X4}: managed=${actual:X2}, native=${expected:X2}");
        }

        mismatches.Should().BeEmpty($"{modelSelector} {checkpoint}: SID register checkpoints should match native x64sc");
    }

    private static void AssertViciiRegistersMatch(Mos6569 vic, IntPtr native, string modelSelector, string checkpoint)
    {
        var nativeState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(native, ref nativeState);
        var mismatches = new List<string>();

        foreach (var register in ViciiRegisterCheckpointOffsets)
        {
            var actual = vic.Peek((ushort)(0xD000 + register));
            var expected = nativeState.Registers[register];

            if (actual != expected)
                mismatches.Add($"${0xD000 + register:X4}: managed=${actual:X2}, native=${expected:X2}");
        }

        mismatches.Should().BeEmpty($"{modelSelector} {checkpoint}: VIC-II register checkpoints should match native x64sc");
    }

    private static void AssertInterruptStateMatches(
        IInterruptSource irqSource,
        IInterruptSource? nmiSource,
        IntPtr native,
        string modelSelector,
        string checkpoint)
    {
        var nativeState = new ViceNativeBridge.ViceInterruptState();
        ViceNativeBridge.GetInterruptState(native, ref nativeState);
        var managedIrqAsserted = irqSource.ConnectedLines
            .Single(line => line.Type == InterruptType.Irq)
            .IsAsserted;
        managedIrqAsserted.Should().Be(
            nativeState.IrqAsserted != 0,
            $"{modelSelector} {checkpoint}: IRQ line assertion should match native x64sc");
        if (nmiSource is null)
        {
            nativeState.NmiAsserted.Should().Be(0, $"{modelSelector} {checkpoint}: no CIA2 means native x64sc should not assert NMI");
            return;
        }

        var managedNmiAsserted = nmiSource.ConnectedLines
            .Single(line => line.Type == InterruptType.Nmi)
            .IsAsserted;
        managedNmiAsserted.Should().Be(
            nativeState.NmiAsserted != 0,
            $"{modelSelector} {checkpoint}: NMI line assertion should match native x64sc");
    }

    private static byte[] CreateDeterministicUltimaxCartridge()
    {
        var image = Enumerable.Repeat((byte)0xEA, 0x4000).ToArray();
        image[0x3FF9] = 0x4C;
        image[0x3FFA] = 0x00;
        image[0x3FFB] = 0xE0;
        image[0x3FFC] = 0x00;
        image[0x3FFD] = 0xE0;
        image[0x3FFE] = 0x00;
        image[0x3FFF] = 0xE0;
        return image;
    }

    private static byte[] CreateDeterministicGameSystemCartridge()
    {
        var image = Enumerable.Repeat((byte)0xEA, 0x80000).ToArray();
        image[0x0000] = 0x09;
        image[0x0001] = 0x80;
        image[0x0002] = 0x09;
        image[0x0003] = 0x80;
        image[0x0004] = (byte)'C';
        image[0x0005] = (byte)'B';
        image[0x0006] = (byte)'M';
        image[0x0007] = (byte)'8';
        image[0x0008] = (byte)'0';
        return image;
    }

    private static byte[] CreateDeterministicStandardCartridge(int imageSize)
    {
        if (imageSize is not StandardCartridgeImage.RomBankSize and not StandardCartridgeImage.Rom16KSize)
            throw new ArgumentOutOfRangeException(nameof(imageSize), imageSize, "Standard cartridge lockstep images must be 8K or 16K.");

        var image = Enumerable.Repeat((byte)0xEA, imageSize).ToArray();
        image[0x0000] = 0x09;
        image[0x0001] = 0x80;
        image[0x0002] = 0x09;
        image[0x0003] = 0x80;
        image[0x0004] = (byte)'C';
        image[0x0005] = (byte)'B';
        image[0x0006] = (byte)'M';
        image[0x0007] = (byte)'8';
        image[0x0008] = (byte)'0';

        if (imageSize == StandardCartridgeImage.Rom16KSize)
        {
            image[0x0009] = 0xAD;
            image[0x000A] = 0x00;
            image[0x000B] = 0xA0;
            image[StandardCartridgeImage.RomBankSize] = 0x42;
            image[0x000C] = 0x4C;
            image[0x000D] = 0x09;
            image[0x000E] = 0x80;
        }
        else
        {
            image[0x0009] = 0x4C;
            image[0x000A] = 0x09;
            image[0x000B] = 0x80;
        }

        return image;
    }

    private static readonly ushort[] SelectedRamWindowAddresses =
    [
        0x0002,
        0x0003,
        0x00FE,
        0x00FF,
        0x0100,
        0x0101,
        0x0200,
        0x02A6,
        0x0300,
        0x0314,
        0x0315,
        0x0400,
        0x0401,
        0x0402,
        0x0403,
        0x07E7,
        0x0800,
        0x0801,
        0x0900,
        0x9FFF,
        0xC000,
        0xCFFF
    ];

    private static readonly byte[] SidRegisterCheckpointOffsets =
    [
        0x00,
        0x01,
        0x02,
        0x03,
        0x04,
        0x05,
        0x06,
        0x07,
        0x08,
        0x09,
        0x0A,
        0x0B,
        0x0C,
        0x0D,
        0x0E,
        0x0F,
        0x10,
        0x11,
        0x12,
        0x13,
        0x14,
        0x15,
        0x16,
        0x17,
        0x18
    ];

    private static readonly byte[] ViciiRegisterCheckpointOffsets =
    [
        0x11,
        0x16,
        0x18,
        0x19,
        0x1A,
        0x20,
        0x21,
        0x22,
        0x23,
        0x24,
        0x25,
        0x26
    ];

    private static readonly ushort[] Cia2IoProbeAddresses =
    [
        0xDD00,
        0xDD02,
        0xDD04,
        0xDD0E,
        0xDDFF
    ];
}
