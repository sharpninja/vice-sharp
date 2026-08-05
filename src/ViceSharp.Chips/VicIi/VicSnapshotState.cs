namespace ViceSharp.Chips.VicIi;

/// <summary>
/// FIX-XSNAPWARP-001: the VIC-II state a machine snapshot round-trips, exactly the
/// fields <see cref="Mos6569.InjectSnapshotState"/> consumes (TR-LOCKSTEP-VSF-001
/// shape: the register file, the raster phase, the .vsf badline/display latches, and
/// the mid-frame video counters that only re-derive at frame top).
/// </summary>
/// <param name="Registers">The 64-byte register file copy.</param>
/// <param name="RasterLine">Current raster line (9-bit).</param>
/// <param name="InLineCycle">Current in-line cycle (RasterX).</param>
/// <param name="AllowBadLines">The .vsf allow_bad_lines latch.</param>
/// <param name="IdleState">The display/idle latch.</param>
/// <param name="VideoCounter">VC (10-bit).</param>
/// <param name="VideoCounterBase">VCBASE (10-bit).</param>
/// <param name="RowCounter">RC (3-bit).</param>
/// <param name="VideoMatrixLineIndex">VMLI.</param>
/// <param name="RefreshCounter">The DRAM refresh counter.</param>
/// <param name="SpriteDmaActiveMask">Per-sprite DMA-active mask.</param>
public sealed record VicSnapshotState(
    byte[] Registers,
    ushort RasterLine,
    byte InLineCycle,
    bool AllowBadLines,
    bool IdleState,
    ushort VideoCounter,
    ushort VideoCounterBase,
    byte RowCounter,
    byte VideoMatrixLineIndex,
    byte RefreshCounter,
    byte SpriteDmaActiveMask);
