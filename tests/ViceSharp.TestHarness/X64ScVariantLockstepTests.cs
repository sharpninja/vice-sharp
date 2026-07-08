using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core.Input;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using ViceSharp.RomFetch;
using System.Reflection;
using Xunit;

namespace ViceSharp.TestHarness;

[Collection("NativeVice")]
public sealed class X64ScVariantLockstepTests
{
    private const ushort BasicScreenStart = 0x0400;
    private const int BasicScreenLength = 1000;
    private const int BasicReadyMaxFrames = 180;
    private const int SelectedD64LockstepSeconds = 30;
    private const string SelectedD64EnvironmentVariable = "VICESHARP_SELECTED_D64";
    private const string SelectedD64RunGateEnvironmentVariable = "VICESHARP_RUN_SELECTED_D64";
    private const string SelectedD64DefaultFileName = "frostpoint.d64";
    private static readonly string[] RepositoryD64FixtureFileNames =
    [
        "Elise.d64",
        "frostpoint.d64",
        "pieces_of_light.d64",
    ];

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

    /// <summary>
    /// FR: FR-Architectures-C64-x64sc, TR: TR-X64SC-PROFILE-COVERAGE.
    /// Use case: The x64sc lockstep harness drives every shipped C64
    /// variant; the master selector set must therefore enumerate every
    /// profile in <see cref="C64MachineProfiles.All"/> and nothing else.
    /// Acceptance: Selector list has 14 entries, matches the profile id
    /// set exactly, and every profile reports family <c>x64sc</c>.
    /// </summary>
    [Fact]
    public void RequiredModelSelectors_CoverEveryDefinedX64ScProfile()
    {
        RequiredSelectors.Should().HaveCount(14);
        RequiredSelectors.Should().BeEquivalentTo(C64MachineProfiles.All.Select(profile => profile.Id));
        C64MachineProfiles.All.Should().OnlyContain(profile => profile.Family == "x64sc");
    }

    /// <summary>
    /// FR: FR-Architectures-C64-x64sc, TR: TR-X64SC-NOCART-BOOT.
    /// Use case: Variants that require a cartridge image to boot (Ultimax,
    /// C64GS) must be excluded from the "no-cartridge boot" harness so
    /// the gate never tries to drive them past the reset vector without
    /// an image attached.
    /// Acceptance: The selector list contains 12 variants, omits the
    /// cartridge-required selectors, and the PLA policies for the omitted
    /// variants match the documented Ultimax / CartridgeRequired modes.
    /// </summary>
    [Fact]
    public void NoCartridgeBootSelectors_CoverEveryDeterministicNoCartridgeVariant()
    {
        NoCartridgeBootSelectors.Should().HaveCount(12);
        NoCartridgeBootSelectors.Should().NotContain("ultimax");
        NoCartridgeBootSelectors.Should().NotContain("c64gs");
        C64MachineProfiles.Resolve("ultimax").SystemCore.AddressDecoderPolicy.Should().Be(C64PlaPolicy.Ultimax.ToString());
        C64MachineProfiles.Resolve("c64gs").SystemCore.AddressDecoderPolicy.Should().Be(C64PlaPolicy.CartridgeRequired.ToString());
    }

    /// <summary>
    /// FR: FR-Architectures-C64-x64sc, TR: TR-X64SC-PROFILE-COVERAGE.
    /// Use case: The BASIC READY prompt parity gate only applies to
    /// keyboard-capable variants without a cartridge; the selector set
    /// must mirror the no-cartridge boot list and exclude ultimax/c64gs.
    /// Acceptance: 12 selectors, identical to the no-cartridge boot set,
    /// none of which is "ultimax" or "c64gs".
    /// </summary>
    [Fact]
    public void BasicReadyPromptSelectors_CoverEveryNoCartridgeKeyboardVariant()
    {
        BasicReadyPromptSelectors.Should().HaveCount(12);
        BasicReadyPromptSelectors.Should().BeEquivalentTo(NoCartridgeBootSelectors);
        BasicReadyPromptSelectors.Should().NotContain("ultimax");
        BasicReadyPromptSelectors.Should().NotContain("c64gs");
    }

