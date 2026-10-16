using ViceSharp.Abstractions;

namespace ViceSharp.Host.Runtime;

public sealed class HostKeyboardAutomation
{
    private const int DefaultReadyPromptStart = 0x0400;
    private const int KernalScreenHibase = 0x0288;
    private const int ReadyPromptLength = 1000;
    private const int KernalKeyboardBufferStart = 0x0277;
    private const int KernalKeyboardBufferCount = 0x00C6;
    private const int KernalKeyboardBufferSize = 10;
    // C64 zero-page "cursor blink enable" flag ($CC): 0 while the screen editor is
    // idle in the BASIC input loop flashing the cursor (i.e. ready for a keystroke),
    // and non-zero during boot, LOAD, PRINT, and program execution when the cursor is
    // suppressed. Autostart gates its LOAD/RUN keystrokes on this so keys are only
    // injected once the prompt is truly ready (READY shown AND the cursor blinking).
    private const int CursorBlinkEnableFlag = 0x00CC;
    private const int MaxReadyWaitFrames = 600;
    private const int InitialReadyDelayFrames = 12;

    private static readonly byte[] Drive8AutostartSequence =
    [
        (byte)'L', (byte)'O', (byte)'A', (byte)'D', (byte)'"', (byte)'*', (byte)'"', (byte)',', (byte)'8', (byte)',', (byte)'1', 13,
        (byte)'R', (byte)'U', (byte)'N', 13
    ];
    private static readonly byte[] RunSequence = [(byte)'R', (byte)'U', (byte)'N', 13];

    private readonly IReadOnlyList<byte> _keySequence;
    private readonly bool _waitForBasicReady;
    private readonly Func<IMachine, string?>? _readyAction;
    private AutomationPhase _phase;
    private int _keyIndex;
    private int _frameDelay;
    private int _readyWaitFrames;
    private bool _readyActionApplied;

    private HostKeyboardAutomation(
        string description,
        IReadOnlyList<byte> keySequence,
        bool waitForBasicReady,
        Func<IMachine, string?>? readyAction = null)
    {
        Description = description;
        _keySequence = keySequence;
        _waitForBasicReady = waitForBasicReady;
        _readyAction = readyAction;
        _phase = waitForBasicReady ? AutomationPhase.WaitingForReady : AutomationPhase.FeedingKeyboardBuffer;
    }

    public string Description { get; }

    public bool IsActive => _phase is AutomationPhase.WaitingForReady or AutomationPhase.FeedingKeyboardBuffer;

    public string? LastError { get; private set; }

    public static HostKeyboardAutomation CreateC64Drive8Autostart()
        => new("C64 BASIC drive 8 autostart", Drive8AutostartSequence, waitForBasicReady: true);

    public static HostKeyboardAutomation CreateC64Drive8Autostart(Func<IMachine, string?> readyAction)
        => new("C64 BASIC drive 8 autostart", RunSequence, waitForBasicReady: true, readyAction);

    /// <summary>Type RUN after the BASIC READY prompt is idle.</summary>
    public static HostKeyboardAutomation CreateBasicRun()
        => new("BASIC RUN", RunSequence, waitForBasicReady: true);

