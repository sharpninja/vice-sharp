using ViceSharp.Protocol;

namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-LAUNCH-001. How a file attaches to the emulator: its media <see cref="Kind"/>, the
/// default <see cref="Slot"/> it targets (<c>null</c> when it cannot be attached), and whether it
/// can be launched (attached and booted) directly.
/// </summary>
/// <param name="Kind">The media nature.</param>
/// <param name="Slot">The default media slot, or <c>null</c> when the file is not attachable.</param>
/// <param name="IsLaunchable">Whether the file can be attached and booted.</param>
public readonly record struct MediaMapping(MediaKind Kind, MediaSlot? Slot, bool IsLaunchable);
