namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Input;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-INPUT-KEYBOARD (BACKFILL-INPUT-001).
/// Use case: Real C64 keyboard scanning drives CIA1 PB ($DC01) with an
/// active-low column mask, then reads CIA1 PA ($DC00) to observe which
/// row lines are pulled low by pressed keys at the scanned column. This
/// fixture wires a focused CIA1 + C64KeyboardMatrix pair (mirroring the
/// production wiring in C64MemoryMap) and exercises the scan path end
/// to end without standing up a full machine.
/// Acceptance: No-key baseline reads PA = 0xFF; pressing a single key
/// pulls exactly its row bit low at its column and leaves PA all-high at
/// every other column; releasing restores the baseline; two keys in the
/// same scanned column pull both row bits low simultaneously.
/// </summary>
public sealed class Cia1KeyboardScanTests
{
    // Standard C64 keycode layout: keycode = (row << 3) | col, matching
    // C64KeyboardMatrix.SetKey decoding. For the canonical "A" keycode
    // 0x0A this yields row = 1, col = 2; so scanning column 2 (PB =
    // ~(1 << 2) = 0xFB) pulls PA bit 1 low (PA = 0xFD).
    private const byte KeyCodeA = 0x0A; // row 1, col 2
    private const byte KeyCodeD = 0x12; // row 2, col 2 (same column as A)
    private const byte ColumnAScanMask = 0xFB; // ~(1 << 2)
    private const byte ColumnZeroScanMask = 0xFE; // ~(1 << 0)

    private static (Mos6526 cia, C64KeyboardMatrix keyboard) BuildCia1WithKeyboard()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);
        var keyboard = new C64KeyboardMatrix();
        keyboard.Initialize();
        cia.Reset();

        // Mirror the production wiring in C64MemoryMap: PB writes update
        // the keyboard column mask, PA reads return the row state.
        cia.PortAInput = keyboard.ReadRowState;
        cia.PortBOutputChanged = value => keyboard.SetColumnMask(value);

        return (cia, keyboard);
    }

    /// <summary>
    /// FR/TR: FR-INPUT-KEYBOARD (BACKFILL-INPUT-001).
    /// Use case: With no key pressed, scanning any column on CIA1 PB
    /// must leave every row line in CIA1 PA pulled high (open keyboard
    /// matrix reads 0xFF).
    /// Acceptance: After driving PB = 0xFE (column 0 active low) with
    /// the matrix idle, PA reads 0xFF.
    /// </summary>
    [Fact]
    public void NoKeyPressed_PaReadsAllHigh()
    {
        var (cia, _) = BuildCia1WithKeyboard();

        cia.Write(0xDC01, ColumnZeroScanMask);

        cia.Read(0xDC00).Should().Be(0xFF,
            "open matrix scanned at any column leaves all PA rows high");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-KEYBOARD (BACKFILL-INPUT-001).
    /// Use case: Pressing 'A' (keycode 0x0A; row 1, col 2) and scanning
    /// column 2 (PB = 0xFB) must pull row 1 of PA low while leaving
    /// every other row high (PA = 0xFD).
    /// Acceptance: With 'A' pressed and PB = 0xFB, PA reads 0xFD.
    /// </summary>
    [Fact]
    public void PressA_ScanColumnTwo_PullsRowOneLow()
    {
        var (cia, keyboard) = BuildCia1WithKeyboard();

        keyboard.SetKey(KeyCodeA, pressed: true);
        cia.Write(0xDC01, ColumnAScanMask);

        cia.Read(0xDC00).Should().Be(0xFD,
            "scanning the column of pressed 'A' pulls PA bit 1 (row 1) low");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-KEYBOARD (BACKFILL-INPUT-001).
    /// Use case: Pressing 'A' (col 2) and then scanning a different
    /// column (column 0; PB = 0xFE) must not pull any PA row low. The
    /// keyboard matrix only couples row and column lines that are both
    /// active in the scan.
    /// Acceptance: With 'A' pressed and PB = 0xFE, PA reads 0xFF.
    /// </summary>
    [Fact]
    public void PressA_ScanUnrelatedColumn_PaStaysHigh()
    {
        var (cia, keyboard) = BuildCia1WithKeyboard();

        keyboard.SetKey(KeyCodeA, pressed: true);
        cia.Write(0xDC01, ColumnZeroScanMask);

        cia.Read(0xDC00).Should().Be(0xFF,
            "key only pulls a row low when its own column is the one being scanned");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-KEYBOARD (BACKFILL-INPUT-001).
    /// Use case: Releasing a previously pressed key must restore the PA
    /// scan reading to the open-matrix baseline (0xFF) when its column
    /// is scanned.
    /// Acceptance: After press-then-release of 'A' with PB = 0xFB, PA
    /// reads 0xFF.
    /// </summary>
    [Fact]
    public void PressThenReleaseA_PaReturnsToAllHigh()
    {
        var (cia, keyboard) = BuildCia1WithKeyboard();

        keyboard.SetKey(KeyCodeA, pressed: true);
        keyboard.SetKey(KeyCodeA, pressed: false);
        cia.Write(0xDC01, ColumnAScanMask);

        cia.Read(0xDC00).Should().Be(0xFF,
            "releasing the only pressed key restores the open-matrix baseline");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-KEYBOARD (BACKFILL-INPUT-001).
    /// Use case: Two keys sharing a column (here 'A' at row 1 col 2, and
    /// 'D' at row 2 col 2) must pull both of their row bits low when
    /// that column is scanned. This is the classic n-key-rollover case
    /// the real keyboard matrix supports without ghosting.
    /// Acceptance: With 'A' and 'D' pressed and PB = 0xFB, PA reads
    /// 0xF9 (bits 1 and 2 both clear).
    /// </summary>
    [Fact]
    public void TwoKeysSameColumn_BothRowsPulledLow()
    {
        var (cia, keyboard) = BuildCia1WithKeyboard();

        keyboard.SetKey(KeyCodeA, pressed: true);
        keyboard.SetKey(KeyCodeD, pressed: true);
        cia.Write(0xDC01, ColumnAScanMask);

        cia.Read(0xDC00).Should().Be(0xF9,
            "two keys in the scanned column pull both of their row bits low");
    }
}
