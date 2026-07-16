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
}
