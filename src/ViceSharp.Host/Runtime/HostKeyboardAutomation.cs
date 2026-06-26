using ViceSharp.Abstractions;

namespace ViceSharp.Host.Runtime;

public sealed class HostKeyboardAutomation
{
    private const int ReadyPromptStart = 0x0400;
    private const int ReadyPromptLength = 1000;
    // C64 zero-page "cursor blink enable" flag ($CC): 0 while the screen editor is
    // idle in the BASIC input loop flashing the cursor (i.e. ready for a keystroke),
    // and non-zero during boot, LOAD, PRINT, and program execution when the cursor is
    // suppressed. Autostart gates its LOAD/RUN keystrokes on this so keys are only
    // injected once the prompt is truly ready (READY shown AND the cursor blinking).
    private const int CursorBlinkEnableFlag = 0x00CC;
    private const int MaxReadyWaitFrames = 600;
    private const int InitialReadyDelayFrames = 12;
    private const int KeyPressFrames = 3;
    private const int KeyReleaseFrames = 3;

    private static readonly string[] Drive8AutostartSequence =
    [
        "L", "O", "A", "D", "\"", "*", "\"", ",", "8", ",", "1", "Return",
        "R", "U", "N", "Return"
    ];
    private static readonly string[] RunSequence = ["R", "U", "N", "Return"];

    private readonly IReadOnlyList<string> _keySequence;
    private readonly bool _waitForBasicReady;
    private readonly Func<IMachine, string?>? _readyAction;
    private AutomationPhase _phase;
    private int _keyIndex;
    private int _frameDelay;
    private int _readyWaitFrames;
    private string? _pressedKey;
    private bool _readyActionApplied;

    private HostKeyboardAutomation(
        string description,
        IReadOnlyList<string> keySequence,
        bool waitForBasicReady,
        Func<IMachine, string?>? readyAction = null)
    {
        Description = description;
        _keySequence = keySequence;
        _waitForBasicReady = waitForBasicReady;
        _readyAction = readyAction;
        _phase = waitForBasicReady ? AutomationPhase.WaitingForReady : AutomationPhase.Pressing;
    }

    public string Description { get; }

    public bool IsActive => _phase is AutomationPhase.WaitingForReady or AutomationPhase.Pressing or AutomationPhase.Releasing;

    public string? LastError { get; private set; }

    public static HostKeyboardAutomation CreateC64Drive8Autostart()
        => new("C64 BASIC drive 8 autostart", Drive8AutostartSequence, waitForBasicReady: true);

    public static HostKeyboardAutomation CreateC64Drive8Autostart(Func<IMachine, string?> readyAction)
        => new("C64 BASIC drive 8 autostart", RunSequence, waitForBasicReady: true, readyAction);

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

            _phase = AutomationPhase.Pressing;
            _frameDelay = InitialReadyDelayFrames;
            return;
        }

        if (_frameDelay > 0)
        {
            _frameDelay--;
            return;
        }

        var keyboard = machine.Devices.All.OfType<IMachineKeyboardInput>().FirstOrDefault();
        if (keyboard is null)
        {
            Fail("The current machine does not expose runtime keyboard input.");
            return;
        }

        if (_phase == AutomationPhase.Pressing)
        {
            if (_keyIndex >= _keySequence.Count)
            {
                _phase = AutomationPhase.Complete;
                return;
            }

            var key = _keySequence[_keyIndex];
            if (!keyboard.SetKeyState(key, pressed: true))
            {
                Fail($"The selected keyboard map could not resolve '{key}'.");
                return;
            }

            _pressedKey = key;
            _phase = AutomationPhase.Releasing;
            _frameDelay = KeyPressFrames;
            return;
        }

        if (_phase == AutomationPhase.Releasing)
        {
            if (_pressedKey is not null)
                keyboard.SetKeyState(_pressedKey, pressed: false);

            _pressedKey = null;
            _keyIndex++;
            _phase = _keyIndex >= _keySequence.Count ? AutomationPhase.Complete : AutomationPhase.Pressing;
            _frameDelay = KeyReleaseFrames;
        }
    }

    private static bool TryFindBasicReadyPrompt(IMachine machine, out string error)
    {
        try
        {
            machine.Bus.Peek(ReadyPromptStart);
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
        Span<byte> screenCodes = stackalloc byte[ReadyPromptLength];
        for (var i = 0; i < screenCodes.Length; i++)
            screenCodes[i] = machine.Bus.Peek((ushort)(ReadyPromptStart + i));

        ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
        ReadOnlySpan<byte> asciiReady = "READY"u8;
        return screenCodes.IndexOf(screenCodeReady) >= 0 || screenCodes.IndexOf(asciiReady) >= 0;
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
        Pressing,
        Releasing,
        Complete,
        Faulted
    }
}
