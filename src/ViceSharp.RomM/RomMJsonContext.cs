using System.Text.Json.Serialization;

namespace ViceSharp.RomM;

/// <summary>
/// TR-ROMM-JSON-001. The source-generated JSON context for every DTO the adapter serializes or
/// deserializes itself (reflection-free, AOT/trim-safe). It grows one type per slice: L2 registers the
/// A-Z <c>char_index</c> map; L5 registers the collection DTO, its list, and the roms payload. RomM.Client's
/// own typed responses are deserialized inside that package and are not this context's concern. It is
/// internal (exposed to the test vessel via InternalsVisibleTo) so the internal wire DTOs stay off the
/// adapter's public surface.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(RomMCollectionDto))]
[JsonSerializable(typeof(List<RomMCollectionDto>))]
[JsonSerializable(typeof(RomMCollectionRomsPayload))]
[JsonSerializable(typeof(RomMBridgeIngestRequest))]
[JsonSerializable(typeof(RomMBridgeIngestResponse))]
internal sealed partial class RomMJsonContext : JsonSerializerContext;
