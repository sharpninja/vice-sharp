using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Adapter;

/// <summary>
/// TR-ROMM-JSON-001. Use case: every DTO the adapter deserializes itself must resolve through the
/// source-generated <see cref="RomMJsonContext"/> (reflection-free, AOT/trim-safe). L2 registers the
/// A-Z char index.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomMJsonContextTests
{
    /// <summary>TR-ROMM-JSON-001: char_index deserializes via the source-generated type info.</summary>
    [Fact]
    [Trait("AC", "TR-ROMM-JSON-001")]
    public void CharIndex_SourceGen()
    {
        JsonTypeInfo? info = RomMJsonContext.Default.GetTypeInfo(typeof(Dictionary<string, int>));
        info.Should().NotBeNull("char_index must be registered for source-gen deserialization");

        var typed = (JsonTypeInfo<Dictionary<string, int>>)info!;
        Dictionary<string, int>? dict = JsonSerializer.Deserialize("""{"B":0,"H":1}""", typed);

        dict.Should().NotBeNull();
        dict!["B"].Should().Be(0);
        dict["H"].Should().Be(1);
    }

    /// <summary>AC-COLLECT-06: the collection DTOs deserialize/serialize via the source-generated context.</summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-06")]
    public void Collections_SourceGenOnly()
    {
        RomMJsonContext.Default.GetTypeInfo(typeof(RomMCollectionDto)).Should().NotBeNull();
        RomMJsonContext.Default.GetTypeInfo(typeof(List<RomMCollectionDto>)).Should().NotBeNull();
        RomMJsonContext.Default.GetTypeInfo(typeof(RomMCollectionRomsPayload)).Should().NotBeNull();

        var dtoInfo = (JsonTypeInfo<RomMCollectionDto>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMCollectionDto))!;
        RomMCollectionDto? dto = JsonSerializer.Deserialize(
            """{"id":1,"name":"Favorites","rom_count":12,"rom_ids":[10,11],"is_smart":true}""", dtoInfo);
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(1);
        dto.Name.Should().Be("Favorites");
        dto.RomCount.Should().Be(12);
        dto.RomIds.Should().Equal(10, 11);
        dto.IsSmart.Should().BeTrue();

        var payloadInfo = (JsonTypeInfo<RomMCollectionRomsPayload>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMCollectionRomsPayload))!;
        string json = JsonSerializer.Serialize(new RomMCollectionRomsPayload { RomIds = new List<int> { 5, 6 } }, payloadInfo);
        json.Should().Contain("rom_ids");
        json.Should().Contain("5");
    }
}