    public void AdvanceFrame(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        if (!IsActive)
            return;

        if (_phase == AutomationPhase.WaitingForReady)
        {
            if (!TryFindBasicReadyPrompt(machine, out var error))
            {
                Fail(error);
                return;
            }

            // Proceed only once the BASIC prompt is genuinely ready for input: the
            // "READY." text is on screen AND the editor is flashing the cursor in its
            // input loop. Checking the text alone fired RUN too early (while a prior
            // READY lingered on screen or mid-LOAD), so the keystrokes were dropped and
            // the program ran with a malformed line ("?SYNTAX ERROR" autostart race).
            if (!ContainsBasicReadyPrompt(machine) || !IsCursorBlinking(machine))
            {
                _readyWaitFrames++;
                if (_readyWaitFrames > MaxReadyWaitFrames)
                    Fail("BASIC READY prompt was not observed before the autostart timeout.");

                return;
            }

            if (_readyAction is not null && !_readyActionApplied)
            {
                var actionError = _readyAction(machine);
                if (!string.IsNullOrWhiteSpace(actionError))
                {
                    Fail(actionError);
                    return;
                }

                _readyActionApplied = true;
            }

            _phase = AutomationPhase.FeedingKeyboardBuffer;
            _frameDelay = InitialReadyDelayFrames;
            return;
        }

        if (_frameDelay > 0)
        {
            _frameDelay--;
            return;
        }

        if (_phase == AutomationPhase.FeedingKeyboardBuffer)
        {
            if (_keyIndex >= _keySequence.Count)
            {
                _phase = AutomationPhase.Complete;
                return;
            }

            if (!TryFeedKernalKeyboardBuffer(machine, out var error))
            {
                Fail(error);
                return;
            }
        }
    }

    private bool TryFeedKernalKeyboardBuffer(IMachine machine, out string error)
    {
        error = string.Empty;
        try
        {
            var pending = machine.Bus.Peek(KernalKeyboardBufferCount);
            if (pending != 0)
                return true;

            var count = Math.Min(KernalKeyboardBufferSize, _keySequence.Count - _keyIndex);
            for (var i = 0; i < count; i++)
                machine.Bus.Write((ushort)(KernalKeyboardBufferStart + i), _keySequence[_keyIndex + i]);

            machine.Bus.Write(KernalKeyboardBufferCount, (byte)count);
            _keyIndex += count;
            if (_keyIndex >= _keySequence.Count)
                _phase = AutomationPhase.Complete;

            return true;
        }
        catch (NotSupportedException ex)
        {
            error = $"The current machine bus cannot feed the KERNAL keyboard buffer: {ex.Message}";
            return false;
        }
    }

    private static bool TryFindBasicReadyPrompt(IMachine machine, out string error)
    {
        try
        {
            machine.Bus.Peek((ushort)GetReadyPromptStart(machine));
            error = string.Empty;
            return true;
        }
        catch (NotSupportedException ex)
        {
            error = $"The current machine bus cannot be scanned for BASIC READY: {ex.Message}";
            return false;
        }
    }

    private static bool ContainsBasicReadyPrompt(IMachine machine)
    {
        var start = GetReadyPromptStart(machine);
        Span<byte> screenCodes = stackalloc byte[ReadyPromptLength];
        for (var i = 0; i < screenCodes.Length; i++)
            screenCodes[i] = machine.Bus.Peek((ushort)(start + i));

        ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
        ReadOnlySpan<byte> asciiReady = "READY"u8;
        return screenCodes.IndexOf(screenCodeReady) >= 0 || screenCodes.IndexOf(asciiReady) >= 0;
    }

    // KERNAL HIBASE ($0288) is the current screen page: $04 on C64, $1E on
    // unexpanded VIC-20, $10 on VIC-20 with 8K+. Zero means the test/minimal
    // bus never initialized it; keep the C64 $0400 default so existing
    // autostart fixtures still see READY.
    private static int GetReadyPromptStart(IMachine machine)
    {
        var page = machine.Bus.Peek(KernalScreenHibase);
        return page == 0 ? DefaultReadyPromptStart : page << 8;
    }

    // True when the C64 screen editor is flashing the cursor in its BASIC input loop
    // (zero-page $CC == 0), i.e. the prompt is idle and ready to accept a keystroke.
    private static bool IsCursorBlinking(IMachine machine)
        => machine.Bus.Peek(CursorBlinkEnableFlag) == 0;

    private void Fail(string message)
    {
        LastError = string.IsNullOrWhiteSpace(message)
            ? "Host keyboard automation failed."
            : message;
        _phase = AutomationPhase.Faulted;
    }

    private enum AutomationPhase
    {
        WaitingForReady,
        FeedingKeyboardBuffer,
        Complete,
        Faulted
    }
}
