using System.Buffers.Binary;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Snapshots;
using ViceSharp.Core;

namespace ViceSharp.Core.Snapshots;

public sealed class RuntimeSnapshot : ISnapshot
{
    private const int HeaderSize = 16;
    private const byte ProcessorPortFlag = 1;
    private readonly byte[] _memory;

    public RuntimeSnapshot(MachineState state, ReadOnlySpan<byte> memory)
    {
        if (memory.Length != ushort.MaxValue + 1)
            throw new ArgumentOutOfRangeException(nameof(memory), "Runtime snapshots require a complete 64K memory image.");

        State = state;
        _memory = memory.ToArray();
    }

    public RuntimeSnapshot()
    {
        State = default;
        _memory = new byte[ushort.MaxValue + 1];
    }

    public MachineState State { get; private set; }

    public ReadOnlyMemory<byte> Memory => _memory;

    /// <summary>
    /// CPU-visible $D000-$DFFF bytes captured with this image.
    /// Not serialized. Restore writes them to IO chips so VIC-II, SID, CIA,
    /// and color RAM round-trip while <see cref="Memory"/> keeps RAM under ROM.
    /// </summary>
    public byte[]? VisibleIo { get; set; }

    /// <summary>
    /// PLA data-direction and data-port latches. RAM at $0000/$0001 can differ
    /// from those latches after reset. Serialized in header bytes 13-15.
    /// Legacy images keep byte 15 clear and a full 64-bit cycle.
    /// </summary>
    public bool HasProcessorPort { get; set; }

    public byte PortDirection { get; set; }

    public byte PortData { get; set; }

    /// <summary>
    /// Datasette PLAY and RECORD at capture. Not serialized. False on images
    /// taken before the controls were stored, which leaves the buttons alone.
    /// </summary>
    public bool HasTapeControls { get; set; }

    /// <summary>PLAY button at capture. Meaningful when <see cref="HasTapeControls"/> is set.</summary>
    public bool TapePlayPressed { get; set; }

    /// <summary>RECORD button at capture. Meaningful when <see cref="HasTapeControls"/> is set.</summary>
    public bool TapeRecordPressed { get; set; }

    /// <summary>
    /// In-flight Datasette pulse and tape cursor. Not serialized. VICE
    /// saves the pending alarm and the tape image position.
    /// </summary>
    public bool HasTapeTiming { get; set; }

    public int TapePulseOffset { get; set; }

    public long TapeMotorRampCounter { get; set; }

    public bool TapeMotorRampComplete { get; set; }

    public bool TapeMotorRampStarted { get; set; }

    public int TapeCyclesUntilPulse { get; set; }

    public bool TapeHavePendingPulse { get; set; }

    /// <summary>
    /// Mounted TAP image at capture. Not serialized. VICE writes the tape
    /// image in the datasette snapshot module. False on older images, which
    /// leaves the current tape alone.
    /// </summary>
    public bool HasTapeImage { get; set; }

    /// <summary>TAP file bytes. Null when the capture had no tape.</summary>
    public byte[]? TapeImage { get; set; }

    /// <summary>
    /// Datasette connection at capture. Not serialized. False on older
    /// images, which leaves the current connection alone.
    /// </summary>
    public bool HasTapeAttached { get; set; }

    /// <summary>True when the Datasette was connected. Meaningful when <see cref="HasTapeAttached"/> is set.</summary>
    public bool TapeAttached { get; set; }

    /// <summary>
    /// VIC-II, SID, and CIA execution state for this image. Not serialized.
    /// Null on a deserialized image. Restore applies it after the visible IO
    /// writes so a running baseline keeps raster phase and OSC3.
    /// </summary>
    public C64ChipExecutionImage? ChipExecution { get; set; }

    /// <summary>
    /// Color RAM ($D800-$DBFF). Not part of the 64KB mem_ram image and not
    /// serialized. VICE stores it in the VIC-II snapshot module.
    /// </summary>
    public byte[]? ColorRam { get; set; }

    /// <summary>
    /// Clock interrupt latch for this image. Not serialized. VICE saves
    /// irq_clk and irq_delay_cycles beside the CPU.
    /// </summary>
    internal SystemClock.InterruptReplayState? InterruptTiming { get; set; }

    /// <summary>
    /// Keyboard matrix rows at capture. Not serialized. VICE stores keyarr
    /// in the keyboard snapshot. Index 8, when present, is the RESTORE line.
    /// </summary>
    public byte[]? KeyboardRows { get; set; }

    /// <summary>
    /// Host key names that were down at capture. Not serialized. The matrix
    /// row alone stores one bit per column, so two host keys that share a
    /// column (LeftShift and F2) collapse unless their names are restored.
    /// Null is a legacy image and restores one owner per down bit.
    /// </summary>
    public string[]? KeyboardHostKeys { get; set; }

    /// <summary>
    /// Matrix codes each held host key owned at capture. Not serialized.
    /// A raw release uses these codes. Looking the name up on the playback
    /// map assigns a different physical key.
    /// </summary>
    public bool HasKeyboardHostKeyCodes { get; set; }

