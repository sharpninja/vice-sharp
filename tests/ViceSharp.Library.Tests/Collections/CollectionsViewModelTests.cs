using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Collections;

/// <summary>
/// FR-ROMM-COLLECT-001 (AC-COLLECT-05). Use case: editing a collection's membership persists to the
/// server and then refreshes the local view.
/// </summary>
[Trait("Category", "Library")]
public sealed class CollectionsViewModelTests
{
    /// <summary>AC-COLLECT-05: add/remove roms call the gateway and then refresh the collections.</summary>
    [Fact]
    [Trait("AC", "AC-COLLECT-05")]
    public async Task AddRemove_Refreshes()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeCollectionsGateway(new[]
        {
            new LibraryCollection(1, "Favorites", 2, false, new[] { 10, 11 }),
        });
        var vm = new CollectionsViewModel(gateway);

        await vm.AddRomsAsync(1, new[] { 12 }, ct);

        gateway.Added.Should().ContainSingle();
        gateway.Added[0].Id.Should().Be(1);
        gateway.Added[0].Roms.Should().Equal(12);
        gateway.GetCalls.Should().BeGreaterThan(0);
        vm.Collections.Should().NotBeEmpty();

        await vm.RemoveRomsAsync(1, new[] { 10 }, ct);

        gateway.Removed.Should().ContainSingle();
        gateway.Removed[0].Roms.Should().Equal(10);
    }
}
