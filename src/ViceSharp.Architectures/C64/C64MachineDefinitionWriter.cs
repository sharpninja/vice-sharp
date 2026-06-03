using ViceSharp.Architectures.Adhoc;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ViceSharp.Architectures.C64;

/// <summary>
/// Builds a machine-definition entity for a C64 variant from its
/// <see cref="C64MachineProfile"/> and writes it as YAML (schema v1): a single
/// specific ROM set, the variant's chips (VIC-II / SID die + raster timing),
/// the PAL/NTSC selection, and the bus / system-core config. Because the source
/// of truth is <see cref="C64MachineProfiles"/>, the emitted definitions never
/// drift from the in-code profiles.
///
/// Serialization uses YamlDotNet's source-generated <see cref="StaticContext"/>
/// (<see cref="MachineDefinitionYamlContext"/>), so it is AOT/trim-safe without
/// reflection or suppressions.
/// </summary>
public static class C64MachineDefinitionWriter
{
    private const string CartridgeOnlyKernal = C64ViceRomNames.KernalNone;

    /// <summary>Map a C64 profile onto the serializable machine-definition entity.</summary>
    public static MachineDefinitionDocument BuildDocument(C64MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var core = profile.SystemCore;
        var hasKernal = !string.Equals(profile.KernalRomName, CartridgeOnlyKernal, StringComparison.OrdinalIgnoreCase);

        var document = new MachineDefinitionDocument
        {
            SchemaVersion = 1,
            Machine = new MachineInfo
            {
                Id = profile.Id,
                Name = profile.DisplayName,
                VideoStandard = profile.VideoStandard.ToString(),
                MasterClockHz = profile.NominalClockHz,
                ResetVector = "0xFFFC",
            },
            SystemCore = new SystemCoreInfo
            {
                Board = core.BoardPolicy,
                BusPolicy = core.BusPolicy,
                AddressDecoderPolicy = core.AddressDecoderPolicy,
                KeyboardConnected = core.KeyboardMatrixConnected,
                TapePortConnected = core.TapePortConnected,
                IecBusConnected = core.IecBusConnected,
                Cia2Connected = core.Cia2Connected,
                CartridgeBootExpected = profile.CartridgeBootExpected,
            },
            InterruptLines =
            [
                new InterruptLineInfo { Id = "irq", Type = "Irq" },
                new InterruptLineInfo { Id = "nmi", Type = "Nmi" },
            ],
        };

        var regions = document.Memory.Regions;
        regions.Add(new MemoryRegionInfo { Id = "ram-main", Kind = "Ram", Start = "0x0000", End = "0xFFFF" });
        regions.Add(new MemoryRegionInfo { Id = "ram-color", Kind = "Ram", Start = "0xD800", End = "0xDBFF" });
        regions.Add(new MemoryRegionInfo
        {
            Id = "rom-basic", Kind = "Rom", Start = "0xA000", End = "0xBFFF",
            Rom = new RomSelectionInfo { System = "C64", Role = "basic", File = profile.BasicRomName },
        });
        if (hasKernal)
        {
            regions.Add(new MemoryRegionInfo
            {
                Id = "rom-kernal", Kind = "Rom", Start = "0xE000", End = "0xFFFF",
                Rom = new RomSelectionInfo { System = "C64", Role = "kernal", File = profile.KernalRomName },
            });
        }
        regions.Add(new MemoryRegionInfo
        {
            Id = "rom-chargen", Kind = "Rom", Start = "0xD000", End = "0xDFFF",
            Rom = new RomSelectionInfo { System = "C64", Role = "chargen", File = profile.CharacterRomName },
        });

        var chips = document.Chips;
        chips.Add(new ChipInfo { Id = "cpu", Type = "Mos6502", Role = "Cpu" });
        chips.Add(new ChipInfo
        {
            Id = "vic", Type = "Mos6569", Model = profile.VicII.ToString(), Role = "VideoChip",
            BaseAddress = "0xD000", IrqLine = "irq",
            CyclesPerLine = profile.CyclesPerLine, RasterLines = profile.RasterLines,
        });
        chips.Add(new ChipInfo { Id = "cia1", Type = "Mos6526", Role = "Cia1", BaseAddress = "0xDC00", IrqLine = "irq" });
        if (core.Cia2Connected)
        {
            // CIA2 drives the NMI line in the C64 topology.
            chips.Add(new ChipInfo { Id = "cia2", Type = "Mos6526", Role = "Cia2", BaseAddress = "0xDD00", IrqLine = "nmi" });
        }
        chips.Add(new ChipInfo { Id = "sid", Type = "Sid6581", Model = profile.Sid.ToString(), Role = "AudioChip" });

        return document;
    }

    /// <summary>Emit the schema-v1 machine YAML for a single C64 variant.</summary>
    public static string ToYaml(C64MachineProfile profile)
    {
        var serializer = new StaticSerializerBuilder(new MachineDefinitionYamlContext())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(BuildDocument(profile));
    }

    /// <summary>Emit (id, yaml) for every C64 variant in <see cref="C64MachineProfiles.All"/>.</summary>
    public static IEnumerable<(string Id, string Yaml)> All() =>
        C64MachineProfiles.All.Select(profile => (profile.Id, ToYaml(profile)));
}
