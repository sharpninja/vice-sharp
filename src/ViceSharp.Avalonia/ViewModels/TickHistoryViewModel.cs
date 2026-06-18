using System.Collections.ObjectModel;
using System.Text;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// FR-TICKHIST-001: the "last 100 ticks" history panel. Lists the most recently executed
/// CPU instructions (newest first). When the emulator is PAUSED, selecting a tick opens a
/// debug screen showing that tick's CPU registers and a scrolling memory dump reconstructed
/// as it was at that tick (current paused RAM with later ticks' write-deltas undone).
/// </summary>
public sealed class TickHistoryViewModel : ObservableObject
{
    private readonly IHostProtocolClient _host;
    private bool _isPaused;
    private bool _isDebugVisible;
    private string _statusText = "Pause the emulator, then refresh to inspect recent ticks.";
    private string _registersText = string.Empty;
    private string _debugTitle = string.Empty;
    private TickRowViewModel? _selectedTick;

    public TickHistoryViewModel(IHostProtocolClient host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _host = host;
    }

    /// <summary>Captured ticks, newest first.</summary>
    public ObservableCollection<TickRowViewModel> Ticks { get; } = new();

    /// <summary>Reconstructed memory dump lines for the selected tick.</summary>
    public ObservableCollection<string> MemoryDump { get; } = new();

    /// <summary>Each chip's decoded state as captured at the selected tick.</summary>
    public ObservableCollection<ChipStateGroupViewModel> ChipStates { get; } = new();

    /// <summary>Whether the emulator is paused (only then can a tick be inspected).</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (SetProperty(ref _isPaused, value))
                OnPropertyChanged(nameof(CanInspect));
        }
    }

    public bool CanInspect => _isPaused;

    public bool IsDebugVisible
    {
        get => _isDebugVisible;
        private set => SetProperty(ref _isDebugVisible, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string RegistersText
    {
        get => _registersText;
        private set => SetProperty(ref _registersText, value);
    }

    public string DebugTitle
    {
        get => _debugTitle;
        private set => SetProperty(ref _debugTitle, value);
    }

    public TickRowViewModel? SelectedTick
    {
        get => _selectedTick;
        set => SetProperty(ref _selectedTick, value);
    }

    /// <summary>Reload the tick list from the host (newest first).</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var response = await _host.GetTickHistoryAsync(cancellationToken).ConfigureAwait(true);
        Ticks.Clear();
        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            return;
        }

        for (var i = response.Ticks.Count - 1; i >= 0; i--)
            Ticks.Add(TickRowViewModel.From(response.Ticks[i]));

        StatusText = Ticks.Count == 0
            ? "No ticks captured yet - run the emulator, then pause."
            : $"{Ticks.Count} ticks. {(IsPaused ? "Select a tick to inspect." : "Pause to inspect a tick.")}";
    }

    /// <summary>Open the debug screen for a tick: its registers + reconstructed memory dump.
    /// No-op (with a hint) unless the emulator is paused.</summary>
    public async Task InspectAsync(TickRowViewModel? tick, CancellationToken cancellationToken = default)
    {
        if (tick is null)
            return;

        SelectedTick = tick;
        if (!IsPaused)
        {
            StatusText = "Pause the emulator to inspect a tick.";
            return;
        }

        RegistersText = tick.RegistersText;
        DebugTitle = $"Tick {tick.Index} - instruction ${tick.InstructionAddress:X4} (${tick.Opcode:X2})";

        var response = await _host.ReadMemoryAtTickAsync(tick.Index, 0x0000, 0x10000, cancellationToken).ConfigureAwait(true);
        MemoryDump.Clear();
        if (!response.Status.IsSuccess)
        {
            StatusText = response.Status.Message;
            IsDebugVisible = true;
            return;
        }

        foreach (var line in HexDump.Format(response.Data, baseAddress: 0))
            MemoryDump.Add(line);

        var chipResponse = await _host.GetChipStateAtTickAsync(tick.Index, cancellationToken).ConfigureAwait(true);
        ChipStates.Clear();
        if (chipResponse.Status.IsSuccess)
        {
            foreach (var chip in chipResponse.Chips)
                ChipStates.Add(ChipStateGroupViewModel.From(chip));
        }

        IsDebugVisible = true;
        StatusText = $"Showing tick {tick.Index} (full system state as it was then).";
    }

    /// <summary>Return from the debug screen to the tick list.</summary>
    public void CloseDebug()
    {
        IsDebugVisible = false;
        MemoryDump.Clear();
        ChipStates.Clear();
    }
}

