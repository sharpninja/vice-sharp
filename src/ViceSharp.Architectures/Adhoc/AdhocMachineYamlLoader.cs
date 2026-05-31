using System.Globalization;
using ViceSharp.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// Reads an ad-hoc machine architecture YAML document, validates it
/// against schema v1, and produces a <see cref="AdhocMachineBlueprint"/>
/// that can build a runnable <see cref="IMachine"/>.
/// </summary>
/// <remarks>
/// Uses YamlDotNet's low-level <see cref="YamlStream"/> representation
/// model with manual field binding so the loader stays NativeAOT and
/// trim-safe. Earlier revisions used the reflection-emit IDeserializer
/// which produced IL2104/IL3053 warnings under AOT publish; the
/// representation-model path avoids reflection-emit entirely.
/// </remarks>
public sealed class AdhocMachineYamlLoader
{
    private const int SupportedSchemaVersion = 1;

    public AdhocMachineYamlLoader()
    {
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
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            if (stream.Documents.Count == 0)
            {
                throw new AdhocMachineValidationException("yaml document is empty.");
            }
            var root = stream.Documents[0].RootNode;
            if (root is not YamlMappingNode rootMap)
            {
                throw new AdhocMachineValidationException("yaml document root must be a mapping.");
            }
            doc = BindDocument(rootMap);
        }
        catch (YamlException ex)
        {
            throw new AdhocMachineValidationException(
                $"yaml parse error: {ex.Message}", ex);
        }

        return Validate(doc);
    }

    private static AdhocMachineDocument BindDocument(YamlMappingNode root)
    {
        var doc = new AdhocMachineDocument();
        foreach (var (key, value) in EnumerateMapping(root))
        {
            switch (key)
            {
                case "schemaVersion":
                    doc.SchemaVersion = ParseInt(value);
                    break;
                case "machine":
                    doc.Machine = BindMachineSection(RequireMapping(value, "machine"));
                    break;
                case "memory":
                    doc.Memory = BindMemorySection(RequireMapping(value, "memory"));
                    break;
                case "chips":
                    doc.Chips = BindChipList(RequireSequence(value, "chips"));
                    break;
                case "interruptLines":
                    doc.InterruptLines = BindInterruptLineList(RequireSequence(value, "interruptLines"));
                    break;
                default:
                    // Ignore unknown top-level keys (matches IgnoreUnmatchedProperties).
                    break;
            }
        }
        return doc;
    }

    private static AdhocMachineSection BindMachineSection(YamlMappingNode map)
    {
        var section = new AdhocMachineSection();
        foreach (var (key, value) in EnumerateMapping(map))
        {
            switch (key)
            {
                case "name":
                    section.Name = ParseString(value);
                    break;
                case "videoStandard":
                    section.VideoStandard = ParseString(value);
                    break;
                case "masterClockHz":
                    section.MasterClockHz = ParseLong(value);
                    break;
                case "resetVector":
                    section.ResetVector = ParseString(value);
                    break;
            }
        }
        return section;
    }

    private static AdhocMemorySection BindMemorySection(YamlMappingNode map)
    {
        var section = new AdhocMemorySection();
        foreach (var (key, value) in EnumerateMapping(map))
        {
            if (key == "regions")
            {
                section.Regions = new List<AdhocMemoryRegion>();
                foreach (var item in RequireSequence(value, "memory.regions"))
                {
                    section.Regions.Add(BindMemoryRegion(RequireMapping(item, "memory.regions[]")));
                }
            }
        }
        return section;
    }

    private static AdhocMemoryRegion BindMemoryRegion(YamlMappingNode map)
    {
        var region = new AdhocMemoryRegion();
        foreach (var (key, value) in EnumerateMapping(map))
        {
            switch (key)
            {
                case "id":
                    region.Id = ParseString(value);
                    break;
                case "kind":
                    region.Kind = ParseString(value);
                    break;
                case "start":
                    region.Start = ParseString(value);
                    break;
                case "end":
                    region.End = ParseString(value);
                    break;
                case "size":
                    region.Size = ParseInt(value);
                    break;
            }
        }
        return region;
    }

    private static List<AdhocChipDefinition> BindChipList(YamlSequenceNode seq)
    {
        var list = new List<AdhocChipDefinition>();
        foreach (var node in seq)
        {
            list.Add(BindChip(RequireMapping(node, "chips[]")));
        }
        return list;
    }

    private static AdhocChipDefinition BindChip(YamlMappingNode map)
    {
        var chip = new AdhocChipDefinition();
        foreach (var (key, value) in EnumerateMapping(map))
        {
            switch (key)
            {
                case "id":
                    chip.Id = ParseString(value);
                    break;
                case "type":
                    chip.Type = ParseString(value);
                    break;
                case "role":
                    chip.Role = ParseString(value);
                    break;
                case "baseAddress":
                    chip.BaseAddress = ParseString(value);
                    break;
                case "irqLine":
                    chip.IrqLine = ParseString(value);
                    break;
                case "nmiLine":
                    chip.NmiLine = ParseString(value);
                    break;
            }
        }
        return chip;
    }

    private static List<AdhocInterruptLineDefinition> BindInterruptLineList(YamlSequenceNode seq)
    {
        var list = new List<AdhocInterruptLineDefinition>();
        foreach (var node in seq)
        {
            var line = new AdhocInterruptLineDefinition();
            foreach (var (key, value) in EnumerateMapping(RequireMapping(node, "interruptLines[]")))
            {
                switch (key)
                {
                    case "id":
                        line.Id = ParseString(value);
                        break;
                    case "type":
                        line.Type = ParseString(value);
                        break;
                }
            }
            list.Add(line);
        }
        return list;
    }

    // ---- YAML helpers ----

    internal static IEnumerable<(string Key, YamlNode Value)> EnumerateMapping(YamlMappingNode map)
    {
        foreach (var entry in map.Children)
        {
            var keyNode = entry.Key as YamlScalarNode;
            if (keyNode is null || keyNode.Value is null) continue;
            yield return (keyNode.Value, entry.Value);
        }
    }

    internal static YamlMappingNode RequireMapping(YamlNode node, string path)
    {
        return node as YamlMappingNode
            ?? throw new AdhocMachineValidationException($"'{path}' must be a mapping.");
    }

    internal static YamlSequenceNode RequireSequence(YamlNode node, string path)
    {
        return node as YamlSequenceNode
            ?? throw new AdhocMachineValidationException($"'{path}' must be a sequence.");
    }

    internal static string? ParseString(YamlNode node)
    {
        if (node is YamlScalarNode scalar)
        {
            // YamlScalarNode.Value is null for null literal; otherwise the raw string.
            return scalar.Value;
        }
        return null;
    }

    internal static int? ParseInt(YamlNode node)
    {
        var s = ParseString(node);
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        return null;
    }

    internal static long? ParseLong(YamlNode node)
    {
        var s = ParseString(node);
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        return null;
    }

    // ---- Validation (unchanged from prior revision) ----

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
