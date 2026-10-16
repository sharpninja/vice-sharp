using FluentAssertions;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Csdb;

/// <summary>
/// FR-CSDB-001 (AC-CSDB-06). Use case: the head picks the co-located gateway when the RomM roms root is
/// writable, and the bridge otherwise.
/// </summary>
[Trait("Category", "Library")]
public sealed class CsdbGatewaySelectionTests
{
    /// <summary>AC-CSDB-06: a writable roms root selects Local; absent/null selects Bridge.</summary>
    [Fact]
    [Trait("AC", "AC-CSDB-06")]
    public void PicksByConfig()
    {
        DirectoryInfo writable = Directory.CreateTempSubdirectory("vs-romm-csdb");
        try
        {
            CsdbGatewaySelection.Select(writable.FullName).Should().Be(CsdbGatewayMode.Local);
        }
        finally
        {
            writable.Delete(recursive: true);
        }

        CsdbGatewaySelection.Select(null).Should().Be(CsdbGatewayMode.Bridge);
        CsdbGatewaySelection.Select(string.Empty).Should().Be(CsdbGatewayMode.Bridge);

        string missing = Path.Combine(Path.GetTempPath(), "vs-romm-missing-" + Guid.NewGuid().ToString("N"));
        CsdbGatewaySelection.Select(missing).Should().Be(CsdbGatewayMode.Bridge);
    }
}