    /// <summary>
    /// FR: FR-PRF-001, TR: TR-CYCLE-001.
    /// Use case: Every required x64sc variant must produce the same
    /// post-reset state as native VICE x64sc when both are reset.
    /// Acceptance: <see cref="LockstepValidator.Run"/> reports Success
    /// after 0 cycles for every variant id.
    /// </summary>
    [ViceTheory]
    [MemberData(nameof(RequiredModelSelectors))]
    public void ResetStateMatches_ForEveryRequiredX64ScVariant(string modelSelector)
    {
        using var validator = new LockstepValidator(modelSelector);

        var report = validator.Run(0);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)}");
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Drive each no-cartridge x64sc variant cycle-by-cycle for
    /// one full profile-defined scanline; managed and native state must
    /// remain identical the whole way.
    /// Acceptance: <see cref="LockstepValidator.Run"/> reports Success
    /// and TotalCyclesExecuted equals the profile's CyclesPerLine.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Extend the first-scanline gate to two consecutive scan
    /// lines so the harness covers the wrap from the last cycle of line
    /// 0 into the first cycle of line 1 for every variant.
    /// Acceptance: Success reported after CyclesPerLine*2 cycles and
    /// TotalCyclesExecuted equals that target.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: For each no-cartridge x64sc variant, the lockstep
    /// validator runs an entire profile-defined PAL/NTSC frame and
    /// asserts cycle-by-cycle parity with native VICE.
    /// Acceptance: Validator reports Success after exactly
    /// CyclesPerLine*RasterLines cycles, with a full trace logged on
    /// failure.
    /// </summary>
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

    /// <summary>
    /// FR: FR-DRV-001, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Attach a deterministic D64 image to the managed drive
    /// and to native VICE's IEC drive, then run a full profile frame to
    /// prove drive emulation drift does not desynchronise CPU/VIC state.
    /// Acceptance: After resetting both machines with the disk attached
    /// and running CyclesPerLine*RasterLines cycles, RAM windows still
    /// match between managed and native machines.
    /// </summary>
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
            ViceNativeBridge.ResetMachine(native);
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

    /// <summary>
    /// FR: FR-INP-001, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Hold the Space key on both managed and native machines
    /// for one full frame and verify the keyboard matrix drive does not
    /// perturb cycle-accurate lockstep.
    /// Acceptance: RAM windows match between managed and native after
    /// CyclesPerLine*RasterLines cycles with Space pressed.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Extend frame-level lockstep coverage across two
    /// consecutive frames to expose any drift accumulated during the
    /// vertical blank/wrap transitions.
    /// Acceptance: Validator reports Success after exactly
    /// CyclesPerLine*RasterLines*2 cycles for every no-cartridge variant.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-CPU-002, TR: TR-CYCLE-001, BACKFILL-LOCKSTEP-001.
    /// Use case: Extend frame-level lockstep coverage across ten consecutive
    /// PAL frames to expose drift that only accumulates over many vertical
    /// blank wraps and CIA timer cycles. This is the Phase 1 slice 7A depth
    /// gate: any divergence within 10 frames blocks Phase 1 close.
    /// Acceptance: Validator reports Success after exactly
    /// CyclesPerLine*RasterLines*10 cycles for every no-cartridge variant.
    /// </summary>
    [ViceTheory]
    [MemberData(nameof(NoCartridgeBootModelSelectors))]
    public void FirstTenProfileFramesMatch_ForEveryDeterministicNoCartridgeX64ScVariant(string modelSelector)
    {
        var profile = C64MachineProfiles.Resolve(modelSelector);
        var cycles = profile.CyclesPerLine * profile.RasterLines * 10;
        using var validator = new LockstepValidator(modelSelector);

        var report = validator.Run(cycles);

        report.Success.Should().BeTrue($"{modelSelector}: {FormatReport(report)} {validator.FormatRamtasDiagnostic()}");
        report.TotalCyclesExecuted.Should().Be(cycles);
    }

    /// <summary>
    /// FR: BACKFILL-LOCKSTEP-001, FR: ARCH-TRUEDRIVE-1541-002, TR: TR-CYCLE-001.
    /// Use case: D64 media used as parity-test inputs must be committed with the
    /// test harness rather than depending on a developer's Downloads directory.
    /// Acceptance: Every repository D64 fixture exists and is a canonical
    /// 35-track 174,848-byte disk image.
    /// </summary>
    [Fact]
    public void RepositoryD64Fixtures_ArePresentAndCanonical35TrackImages()
    {
        foreach (var fileName in RepositoryD64FixtureFileNames)
        {
            var path = ResolveRepoD64FixturePath(fileName);

            File.Exists(path).Should().BeTrue($"repository D64 fixture {fileName} should exist at {path}");
            new FileInfo(path).Length.Should().Be(
                D64Image.DiskSize35Track,
                $"{fileName} should be a canonical 35-track D64 test artifact");
        }
    }

    /// <summary>
    /// FR: BACKFILL-LOCKSTEP-001, FR: ARCH-TRUEDRIVE-1541-002, TR: TR-CYCLE-001.
    /// Use case: The final selected-media x64sc gate must keep the managed
    /// PAL C64 in CPU lockstep with native VICE for a full 30 seconds while
    /// the selected D64 image is attached to drive 8 on both machines.
    /// Acceptance: With <c>VICESHARP_SELECTED_D64</c> explicitly pointing at a
    /// selected image, or <c>VICESHARP_RUN_SELECTED_D64=1</c> enabling the
    /// repository <c>frostpoint.d64</c> fixture, the validator reports success
    /// after exactly NominalClockHz*30 cycles with zero skipped coverage in
    /// this environment.
    /// </summary>
    [ViceFact]
    public void SelectedD64_PostKernalCloseStateMatchesNative_ForC64Pal()
    {
        var selectedD64Path = ResolveSelectedD64Path();
        if (selectedD64Path is null)
        {
            Assert.Skip(
                $"Set {SelectedD64EnvironmentVariable} to a selected D64 image, or set {SelectedD64RunGateEnvironmentVariable}=1 to run this long selected-media gate against the repository fixture.");
        }

        var profile = C64MachineProfiles.C64Pal;
        var cycles = profile.NominalClockHz * SelectedD64LockstepSeconds;
        var diskImage = File.ReadAllBytes(selectedD64Path);
        diskImage.Should().HaveCount(D64Image.DiskSize35Track, "selected D64 image must be a canonical 35-track D64");

        using var validator = new LockstepValidator(
            profile.Id,
            diskImage: diskImage,
            diskPath: selectedD64Path,
            recordRecentTrace: true);
        validator.QueueC64Drive8LoadCommand(profile.CyclesPerLine * profile.RasterLines);

        var report = validator.RunUntilKernalCloseStableAndSearchNative(cycles, profile.NominalClockHz);

        report.Success.Should().BeTrue(
            $"{profile.Id} selected D64 '{selectedD64Path}' should converge after the managed KERNAL CLOSE routine returns: " +
            $"{report} {validator.FormatRamtasDiagnostic()}{Environment.NewLine}{report.Trace}");
    }

    /// <summary>
    /// FR: FR-CPU-001, FR: FR-VIC-002, TR: TR-CYCLE-001.
    /// Use case: For each basic-capable variant, both managed and native
    /// machines should display the "READY." prompt at the same frame and
    /// the same screen offset after boot.
    /// Acceptance: Within <c>BasicReadyMaxFrames</c> frames, the managed
    /// BASIC READY offset matches the native offset; if the prompt never
    /// appears or appears at a different offset the test fails with both
    /// rendered screens dumped in the message.
    /// </summary>
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

    /// <summary>
    /// FR: FR-DRV-001, FR: FR-CPU-001, FR: FR-VIC-002, TR: TR-CYCLE-001.
    /// Use case: Same BASIC READY prompt parity check as the standalone
    /// variant, but with a deterministic D64 attached to drive 8 to
    /// surface any drive emulation drift visible on the screen.
    /// Acceptance: Within <c>BasicReadyMaxFrames</c>, managed and native
    /// machines display READY at the same offset; otherwise both screen
    /// dumps are surfaced in the failure message.
    /// </summary>
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
            ViceNativeBridge.ResetMachine(native);
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Autostart an 8K or 16K standard cartridge image on each
    /// cartridge-capable variant; the managed and native frames must
    /// match cycle-for-cycle across the first profile frame.
    /// Acceptance: Validator reports Success after one full frame for
    /// every (selector, mapping mode, size) combination.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Continue the standard cartridge autostart parity check
    /// across two consecutive frames to expose drift in cartridge bank
    /// or memory routing on the frame boundary.
    /// Acceptance: Validator reports Success after exactly two profile
    /// frames for every (selector, mapping mode, size) combination.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-MEM-003, TR: TR-CYCLE-001.
    /// Use case: Ultimax/max variants depend on the cartridge for ROM
    /// visibility; with a deterministic Ultimax cartridge attached, the
    /// first scanline must match native VICE.
    /// Acceptance: Validator runs CyclesPerLine cycles, reports Success,
    /// and TotalCyclesExecuted equals CyclesPerLine.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-MEM-003, TR: TR-CYCLE-001.
    /// Use case: Continue the Ultimax cartridge parity check across two
    /// scanlines so the wrap from cycle N-1 of line 0 into cycle 0 of
    /// line 1 stays cycle-accurate.
    /// Acceptance: Validator reports Success after CyclesPerLine*2
    /// cycles for both "ultimax" and "max" selectors.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-MEM-003, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Run a full profile frame on the Ultimax variants with
    /// the deterministic Ultimax cartridge image so both ROMH and ROML
    /// visibility (the Ultimax PLA policy) stay synchronised.
    /// Acceptance: Validator reports Success after exactly one full
    /// profile frame for "ultimax" and "max".
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-MEM-003, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Extend the Ultimax cartridge parity check across two
    /// frames so any drift accumulated across vertical blank is caught.
    /// Acceptance: Validator reports Success after CyclesPerLine *
    /// RasterLines * 2 cycles for both "ultimax" and "max".
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-PRF-002, TR: TR-CYCLE-001.
    /// Use case: C64GS variants only boot from a Game System cartridge;
    /// with a deterministic image attached, the first profile scanline
    /// must match native x64sc gs.
    /// Acceptance: Validator reports Success after exactly
    /// CyclesPerLine cycles for "c64gs" and "gs".
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-PRF-002, TR: TR-CYCLE-001.
    /// Use case: Continue the C64GS Game System cartridge parity check
    /// across two scanlines to expose line-wrap drift.
    /// Acceptance: Validator reports Success after CyclesPerLine*2
    /// cycles for both "c64gs" and "gs".
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-PRF-002, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Run a full profile frame on the C64GS variants with the
    /// deterministic Game System cartridge.
    /// Acceptance: Validator reports Success after exactly
    /// CyclesPerLine*RasterLines cycles for "c64gs" and "gs".
    /// </summary>
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

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-PRF-002, FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Cover two consecutive frames on C64GS variants to catch
    /// drift accumulated across vertical blank.
    /// Acceptance: Validator reports Success after CyclesPerLine *
    /// RasterLines * 2 cycles for both "c64gs" and "gs".
    /// </summary>
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

    /// <summary>
    /// FR: FR-MEM-001, TR: TR-CYCLE-001.
    /// Use case: For every required x64sc variant, after one scanline
    /// the selected RAM windows (zero page, stack, color RAM, etc.)
    /// must match native VICE byte-for-byte.
    /// Acceptance: <c>AssertSelectedRamWindowsMatch</c> succeeds after
    /// reset and again after running CyclesPerLine cycles in lockstep.
    /// </summary>
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

    /// <summary>
    /// FR: FR-MEM-001, TR: TR-CYCLE-001.
    /// Use case: Extend the RAM window parity check to one full profile
    /// frame for every required variant.
    /// Acceptance: <c>AssertSelectedRamWindowsMatch</c> succeeds after
    /// reset and after running CyclesPerLine*RasterLines cycles.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-VIC-006, TR: TR-CYCLE-001.
    /// Use case: The VIC-II raster line/cycle/badline-flag triplet must
    /// agree with native VICE at every cycle of the first scanline for
    /// every required variant.
    /// Acceptance: <c>AssertVicRasterTimingMatches</c> succeeds at reset
    /// and after CyclesPerLine cycles.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Beyond timing fields, the entire VIC-II register file
    /// readback after the first scanline must match native VICE for
    /// every required variant.
    /// Acceptance: <c>AssertViciiRegistersMatch</c> succeeds after
    /// running CyclesPerLine cycles in lockstep.
    /// </summary>
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

    /// <summary>
    /// FR: FR-MEM-003, FR: FR-CIA-006, TR: TR-CYCLE-001.
    /// Use case: Ultimax variants disconnect CIA2; reads to $DD00-$DDFF
    /// must return open-bus values that match native VICE.
    /// Acceptance: For each probed CIA2 address, managed read equals
    /// native read; on mismatch the failure includes a phi1 diagnostic.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CIA-006, FR: FR-MEM-001, TR: TR-CYCLE-001.
    /// Use case: On standard C64 family variants CIA2 is connected; the
    /// managed memory map must route writes and reads to $DD00-$DDFF to
    /// the CIA register file exactly as native VICE does.
    /// Acceptance: After identical writes to $DD02 and $DD00 on both
    /// machines, every probed CIA2 address reads back the same byte
    /// from managed and native.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-VIC-006, TR: TR-CYCLE-001.
    /// Use case: Extend VIC-II raster timing parity to a full profile
    /// frame for every required variant.
    /// Acceptance: <c>AssertVicRasterTimingMatches</c> succeeds after
    /// CyclesPerLine*RasterLines cycles in lockstep.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CIA-001, FR: FR-SID-001, TR: TR-CYCLE-001.
    /// Use case: After the first profile scanline, the register state
    /// of CIA1, CIA2 (when connected) and SID must match native VICE,
    /// along with the IRQ/NMI dispatcher state.
    /// Acceptance: All four checkpoint helpers (CIA1, CIA2, SID,
    /// interrupt state) succeed both at reset and after CyclesPerLine
    /// cycles of lockstep.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CIA-001, FR: FR-SID-001, TR: TR-CYCLE-001.
    /// Use case: Extend the chip register checkpoint suite to one full
    /// profile frame for every required variant.
    /// Acceptance: CIA1, CIA2 (if connected), SID, and interrupt-state
    /// helpers all succeed after CyclesPerLine*RasterLines cycles.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CPU-001, FR: FR-VIC-001, FR: FR-CIA-001, TR: TR-CYCLE-001.
    /// Use case: Run a full frame of activity, then reset both machines
    /// and verify all chip-level state (VIC, CIA1, CIA2 if connected,
    /// SID) and CPU state return to a single, native-matching baseline.
    /// Acceptance: After running and resetting, every chip checkpoint
    /// and the CPU register snapshot match native VICE for every
    /// required variant.
    /// </summary>
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
        return ViceDataPathResolver.FindDataFile("C64", "gtk3_pos.vkm");
    }

    private static byte[] CreateDeterministicD64Image()
    {
        var image = new D64Image();
        image.Format();

        var directory = image.GetSector(18, 1);
        directory[0] = 0x00;
        directory[1] = 0xFF;
        directory[2] = 0x82;
        directory[3] = 17;
        directory[4] = 0;
        "LOCKSTEP"u8.CopyTo(directory[5..]);
        directory.Slice(13, 8).Fill(0xA0);

        var file = image.GetSector(17, 0);
        file[0] = 0x00;
        file[1] = 0xFF;
        for (var offset = 0; offset < 256; offset++)
            file[offset] = (byte)(255 - offset);
        file[0] = 0x00;
        file[1] = 0xFF;

        return image.ToArray();
    }

    private static string WriteTempD64Image(byte[] diskImage)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "d64-attach-temp");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"vice-sharp-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, diskImage);
        return path;
    }

    private static string? ResolveSelectedD64Path()
    {
        var configuredPath = Environment.GetEnvironmentVariable(SelectedD64EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = Environment.ExpandEnvironmentVariables(configuredPath);
            File.Exists(configuredPath).Should().BeTrue(
                $"{SelectedD64EnvironmentVariable} should point at an existing selected D64 image");
            return configuredPath;
        }

        if (!SelectedD64RunGateEnabled())
            return null;

        var fixturePath = ResolveRepoD64FixturePath(SelectedD64DefaultFileName);
        File.Exists(fixturePath).Should().BeTrue(
            $"{SelectedD64DefaultFileName} should be committed under the repository D64 fixture directory");
        return fixturePath;
    }

    private static bool SelectedD64RunGateEnabled()
    {
        var value = Environment.GetEnvironmentVariable(SelectedD64RunGateEnvironmentVariable);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRepoD64FixturePath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "D64", fileName);
        if (File.Exists(outputPath))
            return outputPath;

        var sourceDirectory = typeof(X64ScVariantLockstepTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "TestHarnessSourceDirectory")
            ?.Value;

        return string.IsNullOrWhiteSpace(sourceDirectory)
            ? outputPath
            : Path.Combine(sourceDirectory, "Fixtures", "D64", fileName);
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
        // Delta-stepping: each StepCycle fires one CLK_INC checkpoint, but bad-line steals
        // advance maincpu_clk by 43 extra ticks WITHOUT checkpoints. Advancing managed by
        // the same delta keeps both machines at the same absolute cycle position.
        // Same fix as LockstepValidator.Run().
        var initState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(native, ref initState);
        long prevNativeCycle = initState.Cycle;

        for (var cycle = 0L; cycle < cycles; cycle++)
        {
            ViceNativeBridge.StepCycle(native);

            var vicState = new ViceNativeBridge.ViceVicState();
            ViceNativeBridge.GetVicState(native, ref vicState);
            long nativeDelta = vicState.Cycle - prevNativeCycle;
            prevNativeCycle = vicState.Cycle;

            for (long j = 0; j < nativeDelta; j++)
                machine.Clock.Step();

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
            : $"managedVic(line={vic.CurrentRasterLine},cycle={vic.CurrentCycle}{FormatManagedVicBank(memory)},lastPhi1=${vic.LastReadPhi1:X2},d018=${vic.Peek(0xD018):X2})";
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
        if (!TryTranslateManagedVicAddress(memory, 0x3FFF, out var idleAddress) ||
            !TryTranslateManagedVicAddress(memory, 0x03FB, out var pointer3Address) ||
            !TryTranslateManagedVicAddress(memory, 0x03FC, out var pointer4Address) ||
            !TryTranslateManagedVicAddress(memory, (ushort)(0x3F00 + (byte)(0xFF - vic.CurrentRasterLine * 5)), out var refreshAddress))
            return "managedPhi1 address translation unavailable";

        return
            $"managedPhi1(idle=${idleAddress:X4}:${memory.Span[idleAddress]:X2},ptr3=${pointer3Address:X4}:${memory.Span[pointer3Address]:X2}," +
            $"ptr4=${pointer4Address:X4}:${memory.Span[pointer4Address]:X2},refresh0=${refreshAddress:X4}:${memory.Span[refreshAddress]:X2})";
    }

    private static string FormatManagedVicBank(IMemory? memory)
    {
        if (memory is null)
            return string.Empty;

        var property = memory.GetType().GetProperty(
            "VicBankForDiagnostics",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(memory) is int bank ? $",bank={bank}" : string.Empty;
    }

    private static bool TryTranslateManagedVicAddress(IMemory memory, ushort vicAddress, out ushort address)
    {
        var method = memory.GetType().GetMethod(
            "TranslateVicAddressForDiagnostics",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method?.Invoke(memory, [vicAddress]) is ushort translatedAddress)
        {
            address = translatedAddress;
            return true;
        }

        address = 0;
        return false;
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
            // Peek-vs-peek: managed Mos6569.Peek mirrors vicii_peek exactly
            // (FR-VIC-REGISTERS AC-15), so compare against the shim's
            // vicii_peek view, not the raw vicii.regs store.
            var expected = nativeState.RegistersPeek[register];

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
