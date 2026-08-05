using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using Xunit;

namespace ViceSharp.Library.Tests.Media;

/// <summary>
/// FR-ROMM-LAUNCH-001 (AC-LAUNCH-02, AC-LAUNCH-03). Use case: the library attaches a downloaded game
/// to the correct slot and refuses to "launch" content that cannot boot.
/// </summary>
[Trait("Category", "Library")]
public sealed class MediaExtensionMapTests
{
    /// <summary>AC-LAUNCH-02: known extensions map to the expected slot, kind and launchability.</summary>
    [Theory]
    [Trait("AC", "AC-LAUNCH-02")]
    [InlineData("boulderdash.d64", MediaSlot.Drive8, MediaKind.Disk, true)]
    [InlineData("game.g64", MediaSlot.Drive8, MediaKind.Disk, true)]
    [InlineData("side.d81", MediaSlot.Drive8, MediaKind.Disk, true)]
    [InlineData("creatures.tap", MediaSlot.Tape, MediaKind.Tape, true)]
    [InlineData("game.t64", MediaSlot.Drive8, MediaKind.Tape, true)]
    [InlineData("wizball.crt", MediaSlot.Cartridge, MediaKind.Cartridge, true)]
    [InlineData("dump.bin", MediaSlot.Cartridge, MediaKind.Cartridge, true)]
    public void Extension_MapsSlot(string fileName, MediaSlot slot, MediaKind kind, bool launchable)
    {
        MediaMapping? mapping = MediaExtensionMap.Resolve(fileName);

        mapping.Should().NotBeNull();
        mapping!.Value.Slot.Should().Be(slot);
        mapping.Value.Kind.Should().Be(kind);
        mapping.Value.IsLaunchable.Should().Be(launchable);
    }

    /// <summary>AC-LAUNCH-03: a raw .prg is not attachable and not launchable.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-03")]
    public void Prg_NotLaunchable()
    {
        MediaMapping? mapping = MediaExtensionMap.Resolve("hello.prg");

        mapping.Should().NotBeNull();
        mapping!.Value.Kind.Should().Be(MediaKind.Program);
        mapping.Value.Slot.Should().BeNull();
        mapping.Value.IsLaunchable.Should().BeFalse();
        MediaExtensionMap.IsLaunchable("hello.prg").Should().BeFalse();
    }

    /// <summary>AC-LAUNCH-02: an unknown extension resolves to null (not attachable).</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-02")]
    public void Unknown_ReturnsNull()
    {
        MediaExtensionMap.Resolve("notes.txt").Should().BeNull();
        MediaExtensionMap.IsLaunchable("notes.txt").Should().BeFalse();
    }

    /// <summary>AC-LAUNCH-02: a bare extension resolves the same as a full file name.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-02")]
    public void BareExtension_Resolves()
    {
        MediaExtensionMap.Resolve(".d64")!.Value.Slot.Should().Be(MediaSlot.Drive8);
    }
}