    /// <summary>One entry per held host key. Codes come from the recording map.</summary>
    public (string Name, byte[] Codes)[]? KeyboardHostKeyCodes { get; set; }

    /// <summary>
    /// Cartridge bank registers at capture. Not serialized. VICE saves
    /// currbank beside the ROM image. The I/O window write is not that bank.
    /// </summary>
    internal bool HasCartridgeBanks { get; set; }

    /// <summary>Bank index and hide bits for each mapped cartridge type.</summary>
    internal C64MemoryMap.CartridgeBankState CartridgeBanks { get; set; }

    /// <summary>
    /// Cartridge ROM and mapping at capture. Not serialized. VICE ocean.c
    /// writes roml_banks beside currbank. The fixed RAM image does not.
    /// </summary>
    internal bool HasCartridgeImage { get; set; }

    /// <summary>Attached ROM bytes, or null when the port was empty.</summary>
    internal byte[]? CartridgeImage { get; set; }

    /// <summary>Mapping mode stored with <see cref="CartridgeImage"/>.</summary>
    internal int? CartridgeMapping { get; set; }

    /// <summary>
    /// Matrix press counts at capture. Not serialized. VICE stores keyarr,
    /// which is one bit per key. Vice# also counts each host name and each
    /// raw matrix press. False on older images, which rebuild counts from
    /// the host names alone.
    /// </summary>
    public bool HasKeyboardPressCounts { get; set; }

    /// <summary>One entry per down matrix key. The count is the owner total.</summary>
    public (byte KeyCode, int Count)[]? KeyboardPressCounts { get; set; }

    /// <summary>
    /// Control-port buttons at capture. Not serialized. Index 0 is port 1
    /// and index 1 is port 2. VICE stores joystick_value per port.
    /// </summary>
    public byte[]? JoystickPorts { get; set; }

    /// <summary>
    /// Port 1 mouse and POT state at capture. Not serialized.
    /// Byte 0 is the pot override, byte 1 is POTX ($D419), byte 2 is POTY
    /// ($D41A), and byte 3 is mouse mode. Bytes 4-7 and 8-11 are mouse X
    /// and Y as little-endian int32. VICE write_mouse_common_snapshot saves
    /// the latest mouse position. $D419/$D41A return these POT bytes while
    /// the override is on.
    /// </summary>
    public byte[]? MousePots { get; set; }

    public ulong Cycle => (ulong)State.Cycle;

    public void Serialize(Span<byte> destination)
    {
        if (destination.Length < GetSerializedSize())
            throw new ArgumentException("Destination is smaller than the serialized snapshot size.", nameof(destination));

        destination[0] = State.A;
        destination[1] = State.X;
        destination[2] = State.Y;
        destination[3] = State.S;
        destination[4] = State.P;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[5..7], State.PC);
        if (HasProcessorPort)
        {
            // Header byte 15 stays 0 on legacy images (full 64-bit cycle).
            // Port snapshots keep the same 16-byte header: a 48-bit cycle,
            // then direction, data, and a presence flag. 48 bits covers
            // years of 1 MHz execution. VICE stores pport beside mem_ram.
            if ((ulong)State.Cycle > 0x0000FFFFFFFFFFFFUL)
                throw new InvalidOperationException("Processor-port snapshots store the cycle in 48 bits.");

            var cycle = (ulong)State.Cycle;
            destination[7] = (byte)cycle;
            destination[8] = (byte)(cycle >> 8);
            destination[9] = (byte)(cycle >> 16);
            destination[10] = (byte)(cycle >> 24);
            destination[11] = (byte)(cycle >> 32);
            destination[12] = (byte)(cycle >> 40);
            destination[13] = PortDirection;
            destination[14] = PortData;
            destination[15] = ProcessorPortFlag;
        }
        else
        {
            BinaryPrimitives.WriteInt64LittleEndian(destination[7..15], State.Cycle);
            destination[15] = 0;
        }

        _memory.CopyTo(destination[HeaderSize..]);
    }

    public void Deserialize(ReadOnlySpan<byte> source)
    {
        if (source.Length < GetSerializedSize())
            throw new ArgumentException("Source is smaller than the serialized snapshot size.", nameof(source));

        long cycle;
        if (source[15] == ProcessorPortFlag)
        {
            cycle = source[7]
                | ((long)source[8] << 8)
                | ((long)source[9] << 16)
                | ((long)source[10] << 24)
                | ((long)source[11] << 32)
                | ((long)source[12] << 40);
            HasProcessorPort = true;
            PortDirection = source[13];
            PortData = source[14];
        }
        else
        {
            cycle = BinaryPrimitives.ReadInt64LittleEndian(source[7..15]);
            HasProcessorPort = false;
            PortDirection = 0;
            PortData = 0;
        }

        State = new MachineState
        {
            A = source[0],
            X = source[1],
            Y = source[2],
            S = source[3],
            P = source[4],
            PC = BinaryPrimitives.ReadUInt16LittleEndian(source[5..7]),
            Cycle = cycle
        };

        source[HeaderSize..(HeaderSize + _memory.Length)].CopyTo(_memory);
    }

    public int GetSerializedSize() => HeaderSize + _memory.Length;
}
