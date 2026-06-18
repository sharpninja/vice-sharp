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
    private byte[] _memoryImage = [];
    private string _searchQuery = string.Empty;
    private string _selectedSearchKind = "Hex";
    private string _searchStatus = string.Empty;
    private int _matchLineIndex = -1;

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

    /// <summary>Interpretations available for the memory search query.</summary>
    public IReadOnlyList<string> MemorySearchKinds { get; } =
        ["Hex", "Decimal", "ASCII", "PETSCII", "Screen codes"];

    /// <summary>The memory search query text (interpreted per <see cref="SelectedSearchKind"/>).</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    /// <summary>How <see cref="SearchQuery"/> is interpreted: Hex, Decimal, or a string encoding.</summary>
    public string SelectedSearchKind
    {
        get => _selectedSearchKind;
        set => SetProperty(ref _selectedSearchKind, value);
    }

    /// <summary>Result of the last memory search (found address / not found / parse error).</summary>
    public string SearchStatus
    {
        get => _searchStatus;
        private set => SetProperty(ref _searchStatus, value);
    }

    /// <summary>Index of the dump line containing the current match (-1 when none).</summary>
    public int MatchLineIndex
    {
        get => _matchLineIndex;
        set => SetProperty(ref _matchLineIndex, value);
    }

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
        private set
        {
            if (SetProperty(ref _isDebugVisible, value))
            {
                OnPropertyChanged(nameof(CanNavigatePrevious));
                OnPropertyChanged(nameof(CanNavigateNext));
            }
        }
    }

    /// <summary>Newest captured tick index (highest), or 0 when empty.</summary>
    private int MaxTickIndex => Ticks.Count == 0 ? 0 : Ticks[0].Index;

    /// <summary>Whether an older tick exists to navigate to (Back / First).</summary>
    public bool CanNavigatePrevious => _isDebugVisible && _selectedTick is { } tick && tick.Index > 0;

    /// <summary>Whether a newer tick exists to navigate to (Next / Last).</summary>
    public bool CanNavigateNext => _isDebugVisible && _selectedTick is { } tick && tick.Index < MaxTickIndex;

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
        set
        {
            if (SetProperty(ref _selectedTick, value))
            {
                OnPropertyChanged(nameof(CanNavigatePrevious));
                OnPropertyChanged(nameof(CanNavigateNext));
            }
        }
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
        SearchStatus = string.Empty;
        MatchLineIndex = -1;
        if (!response.Status.IsSuccess)
        {
            _memoryImage = [];
            StatusText = response.Status.Message;
            IsDebugVisible = true;
            return;
        }

        _memoryImage = response.Data;
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

    /// <summary>Inspect the oldest captured tick.</summary>
    public Task NavigateFirstAsync(CancellationToken cancellationToken = default)
        => NavigateToIndexAsync(0, cancellationToken);

    /// <summary>Inspect the newest captured tick.</summary>
    public Task NavigateLastAsync(CancellationToken cancellationToken = default)
        => NavigateToIndexAsync(MaxTickIndex, cancellationToken);

    /// <summary>Inspect the previous (older) tick.</summary>
    public Task NavigatePreviousAsync(CancellationToken cancellationToken = default)
        => NavigateToIndexAsync((_selectedTick?.Index ?? 0) - 1, cancellationToken);

    /// <summary>Inspect the next (newer) tick.</summary>
    public Task NavigateNextAsync(CancellationToken cancellationToken = default)
        => NavigateToIndexAsync((_selectedTick?.Index ?? 0) + 1, cancellationToken);

    private async Task NavigateToIndexAsync(int index, CancellationToken cancellationToken)
    {
        if (Ticks.Count == 0)
            return;

        var clamped = Math.Clamp(index, 0, MaxTickIndex);
        TickRowViewModel? target = null;
        foreach (var tick in Ticks)
        {
            if (tick.Index == clamped)
            {
                target = tick;
                break;
            }
        }

        if (target is not null)
            await InspectAsync(target, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Search the reconstructed memory for <see cref="SearchQuery"/> interpreted per
    /// <see cref="SelectedSearchKind"/>; sets <see cref="MatchLineIndex"/> to the dump line of
    /// the first match (and reports the address), or -1 when not found / unparseable.</summary>
    public void SearchMemory()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchStatus = "Enter a value to search for.";
            MatchLineIndex = -1;
            return;
        }

        if (!MemorySearch.TryParsePattern(SearchQuery, SelectedSearchKind, out var pattern))
        {
            SearchStatus = $"Could not parse the query as {SelectedSearchKind}.";
            MatchLineIndex = -1;
            return;
        }

        var offset = MemorySearch.IndexOf(_memoryImage, pattern);
        if (offset < 0)
        {
            SearchStatus = $"Not found ({pattern.Length} byte(s)).";
            MatchLineIndex = -1;
            return;
        }

        MatchLineIndex = offset / 16;
        SearchStatus = $"Found {pattern.Length} byte(s) at ${offset:X4}.";
    }

    /// <summary>Return from the debug screen to the tick list.</summary>
    public void CloseDebug()
    {
        IsDebugVisible = false;
        MemoryDump.Clear();
        ChipStates.Clear();
        _memoryImage = [];
        SearchStatus = string.Empty;
        MatchLineIndex = -1;
    }
}

