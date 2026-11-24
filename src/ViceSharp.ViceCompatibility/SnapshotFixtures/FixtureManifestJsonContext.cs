using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>Source-generated JSON contracts (trim and AOT safe) for the manifest, the candidates and the classifications.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(FixtureManifest))]
[JsonSerializable(typeof(FixtureManifestEntry))]
[JsonSerializable(typeof(FixtureCandidate))]
[JsonSerializable(typeof(FixtureClassification))]
[JsonSerializable(typeof(List<FixtureBlockerRecord>))]
[JsonSerializable(typeof(JsonElement))]
public sealed partial class FixtureManifestJsonContext : JsonSerializerContext
{
}
