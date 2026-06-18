namespace ViceSharp.Abstractions;

/// <summary>
/// A chip that can serialize its full internal state into a compact byte buffer for the
/// time-travel debugger, and decode such a buffer back into named register/field values for
/// display. Capture is on the per-instruction hot path, so it must be zero-allocation
/// (write into the caller's span); decode runs on demand (when a tick is inspected) and may
/// allocate.
/// </summary>
public interface IStatefulDevice
{
    /// <summary>Human-readable chip name shown in the debug screen (e.g. "VIC-II", "SID").</summary>
    string StateName { get; }

    /// <summary>Number of bytes <see cref="CaptureState"/> writes.</summary>
    int StateSize { get; }

    /// <summary>Serialize the chip's current internal state into <paramref name="destination"/>
    /// (at least <see cref="StateSize"/> bytes). Must not allocate.</summary>
    void CaptureState(Span<byte> destination);

    /// <summary>Decode a previously captured state buffer into named fields for display.</summary>
    IReadOnlyList<ChipStateField> DecodeState(ReadOnlySpan<byte> state);
}

/// <summary>One named value in a decoded chip state (e.g. "$D012 RASTER" = 0x37).</summary>
/// <param name="Name">Display name.</param>
/// <param name="Value">The value.</param>
/// <param name="Width">Hex width in bytes for formatting (1 = "XX", 2 = "XXXX").</param>
public readonly record struct ChipStateField(string Name, int Value, int Width = 1);
