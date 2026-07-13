namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// The top-level screens of the 10-foot (couch) UI that the shell can navigate
/// between with a controller. The root shell view-model (S20) owns the current
/// destination and drives the focus graph and the back-stack from it; every value
/// here is a full-screen or full-overlay surface, not an in-screen focus target.
/// </summary>
public enum NavigationDestination
{
    /// <summary>
    /// The running emulator surface (the pulled video frame fills the screen). This
    /// is the default destination; the locked joystick is live and no menu is shown.
    /// </summary>
    Gameplay,

    /// <summary>The root 10-foot menu overlay, opened from <see cref="Gameplay"/>.</summary>
    MainMenu,

    /// <summary>The settings screen (display / audio / input / resource options).</summary>
    Settings,

    /// <summary>The device screen: attach, eject, and swap media in the drive slots.</summary>
    Devices,

    /// <summary>The ROM browser used to pick or replace the active machine ROM set.</summary>
    Roms,

    /// <summary>The on-screen virtual keyboard overlay for text entry to the machine.</summary>
    VirtualKeyboard,
}
