namespace ViceSharp.Xbox.Input;

/// <summary>
/// Digital gamepad buttons as a bit set.
/// </summary>
/// <remarks>
/// <para>
/// The bit value of every member is <b>identical</b> to the corresponding
/// member of <c>Windows.Gaming.Input.GamepadButtons</c>. This is deliberate:
/// the (later) UWP-on-Xbox adapter reads a <c>GamepadReading</c> and casts its
/// <c>GamepadButtons</c> straight to this type with <b>no per-bit remap</b>, so
/// the layouts must never drift. Slice S7 pins these values with an assert.
/// </para>
/// <para>
/// There is intentionally no <c>Nexus</c>/<c>Guide</c> (Xbox) member: that
/// button is reserved by the shell and never surfaces to the application.
/// The backing type is <see cref="uint"/> to leave room for the paddle bits.
/// </para>
/// </remarks>
[Flags]
public enum GamepadButtonFlags : uint
{
    /// <summary>No button pressed.</summary>
    None = 0x0,

    /// <summary>The Menu ("start") button.</summary>
    Menu = 0x1,

    /// <summary>The View ("back") button.</summary>
    View = 0x2,

    /// <summary>The A face button.</summary>
    A = 0x4,

    /// <summary>The B face button.</summary>
    B = 0x8,

    /// <summary>The X face button.</summary>
    X = 0x10,

    /// <summary>The Y face button.</summary>
    Y = 0x20,

    /// <summary>The up direction on the directional pad.</summary>
    DPadUp = 0x40,

    /// <summary>The down direction on the directional pad.</summary>
    DPadDown = 0x80,

    /// <summary>The left direction on the directional pad.</summary>
    DPadLeft = 0x100,

    /// <summary>The right direction on the directional pad.</summary>
    DPadRight = 0x200,

    /// <summary>The left shoulder ("bumper") button.</summary>
    LeftShoulder = 0x400,

    /// <summary>The right shoulder ("bumper") button.</summary>
    RightShoulder = 0x800,

    /// <summary>The left thumbstick pressed as a button.</summary>
    LeftThumbstick = 0x1000,

    /// <summary>The right thumbstick pressed as a button.</summary>
    RightThumbstick = 0x2000,

    /// <summary>The first paddle (elite/flight-stick class controllers).</summary>
    Paddle1 = 0x4000,

    /// <summary>The second paddle.</summary>
    Paddle2 = 0x8000,

    /// <summary>The third paddle.</summary>
    Paddle3 = 0x10000,

    /// <summary>The fourth paddle.</summary>
    Paddle4 = 0x20000,
}
