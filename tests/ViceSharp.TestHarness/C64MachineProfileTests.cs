using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.VicIi;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

namespace ViceSharp.TestHarness;

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

    public static TheoryData<string, string, long, int, int, VideoStandard, C64SidModel, bool, bool> RequiredProfiles => new()
    {
        { "c64", "Commodore 64 PAL", 985_248, 63, 312, VideoStandard.Pal, C64SidModel.Mos6581, true, false },
        { "c64c", "Commodore 64C PAL", 985_248, 63, 312, VideoStandard.Pal, C64SidModel.Mos8580, true, false },
        { "c64old", "Commodore 64 old PAL", 985_248, 63, 312, VideoStandard.Pal, C64SidModel.Mos6581, true, false },
        { "ntsc", "Commodore 64 NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64SidModel.Mos6581, true, false },
        { "newntsc", "Commodore 64C NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64SidModel.Mos8580, true, false },
        { "oldntsc", "Commodore 64 old NTSC", 1_022_730, 64, 262, VideoStandard.Ntsc, C64SidModel.Mos6581, true, false },
        { "paln", "Commodore 64 PAL-N / Drean", 1_023_440, 65, 312, VideoStandard.Pal, C64SidModel.Mos6581, true, false },
        { "sx64pal", "Commodore SX-64 PAL", 985_248, 63, 312, VideoStandard.Pal, C64SidModel.Mos6581, true, false },
        { "sx64ntsc", "Commodore SX-64 NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64SidModel.Mos6581, true, false },
        { "pet64pal", "Commodore PET64 PAL", 985_248, 63, 312, VideoStandard.Pal, C64SidModel.Mos6581, true, false },
        { "pet64ntsc", "Commodore PET64 NTSC", 1_022_730, 65, 263, VideoStandard.Ntsc, C64SidModel.Mos6581, true, false },
        { "ultimax", "Commodore MAX / Ultimax", 1_022_730, 65, 263, VideoStandard.Ntsc, C64SidModel.Mos6581, true, true },
        { "c64gs", "Commodore 64 Games System", 985_248, 63, 312, VideoStandard.Pal, C64SidModel.Mos8580, false, true },
        { "c64jap", "Commodore 64 Japanese", 1_022_730, 65, 263, VideoStandard.Ntsc, C64SidModel.Mos6581, true, false }
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
        C64SidModel sidModel,
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
        profile.Sid.Should().Be(sidModel);
        profile.KeyboardEnabled.Should().Be(keyboardEnabled);
        profile.CartridgeBootExpected.Should().Be(cartridgeBootExpected);
        profile.Family.Should().Be("x64sc");
        profile.SystemCore.Family.Should().Be("x64sc");
        profile.SystemCore.KeyboardMatrixConnected.Should().Be(keyboardEnabled);
        profile.SystemCore.CartridgeBootExpected.Should().Be(cartridgeBootExpected);
        profile.SystemCore.BoardPolicy.Should().Be(profile.BoardModel);
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
    [InlineData("breadbox", "c64")]
    [InlineData("pal", "c64")]
    [InlineData("c64new", "c64c")]
    [InlineData("newpal", "c64c")]
    [InlineData("c64ntsc", "ntsc")]
    [InlineData("c64cntsc", "newntsc")]
    [InlineData("c64newntsc", "newntsc")]
    [InlineData("c64oldntsc", "oldntsc")]
    [InlineData("drean", "paln")]
    [InlineData("sx64", "sx64pal")]
    [InlineData("pet64", "pet64pal")]
    [InlineData("max", "ultimax")]
    [InlineData("gs", "c64gs")]
    [InlineData("jap", "c64jap")]
    public void X64ScProfiles_ResolveViceAliases(string alias, string expectedId)
    {
        C64MachineProfiles.Resolve(alias).Id.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("c64", 63 * 312, typeof(Mos6569), typeof(Sid6581))]
    [InlineData("c64c", 63 * 312, typeof(Mos8565), typeof(Sid8580))]
    [InlineData("c64old", 63 * 312, typeof(Mos6569R1), typeof(Sid6581))]
    [InlineData("ntsc", 65 * 263, typeof(Mos6567), typeof(Sid6581))]
    [InlineData("newntsc", 65 * 263, typeof(Mos8562), typeof(Sid8580))]
    [InlineData("oldntsc", 64 * 262, typeof(Mos6567R56A), typeof(Sid6581))]
    [InlineData("paln", 65 * 312, typeof(Mos6572), typeof(Sid6581))]
    [InlineData("c64gs", 63 * 312, typeof(Mos8565), typeof(Sid8580))]
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
}
