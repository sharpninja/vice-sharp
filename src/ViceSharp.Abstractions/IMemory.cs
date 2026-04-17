namespace ViceSharp.Abstractions;

/// <summary>
/// Represents a linear block of RAM or ROM memory.
/// </summary>
public interface IMemory : IAddressSpace
{
    /// <summary>Direct access to underlying memory span</summary>
    Span<byte> Span { get; }
}