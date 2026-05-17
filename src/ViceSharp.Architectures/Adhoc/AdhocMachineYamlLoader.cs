using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ViceSharp.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// Reads an ad-hoc machine architecture YAML document, validates it
/// against schema v1, and produces a <see cref="AdhocMachineBlueprint"/>
/// that can build a runnable <see cref="IMachine"/>.
/// </summary>
/// <remarks>
/// This loader uses YamlDotNet's reflection-based deserializer, so callers
/// using NativeAOT must invoke <see cref="LoadFromFile"/> and
/// <see cref="LoadFromString"/> only from non-AOT code paths or supply their
/// own pre-validated <see cref="AdhocMachineBlueprint"/>.
/// </remarks>
[RequiresDynamicCode(LoaderRequiresDynamicCode)]
[RequiresUnreferencedCode(LoaderRequiresUnreferencedCode)]
public sealed class AdhocMachineYamlLoader
{
    internal const string LoaderRequiresDynamicCode =
        "YamlDotNet's default deserializer uses reflection emit, which is incompatible with NativeAOT.";
    internal const string LoaderRequiresUnreferencedCode =
        "YamlDotNet's default deserializer uses runtime type discovery, which is incompatible with trimming.";

    private const int SupportedSchemaVersion = 1;

    private readonly IDeserializer _deserializer;

