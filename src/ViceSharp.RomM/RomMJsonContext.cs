using System.Text.Json.Serialization;

namespace ViceSharp.RomM;

/// <summary>
/// TR-ROMM-JSON-001. The source-generated JSON context for every DTO the adapter serializes or
/// deserializes itself (reflection-free, AOT/trim-safe). It grows one type per slice; L2 registers the
/// A-Z <c>char_index</c> map (<see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/> of
/// string to int). RomM.Client's own typed responses are deserialized inside that package and are not
/// this context's concern.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(Dictionary<string, int>))]
public sealed partial class RomMJsonContext : JsonSerializerContext;
