namespace ViceSharp.Core.Vic20;

/// <summary>
/// Optional xvic-style RAM block override on a VIC-20 architecture descriptor.
/// When null, the builder uses <see cref="Vic20MemoryLayout.ParseBoardModel"/> on the profile.
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public interface IVic20RamConfiguration
{
    /// <summary>Installed expansion blocks, or null to use the profile BoardModel preset.</summary>
    Vic20RamBlocks? RamBlocksOverride { get; }
}