/// <summary>One row in the tick-history list (a captured CPU instruction + its registers).</summary>
public sealed record TickRowViewModel(
    int Index,
    int InstructionAddress,
    int Opcode,
    int A,
    int X,
    int Y,
    int S,
    int P,
    int Pc,
    int WriteCount)
{
    public static TickRowViewModel From(TickHistoryEntryDto dto)
        => new(dto.Index, dto.InstructionAddress, dto.Opcode, dto.A, dto.X, dto.Y, dto.S, dto.P, dto.Pc, dto.WriteCount);

    /// <summary>Compact one-line summary shown in the list.</summary>
    public string Display
        => $"${InstructionAddress:X4}  op ${Opcode:X2}   A:{A:X2} X:{X:X2} Y:{Y:X2} SP:{S:X2} P:{P:X2}   {(WriteCount > 0 ? $"{WriteCount}w" : string.Empty)}";

    /// <summary>Multi-line register view shown in the debug screen.</summary>
    public string RegistersText
        => $"PC ${Pc:X4}    A ${A:X2}    X ${X:X2}    Y ${Y:X2}    SP ${S:X2}    P ${P:X2}  [{Flags}]\n"
         + $"Instruction ${InstructionAddress:X4}: opcode ${Opcode:X2}    memory writes this tick: {WriteCount}";

    private string Flags
        => $"{Flag(0x80, 'N')}{Flag(0x40, 'V')}-{Flag(0x10, 'B')}{Flag(0x08, 'D')}{Flag(0x04, 'I')}{Flag(0x02, 'Z')}{Flag(0x01, 'C')}";

    private char Flag(int mask, char on) => (P & mask) != 0 ? on : '.';
}

/// <summary>One chip's decoded state for the debug screen (a header + a flow of fields).</summary>
public sealed record ChipStateGroupViewModel(string ChipName, string FieldsText)
{
    public static ChipStateGroupViewModel From(ChipStateDto dto)
    {
        var sb = new StringBuilder();
        foreach (var field in dto.Fields)
        {
            var hex = field.Width >= 2 ? field.Value.ToString("X4") : field.Value.ToString("X2");
            sb.Append(field.Name).Append(" $").Append(hex).Append("    ");
        }

        return new ChipStateGroupViewModel(dto.ChipName, sb.ToString().TrimEnd());
    }
}

/// <summary>Formats a byte buffer as classic "address  hex bytes  ascii" dump lines.</summary>
public static class HexDump
{
    public static IEnumerable<string> Format(byte[] data, int baseAddress, int bytesPerLine = 16)
    {
        ArgumentNullException.ThrowIfNull(data);
        var sb = new StringBuilder(80);
        for (var offset = 0; offset < data.Length; offset += bytesPerLine)
        {
            sb.Clear();
            sb.Append(((baseAddress + offset) & 0xFFFF).ToString("X4")).Append("  ");

            var count = Math.Min(bytesPerLine, data.Length - offset);
            for (var i = 0; i < bytesPerLine; i++)
            {
                if (i < count)
                    sb.Append(data[offset + i].ToString("X2")).Append(' ');
                else
                    sb.Append("   ");
            }

            sb.Append(' ');
            for (var i = 0; i < count; i++)
            {
                var b = data[offset + i];
                sb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            }

            yield return sb.ToString();
        }
    }
}
