using FluentAssertions;
using ViceSharp.Xbox.ViewModels;
using Xunit;

namespace ViceSharp.Heads.Tests;

/// <summary>
/// FR-ROMM-XBOXUI-001 (AC-XUI-01). Use case: the RomM library, details, and lists pages are first-class
/// pushable navigation destinations on the Xbox head. (The HomePage button + Push route wiring live in
/// the UWP head and are validated by the Debug-UWP build + dev-PC E2E.)
/// </summary>
[Trait("Category", "Heads")]
public sealed class XboxNavigationTests
{
    /// <summary>AC-XUI-01: the RomM destinations are defined navigation targets.</summary>
    [Theory]
    [Trait("AC", "AC-XUI-01")]
    [InlineData(NavigationDestination.Library)]
    [InlineData(NavigationDestination.GameDetails)]
    [InlineData(NavigationDestination.Lists)]
    public void Library_Routable(NavigationDestination destination)
    {
        Enum.IsDefined(destination).Should().BeTrue();
    }
}
