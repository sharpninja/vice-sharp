using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Machine;

/// <summary>
/// FR-ROMM-BROWSE-001 (AC-BROWSE-02). Use case: the library scopes every query to the active machine's
/// RomM platform slug; the mapping must be exact for all supported machines.
/// </summary>
[Trait("Category", "Library")]
public sealed class CurrentMachineSlugTests
{
    /// <summary>AC-BROWSE-02: each machine maps to its RomM platform slug.</summary>
    [Theory]
    [Trait("AC", "AC-BROWSE-02")]
    [InlineData(LibraryMachine.C64, "c64")]
    [InlineData(LibraryMachine.C128, "c128")]
    [InlineData(LibraryMachine.Plus4, "c-plus-4")]
    [InlineData(LibraryMachine.Vic20, "vic-20")]
    [InlineData(LibraryMachine.Pet, "cpet")]
    public void ToSlug_MapsEachMachine(LibraryMachine machine, string slug)
    {
        MachinePlatformSlug.ToSlug(machine).Should().Be(slug);
    }

    /// <summary>AC-BROWSE-02: the reverse mapping round-trips a known slug, case-insensitively.</summary>
    [Theory]
    [Trait("AC", "AC-BROWSE-02")]
    [InlineData("c64", LibraryMachine.C64)]
    [InlineData("c-plus-4", LibraryMachine.Plus4)]
    [InlineData("CPET", LibraryMachine.Pet)]
    public void TryFromSlug_RoundTrips(string slug, LibraryMachine expected)
    {
        MachinePlatformSlug.TryFromSlug(slug, out LibraryMachine machine).Should().BeTrue();
        machine.Should().Be(expected);
    }

    /// <summary>AC-BROWSE-02: an unknown slug is rejected.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-02")]
    public void TryFromSlug_UnknownRejected()
    {
        MachinePlatformSlug.TryFromSlug("amiga", out _).Should().BeFalse();
    }
}