    public AdhocMachineYamlLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Read and validate the YAML document at <paramref name="path"/>.
    /// </summary>
    public AdhocMachineBlueprint LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Ad-hoc machine YAML file not found: {path}", path);
        }

        var yaml = File.ReadAllText(path);
        return LoadFromString(yaml);
    }

    /// <summary>
    /// Parse and validate the supplied YAML <paramref name="yaml"/> text.
    /// </summary>
    public AdhocMachineBlueprint LoadFromString(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        AdhocMachineDocument doc;
        try
        {
            doc = _deserializer.Deserialize<AdhocMachineDocument>(yaml)
                ?? throw new AdhocMachineValidationException(
                    "yaml document is empty or not a mapping.");
        }
        catch (YamlException ex)
        {
            throw new AdhocMachineValidationException(
                $"yaml parse error: {ex.Message}", ex);
        }

        return Validate(doc);
    }

    private static AdhocMachineBlueprint Validate(AdhocMachineDocument doc)
    {
        ValidateSchemaVersion(doc);
        var descriptor = ValidateMachineSection(doc.Machine);
        var lines = ValidateInterruptLines(doc.InterruptLines);
        var regions = ValidateMemorySection(doc.Memory);
        var chips = ValidateChips(doc.Chips, lines);

        var plan = new AdhocMachinePlan
        {
            Regions = regions,
            Chips = chips,
            InterruptLines = lines,
        };
        return new AdhocMachineBlueprint(descriptor, plan);
    }

    private static void ValidateSchemaVersion(AdhocMachineDocument doc)
    {
        if (doc.SchemaVersion is null)
        {
            throw new AdhocMachineValidationException(
                "Required field 'schemaVersion' is missing.");
        }
        if (doc.SchemaVersion != SupportedSchemaVersion)
        {
            throw new AdhocMachineValidationException(
                $"schemaVersion {doc.SchemaVersion} is not supported; this loader only handles version {SupportedSchemaVersion}.");
        }
    }

    private static AdhocArchitectureDescriptor ValidateMachineSection(AdhocMachineSection? section)
    {
        if (section is null)
        {
            throw new AdhocMachineValidationException(
                "Required section 'machine' is missing.");
        }
        if (string.IsNullOrWhiteSpace(section.Name))
        {
            throw new AdhocMachineValidationException(
                "Required field 'machine.name' is missing or empty.");
        }
        if (string.IsNullOrWhiteSpace(section.VideoStandard))
        {
            throw new AdhocMachineValidationException(
                "Required field 'machine.videoStandard' is missing.");
        }
        if (!Enum.TryParse<VideoStandard>(section.VideoStandard, ignoreCase: true, out var videoStandard))
        {
            throw new AdhocMachineValidationException(
                $"machine.videoStandard '{section.VideoStandard}' is not a valid VideoStandard (Pal | Ntsc).");
        }
        if (section.MasterClockHz is null)
        {
            throw new AdhocMachineValidationException(
                "Required field 'machine.masterClockHz' is missing.");
        }
        if (section.MasterClockHz <= 0)
        {
            throw new AdhocMachineValidationException(
                $"machine.masterClockHz must be positive but was {section.MasterClockHz}.");
        }

        return new AdhocArchitectureDescriptor(section.Name!, section.MasterClockHz!.Value, videoStandard);
    }

    private static List<AdhocInterruptLinePlan> ValidateInterruptLines(List<AdhocInterruptLineDefinition>? raw)
    {
        if (raw is null || raw.Count == 0)
        {
            return new List<AdhocInterruptLinePlan>();
        }
        var result = new List<AdhocInterruptLinePlan>(raw.Count);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < raw.Count; i++)
        {
            var line = raw[i];
            if (string.IsNullOrWhiteSpace(line.Id))
            {
                throw new AdhocMachineValidationException(
                    $"interruptLines[{i}].id is missing or empty.");
            }
            if (!seenIds.Add(line.Id))
            {
                throw new AdhocMachineValidationException(
                    $"interruptLines[{i}].id '{line.Id}' is duplicated.");
            }
            if (string.IsNullOrWhiteSpace(line.Type))
            {
                throw new AdhocMachineValidationException(
                    $"interruptLines[{i}].type is missing.");
            }
            if (!Enum.TryParse<InterruptType>(line.Type, ignoreCase: true, out var type))
            {
                throw new AdhocMachineValidationException(
                    $"interruptLines[{i}].type '{line.Type}' is not valid (Irq | Nmi | Reset).");
            }
            result.Add(new AdhocInterruptLinePlan { Id = line.Id, Type = type });
        }
        return result;
    }

    private static List<AdhocMemoryRegionPlan> ValidateMemorySection(AdhocMemorySection? section)
    {
        if (section?.Regions is null || section.Regions.Count == 0)
        {
            throw new AdhocMachineValidationException(
                "memory.regions must contain at least one region.");
        }

        var plans = new List<AdhocMemoryRegionPlan>(section.Regions.Count);
        for (var i = 0; i < section.Regions.Count; i++)
        {
            var region = section.Regions[i];
            if (string.IsNullOrWhiteSpace(region.Id))
            {
                throw new AdhocMachineValidationException(
                    $"memory.regions[{i}].id is missing or empty.");
            }
            if (string.IsNullOrWhiteSpace(region.Kind))
            {
                throw new AdhocMachineValidationException(
                    $"memory.regions[{i}].kind is missing.");
            }
            if (!Enum.TryParse<AdhocMemoryKind>(region.Kind, ignoreCase: true, out var kind))
            {
                throw new AdhocMachineValidationException(
                    $"memory.regions[{i}].kind '{region.Kind}' is not valid (Ram | Rom).");
            }
            var start = ParseUInt16($"memory.regions[{i}].start", region.Start);
            var end = ParseUInt16($"memory.regions[{i}].end", region.End);
            if (end < start)
            {
                throw new AdhocMachineValidationException(
                    $"memory.regions[{i}].end (0x{end:X4}) must be >= start (0x{start:X4}).");
            }
            if (region.Size is int declaredSize)
            {
                var actualSize = end - start + 1;
                if (declaredSize != actualSize)
                {
                    throw new AdhocMachineValidationException(
                        $"memory.regions[{i}].size {declaredSize} does not match (end - start + 1) = {actualSize}.");
                }
            }

            plans.Add(new AdhocMemoryRegionPlan
            {
                Index = i,
                Id = region.Id!,
                Kind = kind,
                Start = start,
                End = end,
            });
        }
        return plans;
    }

    private static List<AdhocChipPlan> ValidateChips(List<AdhocChipDefinition>? raw, List<AdhocInterruptLinePlan> lines)
    {
        if (raw is null || raw.Count == 0)
        {
            throw new AdhocMachineValidationException(
                "chips list must contain at least one chip.");
        }

        var plans = new List<AdhocChipPlan>(raw.Count);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lineIds = new HashSet<string>(lines.Select(l => l.Id), StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < raw.Count; i++)
        {
            var chip = raw[i];
            if (string.IsNullOrWhiteSpace(chip.Id))
            {
                throw new AdhocMachineValidationException(
                    $"chips[{i}].id is missing or empty.");
            }
            if (!seenIds.Add(chip.Id))
            {
                throw new AdhocMachineValidationException(
                    $"chips[{i}].id '{chip.Id}' is duplicated.");
            }
            if (string.IsNullOrWhiteSpace(chip.Type))
            {
                throw new AdhocMachineValidationException(
                    $"chips[{i}].type is missing.");
            }
            if (!Enum.TryParse<AdhocChipType>(chip.Type, ignoreCase: true, out var type))
            {
                throw new AdhocMachineValidationException(
                    $"chips[{i}].type '{chip.Type}' is not a supported chip type. Allowed: Mos6502, Mos6526, Mos6569, Sid6581.");
            }

            DeviceRole? role = null;
            if (!string.IsNullOrWhiteSpace(chip.Role))
            {
                if (!Enum.TryParse<DeviceRole>(chip.Role, ignoreCase: true, out var parsedRole))
                {
                    throw new AdhocMachineValidationException(
                        $"chips[{i}].role '{chip.Role}' is not a valid DeviceRole.");
                }
                role = parsedRole;
            }

            ushort? baseAddress = null;
            // Chips whose I/O window is configurable per-machine (CIA, VIC-II)
            // must supply a baseAddress. Chips with a fixed window (SID at
            // 0xD400, CPU at no address) accept none.
            var requiresBaseAddress = type is AdhocChipType.Mos6526 or AdhocChipType.Mos6569;
            var forbidsBaseAddress = type is AdhocChipType.Mos6502 or AdhocChipType.Sid6581;
            if (!string.IsNullOrWhiteSpace(chip.BaseAddress))
            {
                if (forbidsBaseAddress)
                {
                    throw new AdhocMachineValidationException(
                        $"chips[{i}].baseAddress must not be specified for chip type '{type}'.");
                }
                baseAddress = ParseUInt16($"chips[{i}].baseAddress", chip.BaseAddress);
            }
            else if (requiresBaseAddress)
            {
                throw new AdhocMachineValidationException(
                    $"chips[{i}].baseAddress is required for chip type '{type}'.");
            }

            ValidateLineRef($"chips[{i}].irqLine", chip.IrqLine, lineIds);
            ValidateLineRef($"chips[{i}].nmiLine", chip.NmiLine, lineIds);

            plans.Add(new AdhocChipPlan
            {
                Index = i,
                Id = chip.Id!,
                Type = type,
                Role = role,
                BaseAddress = baseAddress,
                IrqLineId = string.IsNullOrWhiteSpace(chip.IrqLine) ? null : chip.IrqLine,
                NmiLineId = string.IsNullOrWhiteSpace(chip.NmiLine) ? null : chip.NmiLine,
            });
        }

        return plans;
    }

    private static void ValidateLineRef(string fieldPath, string? value, HashSet<string> lineIds)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }
        if (!lineIds.Contains(value))
        {
            throw new AdhocMachineValidationException(
                $"{fieldPath} '{value}' does not match any declared interruptLines[].id.");
        }
    }

    private static ushort ParseUInt16(string fieldPath, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new AdhocMachineValidationException(
                $"{fieldPath} is missing or empty.");
        }
        var trimmed = raw.Trim();
        var style = NumberStyles.Integer;
        var text = trimmed;
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = trimmed[2..];
            style = NumberStyles.HexNumber;
        }
        if (!int.TryParse(text, style, CultureInfo.InvariantCulture, out var value))
        {
            throw new AdhocMachineValidationException(
                $"{fieldPath} value '{raw}' is not a valid integer.");
        }
        if (value < 0 || value > 0xFFFF)
        {
            throw new AdhocMachineValidationException(
                $"{fieldPath} value 0x{value:X} is outside the 16-bit range 0x0000..0xFFFF.");
        }
        return (ushort)value;
    }
}
