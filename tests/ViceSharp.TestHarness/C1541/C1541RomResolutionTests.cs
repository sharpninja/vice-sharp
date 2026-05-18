namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-001 (Phase B1a).
/// Use case: A test or console host instantiates a C1541Descriptor + a
/// RomProvider; the descriptor's RequiredRoms must resolve the 16KB DOS
/// image from the native VICE submodule fallback path.
/// </summary>
public sealed class C1541RomResolutionTests
{
    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Descriptor declares the drive machine with master clock 1 MHz,
    /// PAL standard, and required ROM set rooted in the "DRIVES" architecture
    /// key.
    /// Acceptance: Descriptor returns correct name, clock, and RequiredRoms is
    /// a C1541RomSet under architecture "DRIVES".
    /// </summary>
    [Fact]
    public void Descriptor_ExposesNameClockAndRomSet()
    {
        var desc = new C1541Descriptor();

        desc.MachineName.Should().Be("Commodore 1541");
        desc.MasterClockHz.Should().Be(1_000_000);
        desc.VideoStandard.Should().Be(VideoStandard.Pal);
        desc.RequiredRoms.Should().BeOfType<C1541RomSet>();
        ((C1541RomSet)desc.RequiredRoms!).Architecture.Should().Be(C1541ViceRomNames.ArchitectureKey);
        ((C1541RomSet)desc.RequiredRoms!).DosRomName.Should().Be(C1541ViceRomNames.Dos1541);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: An IRomProvider wired with the standard repo + VICE submodule
    /// fallback resolves the standard 1541 DOS ROM.
    /// Acceptance: IsComplete returns true; LoadRom returns a 16384-byte buffer.
    /// </summary>
    [Fact]
    public void RomProvider_ResolvesStandard1541Dos_FromNativeViceSubmodule()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var set = new C1541RomSet();

        set.IsComplete(provider).Should().BeTrue();
        var rom = provider.LoadRom(C1541ViceRomNames.Dos1541, C1541ViceRomNames.ArchitectureKey);
        rom.Length.Should().Be(C1541ViceRomNames.Dos1541RomSize);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: 1541-II variant is selectable.
    /// Acceptance: Descriptor with explicit 1541-II ROM name resolves the
    /// 16KB image; the RomSet IsComplete check passes.
    /// </summary>
    [Fact]
    public void Descriptor_With1541IiVariant_ResolvesAlternateRom()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var desc = new C1541Descriptor(C1541ViceRomNames.Dos1541Ii);

        desc.RequiredRoms!.IsComplete(provider).Should().BeTrue();
        var rom = provider.LoadRom(C1541ViceRomNames.Dos1541Ii, C1541ViceRomNames.ArchitectureKey);
        rom.Length.Should().Be(C1541ViceRomNames.Dos1541RomSize);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: An incomplete provider (missing the drive ROM) must report
    /// the set as incomplete so the builder can refuse construction with a
    /// clear error.
    /// Acceptance: IsComplete returns false for a provider that lacks the
    /// requested ROM.
    /// </summary>
    [Fact]
    public void RomSet_WithMissingRom_IsNotComplete()
    {
        var emptyProvider = new EmptyRomProvider();
        var set = new C1541RomSet();

        set.IsComplete(emptyProvider).Should().BeFalse();
    }

    private sealed class EmptyRomProvider : IRomProvider
    {
        public bool IsAvailable(string romName, string architecture) => false;
        public ReadOnlyMemory<byte> LoadRom(string romName, string architecture) =>
            throw new InvalidOperationException("EmptyRomProvider has no ROMs.");
    }
}
