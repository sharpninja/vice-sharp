using System.Diagnostics.CodeAnalysis;
using ViceSharp.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ViceSharp.Architectures.Multisystem;

/// <summary>
/// Reads a multi-system topology YAML document, validates it against schema
/// v1, and produces a <see cref="MultiSystemBlueprint"/>. The loader is the
/// sibling of <c>AdhocMachineYamlLoader</c>; presence of the top-level
/// <c>coordinator:</c> key triggers multi-system mode.
/// </summary>
/// <remarks>
/// Uses YamlDotNet's reflection-based deserializer; callers using NativeAOT
/// must invoke this loader only from non-AOT code paths.
/// </remarks>
[RequiresDynamicCode(LoaderRequiresDynamicCode)]
[RequiresUnreferencedCode(LoaderRequiresUnreferencedCode)]
public sealed class MultiSystemYamlLoader
{
    internal const string LoaderRequiresDynamicCode =
        "YamlDotNet's default deserializer uses reflection emit, which is incompatible with NativeAOT.";
    internal const string LoaderRequiresUnreferencedCode =
        "YamlDotNet's default deserializer uses runtime type discovery, which is incompatible with trimming.";

    private const int SupportedSchemaVersion = 1;

    private readonly IDeserializer _deserializer;

    public MultiSystemYamlLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
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
            doc = _deserializer.Deserialize<MultiSystemDocument>(yaml)
                ?? throw new MultiSystemValidationException("yaml document is empty or not a mapping.");
        }
        catch (YamlException ex)
        {
            throw new MultiSystemValidationException($"yaml parse error: {ex.Message}", ex);
        }

        return Validate(doc, basePath);
    }

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
