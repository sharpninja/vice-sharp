using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Connection;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-04). Use case: a 401 must surface a re-authentication prompt.
/// </summary>
[Trait("Category", "Library")]
public sealed class LibraryConnectionViewModelTests
{
    /// <summary>AC-CONN-04: HandleUnauthorized moves to ReauthRequired and raises ConnectionInvalid.</summary>
    [Fact]
    [Trait("AC", "AC-CONN-04")]
    public void Unauthorized_SurfacesReauth()
    {
        var vm = new LibraryConnectionViewModel();
        vm.MarkConnected();
        vm.State.Should().Be(ConnectionState.Connected);

        int raised = 0;
        vm.ConnectionInvalid += (_, _) => raised++;

        vm.HandleUnauthorized();

        vm.State.Should().Be(ConnectionState.ReauthRequired);
        raised.Should().Be(1);
    }
}
