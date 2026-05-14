using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

[Collection("NativeVice")]
public sealed class X64ScVariantLockstepTests
{
    private static string[] RequiredSelectors => C64MachineProfiles.All.Select(profile => profile.Id).ToArray();
    private static string[] NoCartridgeBootSelectors => C64MachineProfiles.All
        .Where(profile => !profile.CartridgeBootExpected)
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

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
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
    [MemberData(nameof(RequiredModelSelectors))]
    public void SelectedRamWindowsMatchNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        using var native = ViceNative.CreateInstance(modelSelector);

        if (profile.Id == "ultimax")
        {
            var cartridge = CreateDeterministicUltimaxCartridge();
            var cartridgePort = machine.Devices.GetAll<ICartridgePort>().Single();
            cartridgePort.AttachCartridge(cartridge, CartridgeMappingMode.Ultimax);
            native.AttachCartridge(cartridge, CartridgeMappingMode.Ultimax);
        }

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
    public void ViciiRasterTimingMatchesNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
        var vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a VIC-II device.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);
            var nativeCycleBaseline = GetNativeVicCycle(native);

            AssertVicRasterTimingMatches(vic, native, nativeCycleBaseline, profile, "reset");

            for (var cycle = 0; cycle < profile.CyclesPerLine; cycle++)
            {
                machine.Clock.Step();
                ViceNativeBridge.StepCycle(native);
            }

            AssertVicRasterTimingMatches(vic, native, nativeCycleBaseline, profile, "first scanline");
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
        var cia2 = machine.Devices.GetByRole(DeviceRole.Cia2) as Mos6526
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose CIA2.");
        var sid = machine.Devices.GetByRole(DeviceRole.AudioChip) as Sid6581
            ?? throw new InvalidOperationException($"Machine '{modelSelector}' did not expose a SID.");
        var native = ViceNativeBridge.CreateMachine(modelSelector);

        try
        {
            machine.Reset();
            ViceNativeBridge.ResetMachine(native);

            AssertCiaCheckpointMatches(cia1, native, 0, modelSelector, "reset");
            AssertCiaCheckpointMatches(cia2, native, 1, modelSelector, "reset");
            AssertSidRegistersMatch(sid, native, modelSelector, "reset");
            AssertInterruptStateMatches(cia1, cia2, native, modelSelector, "reset");

            for (var cycle = 0; cycle < profile.CyclesPerLine; cycle++)
            {
                machine.Clock.Step();
                ViceNativeBridge.StepCycle(native);
            }

            AssertCiaCheckpointMatches(cia1, native, 0, modelSelector, "first scanline");
            AssertCiaCheckpointMatches(cia2, native, 1, modelSelector, "first scanline");
            AssertSidRegistersMatch(sid, native, modelSelector, "first scanline");
            AssertInterruptStateMatches(cia1, cia2, native, modelSelector, "first scanline");
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

    private static void AssertVicRasterTimingMatches(
        Mos6569 vic,
        IntPtr native,
        uint nativeCycleBaseline,
        C64MachineProfile profile,
        string checkpoint)
    {
        var nativeState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(native, ref nativeState);
        var relativeCycle = nativeState.Cycle >= nativeCycleBaseline
            ? nativeState.Cycle - nativeCycleBaseline
            : 0;
        var expectedLine = (ushort)((relativeCycle / profile.CyclesPerLine) % profile.RasterLines);
        var expectedCycle = (byte)(relativeCycle % profile.CyclesPerLine);

        vic.CyclesPerLine.Should().Be(profile.CyclesPerLine, $"{profile.Id} {checkpoint}: profile timing should drive the VIC-II");
        vic.TotalLines.Should().Be(profile.RasterLines, $"{profile.Id} {checkpoint}: profile timing should drive the VIC-II");
        vic.CurrentRasterLine.Should().Be(expectedLine, $"{profile.Id} {checkpoint}: raster line should match native x64sc");
        vic.CurrentCycle.Should().Be(expectedCycle, $"{profile.Id} {checkpoint}: raster cycle should match native x64sc");
        vic.IsBadLine.Should().Be(nativeState.BadLine != 0, $"{profile.Id} {checkpoint}: badline state should match native x64sc");
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

    private static void AssertInterruptStateMatches(
        IInterruptSource irqSource,
        IInterruptSource nmiSource,
        IntPtr native,
        string modelSelector,
        string checkpoint)
    {
        var nativeState = new ViceNativeBridge.ViceInterruptState();
        ViceNativeBridge.GetInterruptState(native, ref nativeState);
        var managedIrqAsserted = irqSource.ConnectedLines
            .Single(line => line.Type == InterruptType.Irq)
            .IsAsserted;
        var managedNmiAsserted = nmiSource.ConnectedLines
            .Single(line => line.Type == InterruptType.Nmi)
            .IsAsserted;

        managedIrqAsserted.Should().Be(
            nativeState.IrqAsserted != 0,
            $"{modelSelector} {checkpoint}: IRQ line assertion should match native x64sc");
        managedNmiAsserted.Should().Be(
            nativeState.NmiAsserted != 0,
            $"{modelSelector} {checkpoint}: NMI line assertion should match native x64sc");
    }

    private static uint GetNativeVicCycle(IntPtr native)
    {
        var state = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(native, ref state);
        return state.Cycle;
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
}
