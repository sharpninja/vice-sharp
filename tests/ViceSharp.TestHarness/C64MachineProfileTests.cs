using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cartridges;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Tape;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

namespace ViceSharp.TestHarness;

[Collection("NativeVice")]
public sealed class C64MachineProfileTests
{
    public static TheoryData<string> RequiredProfileSelectors
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var profile in C64MachineProfiles.All)
                data.Add(profile.Id);

            return data;
        }
    }

    public static TheoryData<string, string> RequiredProfileAliases
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var profile in C64MachineProfiles.All)
            {
                foreach (var alias in profile.Aliases.Append(profile.Id).Distinct(StringComparer.OrdinalIgnoreCase))
                    data.Add(alias, profile.Id);
            }

            return data;
        }
    }

    public static TheoryData<string, string, long, int, int, VideoStandard, C64VicIIModel, C64SidModel, C64BoardModel, bool, bool> RequiredProfiles => new()
    {
        { "c64", "Commodore 64 PAL", 985_248, 63, 312, VideoStandard.Pal, C64VicIIModel.Mos6569, C64SidModel.Mos6581, C64BoardModel.Breadbox, true, false },
        { "c64c", "Commodore 64C PAL", 985_248, 63, 312, VideoStandard.Pal, C64VicIIModel.Mos8565, C64SidModel.Mos8580, C64BoardModel.C64C, true, false },
        { "c64old", "Commodore 64 old PAL", 985_248, 63, 312, VideoStandard.Pal, C64VicIIModel.Mos6569R1, C64SidModel.Mos6581, C64BoardModel.BreadboxOld, true, false },
        { "ntsc", "Commodore 64 NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64VicIIModel.Mos6567R8, C64SidModel.Mos6581, C64BoardModel.Breadbox, true, false },
        { "newntsc", "Commodore 64C NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64VicIIModel.Mos8562, C64SidModel.Mos8580, C64BoardModel.C64C, true, false },
        { "oldntsc", "Commodore 64 old NTSC", 1_022_730, 64, 262, VideoStandard.Ntsc, C64VicIIModel.Mos6567R56A, C64SidModel.Mos6581, C64BoardModel.BreadboxOld, true, false },
        { "paln", "Commodore 64 PAL-N / Drean", 1_023_440, 65, 312, VideoStandard.Pal, C64VicIIModel.Mos6572, C64SidModel.Mos6581, C64BoardModel.Drean, true, false },
        { "sx64pal", "Commodore SX-64 PAL", 985_248, 63, 312, VideoStandard.Pal, C64VicIIModel.Mos6569, C64SidModel.Mos6581, C64BoardModel.SX64, true, false },
        { "sx64ntsc", "Commodore SX-64 NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64VicIIModel.Mos6567R8, C64SidModel.Mos6581, C64BoardModel.SX64, true, false },
        { "pet64pal", "Commodore PET64 PAL", 985_248, 63, 312, VideoStandard.Pal, C64VicIIModel.Mos6569, C64SidModel.Mos6581, C64BoardModel.PET64, true, false },
        { "pet64ntsc", "Commodore PET64 NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64VicIIModel.Mos6567R8, C64SidModel.Mos6581, C64BoardModel.PET64, true, false },
        { "ultimax", "Commodore MAX / Ultimax", 1_022_730, 65, 263, VideoStandard.Ntsc, C64VicIIModel.Mos6567R8, C64SidModel.Mos6581, C64BoardModel.Ultimax, true, true },
        { "c64gs", "Commodore 64 Games System", 985_248, 63, 312, VideoStandard.Pal, C64VicIIModel.Mos8565, C64SidModel.Mos8580, C64BoardModel.C64GS, false, true },
        { "c64jap", "Commodore 64 Japanese", 1_022_730, 65, 263, VideoStandard.Ntsc, C64VicIIModel.Mos6567R8, C64SidModel.Mos6581, C64BoardModel.Japanese, true, false }
    };

    [Theory]
    [MemberData(nameof(RequiredProfiles))]
    public void X64ScProfiles_ExposeExpectedTimingAndDefaults(
        string selector,
        string displayName,
        long nominalClockHz,
        int cyclesPerLine,
        int rasterLines,
        VideoStandard videoStandard,
        C64VicIIModel vicModel,
        C64SidModel sidModel,
        C64BoardModel boardModel,
        bool keyboardEnabled,
        bool cartridgeBootExpected)
    {
        var profile = C64MachineProfiles.Resolve(selector);
        var descriptor = new C64Descriptor(selector);

        profile.Id.Should().Be(selector);
        profile.DisplayName.Should().Be(displayName);
        profile.NominalClockHz.Should().Be(nominalClockHz);
        profile.CyclesPerLine.Should().Be(cyclesPerLine);
        profile.RasterLines.Should().Be(rasterLines);
        profile.VideoStandard.Should().Be(videoStandard);
        profile.VicII.Should().Be(vicModel);
        profile.VicIIModel.Should().Be(vicModel.ToString());
        profile.Sid.Should().Be(sidModel);
        profile.Board.Should().Be(boardModel);
        profile.BoardModel.Should().Be(boardModel.ToString());
        profile.KeyboardEnabled.Should().Be(keyboardEnabled);
        profile.CartridgeBootExpected.Should().Be(cartridgeBootExpected);
        profile.Family.Should().Be("x64sc");
        profile.SystemCore.Family.Should().Be("x64sc");
        profile.SystemCore.KeyboardMatrixConnected.Should().Be(keyboardEnabled);
        profile.SystemCore.CartridgeBootExpected.Should().Be(cartridgeBootExpected);
        profile.SystemCore.BoardPolicy.Should().Be(profile.BoardModel);
        profile.SystemCore.Traits.Should().ContainKey("tapePort");
        descriptor.MachineName.Should().Be(displayName);
        descriptor.MasterClockHz.Should().Be(nominalClockHz);
        descriptor.VideoStandard.Should().Be(videoStandard);
        descriptor.MachineProfile.Should().BeSameAs(profile);
    }

    [Fact]
    public void X64ScProfiles_SelectSystemCoreDefinitionForEveryVariant()
    {
        foreach (var profile in C64MachineProfiles.All)
        {
            profile.SystemCore.Id.Should().StartWith("x64sc:");
            profile.SystemCore.DisplayName.Should().NotBeNullOrWhiteSpace();
            profile.SystemCore.BoardPolicy.Should().Be(profile.BoardModel);
            profile.SystemCore.Traits.Should().ContainKey("pla");
            profile.SystemCore.Traits.Should().ContainKey("bus");
        }
    }

    [Fact]
    public void SystemCoreDefinitions_DifferentiateVariantBusAndPlaPolicies()
    {
        C64MachineProfiles.C64CPal.SystemCore.BusPolicy.Should().Be(C64BusPolicy.Standard.ToString());
        C64MachineProfiles.C64CPal.SystemCore.AddressDecoderPolicy.Should().Be(C64PlaPolicy.Standard.ToString());
        C64MachineProfiles.C64GS.SystemCore.BusPolicy.Should().Be(C64BusPolicy.GameSystem.ToString());
        C64MachineProfiles.C64GS.SystemCore.AddressDecoderPolicy.Should().Be(C64PlaPolicy.CartridgeRequired.ToString());
        C64MachineProfiles.Ultimax.SystemCore.BusPolicy.Should().Be(C64BusPolicy.Max.ToString());
        C64MachineProfiles.Ultimax.SystemCore.AddressDecoderPolicy.Should().Be(C64PlaPolicy.Ultimax.ToString());
        C64MachineProfiles.C64Pal.SystemCore.TapePortConnected.Should().BeTrue();
        C64MachineProfiles.SX64Pal.SystemCore.TapePortConnected.Should().BeFalse();
        C64MachineProfiles.SX64Ntsc.SystemCore.TapePortConnected.Should().BeFalse();
        C64MachineProfiles.C64GS.SystemCore.TapePortConnected.Should().BeFalse();
        C64MachineProfiles.Ultimax.SystemCore.IecBusConnected.Should().BeFalse();
        C64MachineProfiles.C64GS.SystemCore.IecBusConnected.Should().BeFalse();
        C64MachineProfiles.C64Pal.SystemCore.Cia2Connected.Should().BeTrue();
        C64MachineProfiles.C64GS.SystemCore.Cia2Connected.Should().BeTrue();
        C64MachineProfiles.Ultimax.SystemCore.Cia2Connected.Should().BeFalse();
        C64MachineProfiles.Ultimax.SystemCore.Traits.Should().ContainKey("iecBus").WhoseValue.Should().Be("absent");
        C64MachineProfiles.C64GS.SystemCore.Traits.Should().ContainKey("iecBus").WhoseValue.Should().Be("absent");
        C64MachineProfiles.Ultimax.SystemCore.Traits.Should().ContainKey("cia2").WhoseValue.Should().Be("absent");
    }

    [Theory]
    [InlineData("c64", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character)]
    [InlineData("c64c", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character)]
    [InlineData("c64old", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev2, C64ViceRomNames.Character)]
    [InlineData("ntsc", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character)]
    [InlineData("newntsc", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character)]
    [InlineData("oldntsc", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev1, C64ViceRomNames.Character)]
    [InlineData("paln", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character)]
    [InlineData("sx64pal", C64ViceRomNames.Basic, C64ViceRomNames.KernalSx64, C64ViceRomNames.Character)]
    [InlineData("sx64ntsc", C64ViceRomNames.Basic, C64ViceRomNames.KernalSx64, C64ViceRomNames.Character)]
    [InlineData("pet64pal", C64ViceRomNames.Basic, C64ViceRomNames.Kernal4064, C64ViceRomNames.Character)]
    [InlineData("pet64ntsc", C64ViceRomNames.Basic, C64ViceRomNames.Kernal4064, C64ViceRomNames.Character)]
    [InlineData("ultimax", C64ViceRomNames.Basic, C64ViceRomNames.KernalNone, C64ViceRomNames.Character)]
    [InlineData("c64gs", C64ViceRomNames.Basic, C64ViceRomNames.KernalGs, C64ViceRomNames.Character)]
    [InlineData("c64jap", C64ViceRomNames.Basic, C64ViceRomNames.KernalJapanese, C64ViceRomNames.CharacterJapanese)]
    public void X64ScProfiles_SelectViceModelRomResources(
        string selector,
        string expectedBasic,
        string expectedKernal,
        string expectedCharacters)
    {
        var profile = C64MachineProfiles.Resolve(selector);
        var romSet = new C64RomSet(
            profile.RomSet,
            profile.BasicRomName,
            profile.KernalRomName,
            profile.CharacterRomName);

        profile.BasicRomName.Should().Be(expectedBasic);
        profile.KernalRomName.Should().Be(expectedKernal);
        profile.CharacterRomName.Should().Be(expectedCharacters);
        romSet.IsComplete(MachineTestFactory.CreateC64RomProvider()).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(RequiredProfileSelectors))]
    public void ArchitectureBuilder_LoadsViceModelRomResourceBytes(string selector)
    {
        var profile = C64MachineProfiles.Resolve(selector);
        var provider = MachineTestFactory.CreateC64RomProvider();
        var machine = MachineTestFactory.CreateC64Machine(selector);
        var basic = provider.LoadRom(profile.BasicRomName, "C64").Span;
        var character = provider.LoadRom(profile.CharacterRomName, "C64").Span;

        machine.Bus.Peek(0xA000).Should().Be(basic[0], $"{selector} should map the profile-selected BASIC ROM");

        if (C64RomSet.IsKernalRequired(profile.KernalRomName))
        {
            var kernal = provider.LoadRom(profile.KernalRomName, "C64").Span;
            machine.Bus.Peek(0xE000).Should().Be(kernal[0], $"{selector} should map the profile-selected KERNAL ROM");
        }
        else
        {
            machine.Bus.Peek(0xE000).Should().Be(0x00, $"{selector} should model VICE's MAX/Ultimax no-KERNAL policy");
        }

        machine.Bus.Write(0x0001, 0x33);
        machine.Bus.Read(0xD000).Should().Be(character[0], $"{selector} should map the profile-selected character ROM");
    }

    [Fact]
    public void C64GsCartridgeRomLowFollowsPlaVisibilityDuringRamTest()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64gs");
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("C64GS did not expose system RAM.");
        var cartridgePort = machine.Devices.GetAll<ICartridgePort>().Single();
        var cartridge = new byte[StandardCartridgeImage.GameSystemRomSize];
        cartridge[0] = 0xA5;
        memory.Span[0x8000] = 0x5A;

        cartridgePort.AttachCartridge(cartridge, CartridgeMappingMode.GameSystem);

        machine.Bus.Read(0x8000).Should().Be(0xA5, "ROML should be visible in the reset PLA configuration");

        machine.Bus.Write(0x0001, 0x34);

        machine.Bus.Read(0x8000).Should().Be(0x5A, "ROML should disappear when LORAM/HIRAM are both deasserted during RAMTAS");
    }

    [Fact]
    public async Task C64GsSystemCore_DisablesKeyboardMatrixEvenThoughHostInputSurfaceExists()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine("c64gs");
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            new C64Descriptor("c64gs"),
            machine));
        var service = new ViceSharp.Host.Services.InputServiceHost(registry);

        var response = await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Space", true),
            TestContext.Current.CancellationToken);

        response.Status.Code.Should().Be(RpcStatusCode.Ok);
        response.InputState!.Keys.Should().Contain(key => key.Key == "Space" && key.IsPressed && !key.AppliedToRuntime);
        machine.Bus.Write(0xDC01, 0xEF);
        (machine.Bus.Read(0xDC00) & 0x80).Should().Be(0x80);
    }

    [Theory]
    [MemberData(nameof(RequiredProfileSelectors))]
    public void ArchitectureBuilder_MapsControlPortsToCia1ForEveryRequiredX64ScVariant(string selector)
    {
        var machine = MachineTestFactory.CreateC64Machine(selector);
        var joystick = machine.Devices.GetAll<IMachineJoystickInput>().Single();

        joystick.SetJoystickState(2, 0x01, fireButton: true).Should().BeTrue();
        joystick.SetJoystickState(1, 0x02, fireButton: true).Should().BeTrue();

        (machine.Bus.Read(0xDC00) & 0x11).Should().Be(0, $"{selector} control port 2 should drive CIA1 port A");
        (machine.Bus.Read(0xDC01) & 0x12).Should().Be(0, $"{selector} control port 1 should drive CIA1 port B");

        joystick.SetJoystickState(2, 0x00, fireButton: false).Should().BeTrue();
        joystick.SetJoystickState(1, 0x00, fireButton: false).Should().BeTrue();

        (machine.Bus.Read(0xDC00) & 0x11).Should().Be(0x11, $"{selector} control port 2 should release CIA1 port A lines");
        (machine.Bus.Read(0xDC01) & 0x12).Should().Be(0x12, $"{selector} control port 1 should release CIA1 port B lines");
    }

    [Theory]
    [InlineData("c64")]
    [InlineData("c64c")]
    [InlineData("ultimax")]
    [InlineData("c64gs")]
    public void ArchitectureBuilder_RegistersSelectedSystemCoreAsGlueBetweenProfileAndChips(string selector)
    {
        var descriptor = new C64Descriptor(selector);
        var machine = MachineTestFactory.CreateC64Machine(descriptor);

        var systemCore = machine.Devices.GetAll<ISystemCore>().Single();

        systemCore.Definition.Should().BeSameAs(descriptor.Profile.SystemCore);
        machine.Devices.GetByRole(DeviceRole.SystemCore).Should().BeSameAs(systemCore);
        machine.Devices.GetByRole(DeviceRole.Cpu).Should().NotBeNull();
        machine.Devices.GetByRole(DeviceRole.VideoChip).Should().NotBeNull();
        machine.Devices.GetByRole(DeviceRole.Pla).Should().NotBeNull();
    }

    [Theory]
    [InlineData("c64", true)]
    [InlineData("sx64pal", false)]
    [InlineData("sx64ntsc", false)]
    [InlineData("c64gs", false)]
    public void ArchitectureBuilder_RegistersDatasetteOnlyWhenProfileConnectsTapePort(
        string selector,
        bool expectedTapePort)
    {
        var machine = MachineTestFactory.CreateC64Machine(selector);

        machine.Devices.All.OfType<ITapeDevice>().Any().Should().Be(expectedTapePort);
    }

    [Theory]
    [InlineData("c64", 8, 9)]
    [InlineData("sx64pal", 8, 9)]
    [InlineData("sx64ntsc", 8, 9)]
    [InlineData("c64gs")]
    [InlineData("ultimax")]
    public void ArchitectureBuilder_RegistersIecDrivesOnlyWhenProfileConnectsIecBus(
        string selector,
        params int[] expectedDriveNumbers)
    {
        var machine = MachineTestFactory.CreateC64Machine(selector);

        machine.Devices.GetAll<IFloppyDrive>()
            .Select(drive => (int)drive.DriveNumber)
            .Should()
            .BeEquivalentTo(expectedDriveNumbers);
    }

    [Theory]
    [InlineData("c64", true)]
    [InlineData("c64gs", true)]
    [InlineData("ultimax", false)]
    public void ArchitectureBuilder_RegistersCia2OnlyWhenProfileConnectsCia2(
        string selector,
        bool expectedCia2)
    {
        var machine = MachineTestFactory.CreateC64Machine(selector);

        machine.Architecture.Devices.Any(device => device.Role == DeviceRole.Cia2).Should().Be(expectedCia2);
        (machine.Devices.GetByRole(DeviceRole.Cia2) is not null).Should().Be(expectedCia2);
        machine.Devices.GetAll<Mos6526>()
            .Count(cia => cia.BaseAddress == 0xDD00)
            .Should()
            .Be(expectedCia2 ? 1 : 0);
    }

    [Fact]
    public void Ultimax_Dd00IoRegionReadsOpenBusAndIgnoresWritesWhenCia2IsAbsent()
    {
        var machine = MachineTestFactory.CreateC64Machine("ultimax");
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("Ultimax did not expose system RAM.");

        memory.Span[0x03FF] = 0xA7;
        memory.Span[0xDD00] = 0x5A;
        machine.Bus.Write(0x0001, 0x37);

        machine.Bus.Write(0xDD00, 0xA5);
        machine.Clock.Step();

        memory.Span[0xDD00].Should().Be(0x5A);
        machine.Bus.Read(0xDD00).Should().Be(0xA7);
    }

    [Fact]
    public void C64_Dd00IoRegionRoutesToCia2WhenCia2IsConnected()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64");
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("C64 did not expose system RAM.");

        memory.Span[0xDD00] = 0x5A;
        machine.Bus.Write(0x0001, 0x37);

        machine.Bus.Write(0xDD00, 0xA5);

        memory.Span[0xDD00].Should().Be(0x5A);
    }

    [Theory]
    [MemberData(nameof(RequiredProfileAliases))]
    public void X64ScProfiles_ResolveViceAliases(string alias, string expectedId)
    {
        C64MachineProfiles.Resolve(alias).Id.Should().Be(expectedId);
    }

    [ViceTheory]
    [MemberData(nameof(RequiredProfileAliases))]
    public void NativeVice_CreatesAndResetsEveryManagedX64ScAlias(string alias, string expectedId)
    {
        var native = ViceNative.CreateModel(alias);
        var expectedNativeModel = ExpectedNativeModel(expectedId);

        native.Should().NotBe(IntPtr.Zero, $"{alias} should resolve to native x64sc model {expectedId}");
        try
        {
            ViceNative.GetModel(native).Should().Be(expectedNativeModel, $"{alias} should select native x64sc model enum for {expectedId}");
            ViceNative.ResetNative(native);
            ViceNative.GetModel(native).Should().Be(expectedNativeModel, $"{alias} should preserve native x64sc model enum for {expectedId} after reset");
        }
        finally
        {
            ViceNative.Destroy(native);
        }
    }

    [ViceFact]
    public void NativeVice_DefaultCreateSelectsPalBreadboxModel()
    {
        var native = ViceNative.Create();

        native.Should().NotBe(IntPtr.Zero);
        try
        {
            ViceNative.GetModel(native).Should().Be(ExpectedNativeModel("c64"));
            ViceNative.ResetNative(native);
            ViceNative.GetModel(native).Should().Be(ExpectedNativeModel("c64"));
        }
        finally
        {
            ViceNative.Destroy(native);
        }
    }

    [Theory]
    [InlineData("c64", 63 * 312, typeof(Mos6569), typeof(Sid6581))]
    [InlineData("c64c", 63 * 312, typeof(Mos8565), typeof(Sid8580))]
    [InlineData("c64old", 63 * 312, typeof(Mos6569R1), typeof(Sid6581))]
    [InlineData("ntsc", 65 * 263, typeof(Mos6567), typeof(Sid6581))]
    [InlineData("newntsc", 65 * 263, typeof(Mos8562), typeof(Sid8580))]
    [InlineData("oldntsc", 64 * 262, typeof(Mos6567R56A), typeof(Sid6581))]
    [InlineData("paln", 65 * 312, typeof(Mos6572), typeof(Sid6581))]
    [InlineData("sx64pal", 63 * 312, typeof(Mos6569), typeof(Sid6581))]
    [InlineData("sx64ntsc", 65 * 263, typeof(Mos6567), typeof(Sid6581))]
    [InlineData("pet64pal", 63 * 312, typeof(Mos6569), typeof(Sid6581))]
    [InlineData("pet64ntsc", 65 * 263, typeof(Mos6567), typeof(Sid6581))]
    [InlineData("ultimax", 65 * 263, typeof(Mos6567), typeof(Sid6581))]
    [InlineData("c64gs", 63 * 312, typeof(Mos8565), typeof(Sid8580))]
    [InlineData("c64jap", 65 * 263, typeof(Mos6567), typeof(Sid6581))]
    public void ArchitectureBuilder_UsesProfileTimingAndChipDefaults(
        string selector,
        int expectedFrameCycles,
        Type expectedVicType,
        Type expectedSidType)
    {
        var machine = MachineTestFactory.CreateC64Machine(selector);

        machine.RunFrame();

        machine.Clock.TotalCycles.Should().Be(expectedFrameCycles);
        machine.Devices.GetByRole(DeviceRole.VideoChip).Should().BeOfType(expectedVicType);
        machine.Devices.GetByRole(DeviceRole.AudioChip).Should().BeOfType(expectedSidType);
    }

    [Theory]
    [InlineData("c64")]
    [InlineData("c64c")]
    [InlineData("c64old")]
    [InlineData("ntsc")]
    [InlineData("newntsc")]
    [InlineData("oldntsc")]
    [InlineData("paln")]
    [InlineData("sx64pal")]
    [InlineData("sx64ntsc")]
    [InlineData("pet64pal")]
    [InlineData("pet64ntsc")]
    [InlineData("ultimax")]
    [InlineData("c64gs")]
    [InlineData("c64jap")]
    public void HostFactory_CreatesSessionForEveryRequiredX64ScVariant(string selector)
    {
        var factory = new DefaultEmulatorRuntimeFactory();

        var session = factory.Create(new CreateEmulatorSessionRequest(selector));

        session.Architecture.Should().BeAssignableTo<IProfiledArchitectureDescriptor>();
        ((IProfiledArchitectureDescriptor)session.Architecture).MachineProfile.Id.Should().Be(selector);
    }

    private static int ExpectedNativeModel(string profileId)
    {
        return profileId switch
        {
            "c64" => 0,
            "c64c" => 1,
            "c64old" => 2,
            "ntsc" => 3,
            "newntsc" => 4,
            "oldntsc" => 5,
            "paln" => 6,
            "sx64pal" => 7,
            "sx64ntsc" => 8,
            "c64jap" => 9,
            "c64gs" => 10,
            "pet64pal" => 11,
            "pet64ntsc" => 12,
            "ultimax" => 13,
            _ => throw new ArgumentOutOfRangeException(nameof(profileId), profileId, "Unknown x64sc profile id.")
        };
    }
}
