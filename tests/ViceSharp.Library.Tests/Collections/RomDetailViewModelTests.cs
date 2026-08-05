using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Collections;

/// <summary>
/// FR-ROMM-DETAIL-001 (AC-DETAIL-02). Use case: the details page can add its ROM to a collection.
/// </summary>
[Trait("Category", "Library")]
public sealed class RomDetailViewModelTests
{
    /// <summary>AC-DETAIL-02: AddToCollection invokes the gateway with this ROM's id.</summary>
    [Fact]
    [Trait("AC", "AC-DETAIL-02")]
    public async Task AddToCollection_Invokes()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeCollectionsGateway();
        var detail = new RomDetail(101, "Boulder Dash", null, "c64", null, Array.Empty<RomFile>(), Array.Empty<int>());
        var vm = new RomDetailViewModel(detail, gateway);

        await vm.AddToCollectionAsync(5, ct);

        gateway.Added.Should().ContainSingle();
        gateway.Added[0].Id.Should().Be(5);
        gateway.Added[0].Roms.Should().Equal(101);
    }
}
