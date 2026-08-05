using ViceSharp.Protocol;

namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-LAUNCH-001. The seam that attaches a downloaded game to a media slot and optionally boots it.
/// Implemented per head (Xbox payload-attach; desktop shell delegation) so the portable browser VM
/// stays engine- and host-free.
/// </summary>
public interface IGameLauncher
{
    /// <summary>
    /// AC-LAUNCH-04/05/06. Attaches <paramref name="game"/> to <paramref name="slot"/>; when
    /// <paramref name="autostart"/> is <c>true</c>, also boots it (disk autostart / cartridge cold reset).
    /// </summary>
    /// <param name="game">The downloaded game handle.</param>
    /// <param name="slot">The media slot to attach to.</param>
    /// <param name="autostart">Whether to boot after attaching.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<LaunchOutcome> LaunchAsync(
        AcquiredGame game,
        MediaSlot slot,
        bool autostart,
        CancellationToken cancellationToken = default);
}