/// <summary>
/// Parses a memory search query (hex bytes, decimal bytes, or a string in ASCII / PETSCII /
/// C64 screen codes) into a byte pattern and locates it in a memory image.
/// </summary>
public static class MemorySearch
{
    public static bool TryParsePattern(string query, string kind, out byte[] pattern)
    {
        pattern = [];
        var q = query?.Trim() ?? string.Empty;
        if (q.Length == 0)
            return false;

        switch (kind)
        {
            case "Hex": return TryParseHex(q, out pattern);
            case "Decimal": return TryParseDecimal(q, out pattern);
            case "ASCII": pattern = Encode(q, AsciiByte); return true;
            case "PETSCII": pattern = Encode(q, PetsciiByte); return true;
            case "Screen codes": pattern = Encode(q, ScreenCodeByte); return true;
            default: return false;
        }
    }

    public static int IndexOf(byte[] image, byte[] pattern)
    {
        if (image is null || pattern is null || pattern.Length == 0 || pattern.Length > image.Length)
            return -1;

        for (var i = 0; i <= image.Length - pattern.Length; i++)
        {
            var j = 0;
            while (j < pattern.Length && image[i + j] == pattern[j])
                j++;
            if (j == pattern.Length)
                return i;
        }

        return -1;
    }

    private static bool TryParseHex(string q, out byte[] pattern)
    {
        pattern = [];
        var hex = q.Replace(" ", string.Empty).Replace("$", string.Empty)
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (hex.Length == 0 || hex.Length % 2 != 0)
            return false;

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out bytes[i]))
                return false;
        }

        pattern = bytes;
        return true;
    }

    private static bool TryParseDecimal(string q, out byte[] pattern)
    {
        pattern = [];
        var parts = q.Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        var bytes = new byte[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var v) || v is < 0 or > 255)
                return false;
            bytes[i] = (byte)v;
        }

        pattern = bytes;
        return true;
    }

    private static byte[] Encode(string s, Func<char, byte> map)
    {
        var bytes = new byte[s.Length];
        for (var i = 0; i < s.Length; i++)
            bytes[i] = map(s[i]);
        return bytes;
    }

    private static byte AsciiByte(char c) => (byte)(c & 0xFF);

    // C64 default (uppercase) charset: A-Z and a-z both map to PETSCII $41-$5A.
    private static byte PetsciiByte(char c)
    {
        if (c is >= 'a' and <= 'z')
            return (byte)(c - 'a' + 0x41);
        if (c is >= 'A' and <= 'Z')
            return (byte)(c - 'A' + 0x41);
        return (byte)(c & 0xFF);
    }

    // Screen codes: @ = 0, A-Z = 1..26, space = $20, digits keep their ASCII codes.
    private static byte ScreenCodeByte(char c)
    {
        if (c is >= 'a' and <= 'z')
            return (byte)(c - 'a' + 1);
        if (c is >= 'A' and <= 'Z')
            return (byte)(c - 'A' + 1);
        if (c == '@')
            return 0;
        return (byte)(c & 0xFF);
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
