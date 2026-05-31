using ViceSharp.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace ViceSharp.Architectures.Multisystem;

/// <summary>
/// Reads a multi-system topology YAML document, validates it against schema
/// v1, and produces a <see cref="MultiSystemBlueprint"/>. The loader is the
/// sibling of <c>AdhocMachineYamlLoader</c>; presence of the top-level
/// <c>coordinator:</c> key triggers multi-system mode.
/// </summary>
/// <remarks>
/// Uses YamlDotNet's low-level <see cref="YamlStream"/> representation
/// model with manual field binding. The reflection-emit IDeserializer path
/// is intentionally avoided so this loader stays NativeAOT and trim-safe.
/// </remarks>
public sealed class MultiSystemYamlLoader
{
    private const int SupportedSchemaVersion = 1;

    public MultiSystemYamlLoader()
    {
    }

    /// <summary>True when the YAML at <paramref name="path"/> declares a coordinator section.</summary>
    public bool IsMultiSystemFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path)) return false;
        return IsMultiSystemText(File.ReadAllText(path));
    }

    /// <summary>True when the YAML text declares a coordinator section.</summary>
    public bool IsMultiSystemText(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml)) return false;
        foreach (var raw in yaml.Split('\n'))
        {
            var line = raw.TrimEnd('\r').TrimStart();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("coordinator:", StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>Read and validate the multi-system YAML at <paramref name="path"/>.</summary>
    public MultiSystemBlueprint LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Multi-system YAML file not found: {path}", path);
        var yaml = File.ReadAllText(path);
        var basePath = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Environment.CurrentDirectory;
        return LoadFromString(yaml, basePath);
    }

    /// <summary>
    /// Parse and validate <paramref name="yaml"/>. <paramref name="basePath"/>
    /// resolves yamlPath references for embedded machine specs (relative paths).
    /// </summary>
    public MultiSystemBlueprint LoadFromString(string yaml, string? basePath = null)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        basePath ??= Environment.CurrentDirectory;

        MultiSystemDocument doc;
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            if (stream.Documents.Count == 0)
            {
                throw new MultiSystemValidationException("yaml document is empty or not a mapping.");
            }
            var root = stream.Documents[0].RootNode;
            if (root is not YamlMappingNode rootMap)
            {
                throw new MultiSystemValidationException("yaml document root must be a mapping.");
            }
            doc = BindDocument(rootMap);
        }
        catch (YamlException ex)
        {
            throw new MultiSystemValidationException($"yaml parse error: {ex.Message}", ex);
        }

        return Validate(doc, basePath);
    }

    // ---- Binding helpers (low-level YamlStream -> DTO) ----

    private static MultiSystemDocument BindDocument(YamlMappingNode root)
    {
        var doc = new MultiSystemDocument();
        foreach (var (key, value) in EnumerateMapping(root))
        {
            switch (key)
            {
                case "schemaVersion":
                    doc.SchemaVersion = ParseInt(value);
                    break;
                case "coordinator":
                    doc.Coordinator = BindCoordinator(RequireMapping(value, "coordinator"));
                    break;
            }
        }
        return doc;
    }

    private static MultiSystemSection BindCoordinator(YamlMappingNode map)
    {
        var section = new MultiSystemSection();
        foreach (var (key, value) in EnumerateMapping(map))
        {
            switch (key)
            {
                case "host":
                    section.Host = BindMachineSpec(RequireMapping(value, "coordinator.host"));
                    break;
                case "peripherals":
                    section.Peripherals = BindPeripheralList(RequireSequence(value, "coordinator.peripherals"));
                    break;
                case "cartExtensions":
                    section.CartExtensions = BindCartExtensionList(RequireSequence(value, "coordinator.cartExtensions"));
                    break;
                case "buses":
                    section.Buses = BindBusList(RequireSequence(value, "coordinator.buses"));
                    break;
            }
        }
        return section;
    }

    private static MultiSystemMachineSpec BindMachineSpec(YamlMappingNode map)
    {
        var spec = new MultiSystemMachineSpec();
        foreach (var (key, value) in EnumerateMapping(map))
        {
            switch (key)
            {
                case "id": spec.Id = ParseString(value); break;
                case "kind": spec.Kind = ParseString(value); break;
                case "yamlPath": spec.YamlPath = ParseString(value); break;
                case "yamlInline": spec.YamlInline = ParseString(value); break;
                case "busAttachments":
                    spec.BusAttachments = BindAttachmentList(RequireSequence(value, "busAttachments"));
                    break;
            }
        }
        return spec;
    }

    private static List<MultiSystemPeripheralSpec> BindPeripheralList(YamlSequenceNode seq)
    {
        var list = new List<MultiSystemPeripheralSpec>();
        foreach (var node in seq)
        {
            var m = RequireMapping(node, "coordinator.peripherals[]");
            var spec = new MultiSystemPeripheralSpec();
            foreach (var (key, value) in EnumerateMapping(m))
            {
                switch (key)
                {
                    case "id": spec.Id = ParseString(value); break;
                    case "role": spec.Role = ParseString(value); break;
                    case "fidelity": spec.Fidelity = ParseString(value); break;
                    case "kind": spec.Kind = ParseString(value); break;
                    case "deviceNumber": spec.DeviceNumber = ParseInt(value); break;
                    case "diskImagePath": spec.DiskImagePath = ParseString(value); break;
                    case "yamlPath": spec.YamlPath = ParseString(value); break;
                    case "yamlInline": spec.YamlInline = ParseString(value); break;
                    case "busAttachments":
                        spec.BusAttachments = BindAttachmentList(RequireSequence(value, "peripherals[].busAttachments"));
                        break;
                }
            }
            list.Add(spec);
        }
        return list;
    }

    private static List<MultiSystemCartExtensionSpec> BindCartExtensionList(YamlSequenceNode seq)
    {
        var list = new List<MultiSystemCartExtensionSpec>();
        foreach (var node in seq)
        {
            var m = RequireMapping(node, "coordinator.cartExtensions[]");
            var spec = new MultiSystemCartExtensionSpec();
            foreach (var (key, value) in EnumerateMapping(m))
            {
                switch (key)
                {
                    case "id": spec.Id = ParseString(value); break;
                    case "fidelity": spec.Fidelity = ParseString(value); break;
                    case "yamlPath": spec.YamlPath = ParseString(value); break;
                    case "yamlInline": spec.YamlInline = ParseString(value); break;
                    case "busAttachments":
                        spec.BusAttachments = BindAttachmentList(RequireSequence(value, "cartExtensions[].busAttachments"));
                        break;
                }
            }
            list.Add(spec);
        }
        return list;
    }

    private static List<MultiSystemBusSpec> BindBusList(YamlSequenceNode seq)
    {
        var list = new List<MultiSystemBusSpec>();
        foreach (var node in seq)
        {
            var m = RequireMapping(node, "coordinator.buses[]");
            var bus = new MultiSystemBusSpec();
            foreach (var (key, value) in EnumerateMapping(m))
            {
                switch (key)
                {
                    case "id": bus.Id = ParseString(value); break;
                    case "signals":
                        bus.Signals = BindStringList(RequireSequence(value, "buses[].signals"));
                        break;
                }
            }
            list.Add(bus);
        }
        return list;
    }

    private static List<MultiSystemBusAttachment> BindAttachmentList(YamlSequenceNode seq)
    {
        var list = new List<MultiSystemBusAttachment>();
        foreach (var node in seq)
        {
            var m = RequireMapping(node, "busAttachments[]");
            var att = new MultiSystemBusAttachment();
            foreach (var (key, value) in EnumerateMapping(m))
            {
                switch (key)
                {
                    case "busId": att.BusId = ParseString(value); break;
                    case "endpointName": att.EndpointName = ParseString(value); break;
                }
            }
            list.Add(att);
        }
        return list;
    }

    private static List<string> BindStringList(YamlSequenceNode seq)
    {
        var list = new List<string>();
        foreach (var node in seq)
        {
            var s = ParseString(node);
            if (s is not null) list.Add(s);
        }
        return list;
    }

    private static IEnumerable<(string Key, YamlNode Value)> EnumerateMapping(YamlMappingNode map)
    {
        foreach (var entry in map.Children)
        {
            if (entry.Key is YamlScalarNode keyNode && keyNode.Value is { } keyValue)
            {
                yield return (keyValue, entry.Value);
            }
        }
    }

    private static YamlMappingNode RequireMapping(YamlNode node, string path)
    {
        return node as YamlMappingNode
            ?? throw new MultiSystemValidationException($"'{path}' must be a mapping.");
    }

    private static YamlSequenceNode RequireSequence(YamlNode node, string path)
    {
        return node as YamlSequenceNode
            ?? throw new MultiSystemValidationException($"'{path}' must be a sequence.");
    }

    private static string? ParseString(YamlNode node)
    {
        return node is YamlScalarNode scalar ? scalar.Value : null;
    }

    private static int? ParseInt(YamlNode node)
    {
        var s = ParseString(node);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : null;
    }

    // ---- Validation (unchanged) ----

    private static MultiSystemBlueprint Validate(MultiSystemDocument doc, string basePath)
    {
        if (doc.SchemaVersion is null)
            throw new MultiSystemValidationException("schemaVersion is required.");
        if (doc.SchemaVersion != SupportedSchemaVersion)
            throw new MultiSystemValidationException(
                $"unsupported schemaVersion {doc.SchemaVersion}; this loader supports {SupportedSchemaVersion}.");

        var coord = doc.Coordinator
            ?? throw new MultiSystemValidationException("coordinator section is required.");

        var host = ValidateHost(coord.Host, basePath);
        var buses = ValidateBuses(coord.Buses);
        var busIds = new HashSet<string>(buses.Select(b => b.Id), StringComparer.Ordinal);
        var systemIds = new HashSet<string>(StringComparer.Ordinal) { host.Id };

        ValidateAttachmentReferences(host.BusAttachments, host.Id, busIds);

        var peripherals = ValidatePeripherals(coord.Peripherals, basePath, busIds, systemIds);
        var extensions = ValidateCartExtensions(coord.CartExtensions, basePath, busIds, systemIds);

        return new MultiSystemBlueprint(host, peripherals, extensions, buses);
    }

    private static MultiSystemMachinePlan ValidateHost(MultiSystemMachineSpec? host, string basePath)
    {
        if (host is null)
            throw new MultiSystemValidationException("coordinator.host is required.");
        if (string.IsNullOrWhiteSpace(host.Id))
            throw new MultiSystemValidationException("coordinator.host.id is required.");
        var yaml = ResolveYamlTextOptional(host.YamlPath, host.YamlInline, basePath);
        if (yaml is null && string.IsNullOrWhiteSpace(host.Kind))
            throw new MultiSystemValidationException(
                $"coordinator.host '{host.Id}' requires either yamlPath, yamlInline, or kind.");
        var attachments = NormalizeAttachments(host.BusAttachments);
        return new MultiSystemMachinePlan(host.Id!, host.Kind, yaml, attachments);
    }

    private static IReadOnlyList<MultiSystemPeripheralPlan> ValidatePeripherals(
        List<MultiSystemPeripheralSpec>? peripherals,
        string basePath,
        HashSet<string> busIds,
        HashSet<string> systemIds)
    {
        var result = new List<MultiSystemPeripheralPlan>();
        if (peripherals is null) return result;
        foreach (var p in peripherals)
        {
            if (string.IsNullOrWhiteSpace(p.Id))
                throw new MultiSystemValidationException("coordinator.peripherals[].id is required.");
            if (!systemIds.Add(p.Id!))
                throw new MultiSystemValidationException($"duplicate system id '{p.Id}'.");
            var role = string.IsNullOrWhiteSpace(p.Role) ? "Independent" : p.Role!;
            if (role != "Independent")
                throw new MultiSystemValidationException(
                    $"coordinator.peripherals[].role '{role}' is not supported in schema v1 (use 'Independent' or omit).");
            var yaml = ResolveYamlTextOptional(p.YamlPath, p.YamlInline, basePath);
            if (yaml is null && string.IsNullOrWhiteSpace(p.Kind))
                throw new MultiSystemValidationException(
                    $"coordinator.peripherals[] '{p.Id}' requires either yamlPath, yamlInline, or kind.");
            var attachments = NormalizeAttachments(p.BusAttachments);
            ValidateAttachmentReferences(attachments, p.Id!, busIds);
            var fidelity = ParseFidelity(p.Fidelity, $"coordinator.peripherals[] '{p.Id}'");
            result.Add(new MultiSystemPeripheralPlan(p.Id!, role, fidelity, p.Kind, p.DeviceNumber, p.DiskImagePath, yaml, attachments));
        }
        return result;
    }

    private static IReadOnlyList<MultiSystemCartExtensionPlan> ValidateCartExtensions(
        List<MultiSystemCartExtensionSpec>? extensions,
        string basePath,
        HashSet<string> busIds,
        HashSet<string> systemIds)
    {
        var result = new List<MultiSystemCartExtensionPlan>();
        if (extensions is null) return result;
        foreach (var ext in extensions)
        {
            if (string.IsNullOrWhiteSpace(ext.Id))
                throw new MultiSystemValidationException("coordinator.cartExtensions[].id is required.");
            if (!systemIds.Add(ext.Id!))
                throw new MultiSystemValidationException($"duplicate system id '{ext.Id}'.");
            var yaml = ResolveYamlText(ext.YamlPath, ext.YamlInline, basePath, $"coordinator.cartExtensions[] '{ext.Id}'");
            var attachments = NormalizeAttachments(ext.BusAttachments);
            ValidateAttachmentReferences(attachments, ext.Id!, busIds);
            var fidelity = ParseFidelity(ext.Fidelity, $"coordinator.cartExtensions[] '{ext.Id}'");
            result.Add(new MultiSystemCartExtensionPlan(ext.Id!, fidelity, yaml, attachments));
        }
        return result;
    }

    private static Fidelity ParseFidelity(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return Fidelity.Buffered;
        if (Enum.TryParse<Fidelity>(value, ignoreCase: true, out var f))
            return f;
        throw new MultiSystemValidationException(
            $"{label} fidelity '{value}' is not a known value (use Buffered or TrueDevice).");
    }

    private static IReadOnlyList<MultiSystemBusPlan> ValidateBuses(List<MultiSystemBusSpec>? buses)
    {
        var result = new List<MultiSystemBusPlan>();
        if (buses is null) return result;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var b in buses)
        {
            if (string.IsNullOrWhiteSpace(b.Id))
                throw new MultiSystemValidationException("coordinator.buses[].id is required.");
            if (!seen.Add(b.Id!))
                throw new MultiSystemValidationException($"duplicate bus id '{b.Id}'.");
            if (b.Signals is null || b.Signals.Count == 0)
                throw new MultiSystemValidationException($"bus '{b.Id}' requires at least one signal.");
            foreach (var s in b.Signals)
                if (string.IsNullOrWhiteSpace(s))
                    throw new MultiSystemValidationException($"bus '{b.Id}' has an empty signal name.");
            result.Add(new MultiSystemBusPlan(b.Id!, b.Signals.ToArray()));
        }
        return result;
    }

    private static IReadOnlyList<MultiSystemBusAttachmentPlan> NormalizeAttachments(
        List<MultiSystemBusAttachment>? attachments)
    {
        var result = new List<MultiSystemBusAttachmentPlan>();
        if (attachments is null) return result;
        foreach (var a in attachments)
        {
            if (string.IsNullOrWhiteSpace(a.BusId))
                throw new MultiSystemValidationException("busAttachment.busId is required.");
            if (string.IsNullOrWhiteSpace(a.EndpointName))
                throw new MultiSystemValidationException("busAttachment.endpointName is required.");
            result.Add(new MultiSystemBusAttachmentPlan(a.BusId!, a.EndpointName!));
        }
        return result;
    }

    private static void ValidateAttachmentReferences(
        IReadOnlyList<MultiSystemBusAttachmentPlan> attachments,
        string systemId,
        HashSet<string> busIds)
    {
        foreach (var a in attachments)
            if (!busIds.Contains(a.BusId))
                throw new MultiSystemValidationException(
                    $"system '{systemId}' references unknown bus '{a.BusId}'.");
    }

    private static string ResolveYamlText(string? yamlPath, string? yamlInline, string basePath, string label)
        => ResolveYamlTextOptional(yamlPath, yamlInline, basePath)
            ?? throw new MultiSystemValidationException($"{label} requires either yamlPath or yamlInline.");

    private static string? ResolveYamlTextOptional(string? yamlPath, string? yamlInline, string basePath)
    {
        var hasPath = !string.IsNullOrWhiteSpace(yamlPath);
        var hasInline = !string.IsNullOrWhiteSpace(yamlInline);
        if (hasPath && hasInline)
            throw new MultiSystemValidationException(
                "spec declares both yamlPath and yamlInline; choose one.");
        if (!hasPath && !hasInline)
            return null;
        if (hasInline)
            return yamlInline!;

        var resolved = Path.IsPathRooted(yamlPath) ? yamlPath! : Path.Combine(basePath, yamlPath!);
        if (!File.Exists(resolved))
            throw new MultiSystemValidationException(
                $"yamlPath '{yamlPath}' does not exist (resolved to '{resolved}').");
        return File.ReadAllText(resolved);
    }
}
