namespace ViceSharp.Protocol;

/// <summary>
/// PLAN-XBOXUWP slice S15 (IMPL-XBOXUWP-015). FR-XSET-001 / TR-XSET-002:
/// the single, portable source of truth for the emulator settings option
/// lists and their display-label &lt;-&gt; stored-id conversions.
/// <para>
/// The desktop attach panel (<c>ViceSharp.Avalonia.ViewModels.AttachPanelViewModel</c>)
/// and the Xbox 10-foot settings UI (<c>ViceSharp.Xbox.ViewModels</c>) both bind
/// to these lists and apply through the host settings pipeline, so the two
/// surfaces MUST agree byte-for-byte on labels, order, and ids. Hosting this
/// definition in <c>ViceSharp.Protocol</c> (which both consumers already
/// reference) prevents drift.
/// </para>
/// <para>
/// The catalog is AOT/trim-safe and reflection-free: the option lists are
/// static collection-expression arrays and every conversion is a pure
/// switch expression. It takes no dependency on <c>Windows.*</c> or any host
/// type.
/// </para>
/// </summary>
public static class SettingsOptionCatalog
{
    /// <summary>Renderer options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> RendererModes { get; } = ["Host direct", "Software"];

    /// <summary>Display-scale options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> DisplayScales { get; } = ["1x", "2x", "3x", "Fit window"];

    /// <summary>Crop-mode options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> CropModes { get; } = ["Full frame", "Visible area", "Borderless"];

    /// <summary>Aspect-mode options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> AspectModes { get; } = ["Square pixels", "VICE pixel aspect", "Force 4:3"];

    /// <summary>Palette options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> PaletteModes { get; } = ["VICE default", "Pepto", "Monochrome green", "Amber"];

    /// <summary>Audio-mode options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> AudioModes { get; } = ["Enabled", "Muted", "Unavailable"];

    /// <summary>Input-mode options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> InputModes { get; } = ["Keyboard + joystick", "Keyboard only", "Disabled"];

    /// <summary>Primary-joystick-port options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> PrimaryJoystickPorts { get; } = ["Joystick 2", "Joystick 1"];

    /// <summary>Resource-mode options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> ResourceModes { get; } = ["Auto detect", "Use configured paths", "Missing resources"];

    /// <summary>Frame-pacing-strategy options (display labels, desktop order).</summary>
    public static IReadOnlyList<string> PacingStrategies { get; } = ["Semaphore", "VICE"];

    /// <summary>Maps a renderer display label to its stored id.</summary>
    public static string ToRendererId(string renderer) => renderer switch
    {
        "Software" => "software",
        _ => "host"
    };

    /// <summary>Maps a stored renderer id back to its display label.</summary>
    public static string FromRendererId(string renderer) => renderer switch
    {
        "software" => "Software",
        _ => "Host direct"
    };

    /// <summary>Maps a display-scale label to its stored id.</summary>
    public static string ToScaleId(string scale) => scale switch
    {
        "Fit window" => "fit-window",
        _ => scale
    };

    /// <summary>Maps a stored display-scale id back to its display label (empty id defaults to "2x").</summary>
    public static string FromScaleId(string scale) => scale switch
    {
        "fit-window" => "Fit window",
        "" => "2x",
        _ => scale
    };

    /// <summary>Maps a crop-mode display label to its stored id.</summary>
    public static string ToCropModeId(string cropMode) => cropMode switch
    {
        "Full frame" => "full-frame",
        "Borderless" => "borderless",
        _ => "visible-area"
    };

    /// <summary>
    /// Maps a stored crop-mode id back to its display label. An unrecognised id
    /// falls back using the current border state (<paramref name="showBorder"/>),
    /// matching the desktop table.
    /// </summary>
    public static string FromCropModeId(string cropMode, bool showBorder) => cropMode switch
    {
        "full-frame" => "Full frame",
        "borderless" => "Borderless",
        "visible-area" => "Visible area",
        _ => showBorder ? "Visible area" : "Borderless"
    };

    /// <summary>Maps an aspect-mode display label to its stored id.</summary>
    public static string ToAspectModeId(string aspectMode) => aspectMode switch
    {
        "Square pixels" => "square-pixels",
        "Force 4:3" => "force-4-3",
        _ => "vice-pixel-aspect"
    };

    /// <summary>
    /// Maps a stored aspect-mode id back to its display label. An unrecognised id
    /// falls back using the current aspect-ratio state
    /// (<paramref name="maintainAspectRatio"/>), matching the desktop table.
    /// </summary>
    public static string FromAspectModeId(string aspectMode, bool maintainAspectRatio) => aspectMode switch
    {
        "square-pixels" => "Square pixels",
        "force-4-3" => "Force 4:3",
        "vice-pixel-aspect" => "VICE pixel aspect",
        _ => maintainAspectRatio ? "VICE pixel aspect" : "Square pixels"
    };

    /// <summary>Maps a palette display label to its stored id.</summary>
    public static string ToPaletteId(string palette) => palette switch
    {
        "Pepto" => "pepto",
        "Monochrome green" => "monochrome-green",
        "Amber" => "amber",
        _ => "vice"
    };

    /// <summary>Maps a stored palette id back to its display label.</summary>
    public static string FromPaletteId(string palette) => palette switch
    {
        "pepto" => "Pepto",
        "monochrome-green" => "Monochrome green",
        "amber" => "Amber",
        _ => "VICE default"
    };

    /// <summary>Maps an audio-mode display label to its stored id.</summary>
    public static string ToAudioModeId(string audioMode) => audioMode switch
    {
        "Muted" => "muted",
        "Unavailable" => "unavailable",
        _ => "enabled"
    };

    /// <summary>Maps a stored audio-mode id back to its display label.</summary>
    public static string FromAudioModeId(string audioMode) => audioMode switch
    {
        "muted" => "Muted",
        "unavailable" => "Unavailable",
        _ => "Enabled"
    };

    /// <summary>Maps an input-mode display label to its stored id.</summary>
    public static string ToInputModeId(string inputMode) => inputMode switch
    {
        "Keyboard only" => "keyboard-only",
        "Disabled" => "disabled",
        _ => "keyboard-joystick"
    };

    /// <summary>Maps a stored input-mode id back to its display label.</summary>
    public static string FromInputModeId(string inputMode) => inputMode switch
    {
        "keyboard-only" => "Keyboard only",
        "disabled" => "Disabled",
        _ => "Keyboard + joystick"
    };

    /// <summary>Maps a resource-mode display label to its stored id.</summary>
    public static string ToResourceModeId(string resourceMode) => resourceMode switch
    {
        "Use configured paths" => "configured-paths",
        "Missing resources" => "missing-resources",
        _ => "auto-detect"
    };

    /// <summary>Maps a stored resource-mode id back to its display label.</summary>
    public static string FromResourceModeId(string resourceMode) => resourceMode switch
    {
        "configured-paths" => "Use configured paths",
        "missing-resources" => "Missing resources",
        _ => "Auto detect"
    };

    /// <summary>Maps a primary-joystick-port display label to its <see cref="InputPort"/>.</summary>
    public static InputPort ToInputPort(string inputPort) =>
        string.Equals(inputPort, "Joystick 1", StringComparison.OrdinalIgnoreCase)
            ? InputPort.Joystick1
            : InputPort.Joystick2;

    /// <summary>Maps an <see cref="InputPort"/> back to its primary-joystick-port display label.</summary>
    public static string FromInputPort(InputPort inputPort) =>
        inputPort == InputPort.Joystick1 ? "Joystick 1" : "Joystick 2";

    /// <summary>Maps a pacing-strategy display label to its stored id.</summary>
    public static string ToPacingStrategyId(string strategy) =>
        string.Equals(strategy, "VICE", StringComparison.OrdinalIgnoreCase) ? "vice" : "semaphore";

    /// <summary>Maps a stored pacing-strategy id back to its display label.</summary>
    public static string FromPacingStrategyId(string id) =>
        string.Equals(id, "semaphore", StringComparison.OrdinalIgnoreCase) ? "Semaphore" : "VICE";
}
