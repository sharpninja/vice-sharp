using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;

namespace ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-LAUNCH-05). The Xbox implementation of <see cref="IGameLauncher"/>: it reads the
/// downloaded game's bytes from the app-writable cache and hands them to the in-process session as a
/// payload attach (never an arbitrary path, per the UWP sandbox), then - when autostart is requested -
/// autostarts Drive 8 for a disk or cold-resets for a cartridge/tape.
/// </summary>
public sealed class XboxGameLauncher : IGameLauncher
{
    private readonly IXboxLaunchSession _session;

    /// <summary>Creates the launcher.</summary>
    /// <param name="session">The in-process launch session surface.</param>
    public XboxGameLauncher(IXboxLaunchSession session) =>
        _session = session ?? throw new ArgumentNullException(nameof(session));

    /// <inheritdoc />
    public async Task<LaunchOutcome> LaunchAsync(
        AcquiredGame game,
        MediaSlot slot,
        bool autostart,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);

        byte[] payload = await File.ReadAllBytesAsync(game.LocalPath, cancellationToken).ConfigureAwait(false);

        bool attached = await _session
            .AttachMediaAsync(slot, game.LocalPath, isReadOnly: true, payload, game.FileName, cancellationToken)
            .ConfigureAwait(false);

        if (!attached)
        {
            return new LaunchOutcome(false, $"Attach failed for {game.FileName}.");
        }

        if (autostart)
        {
            if (slot == MediaSlot.Drive8)
            {
                await _session.AutostartDrive8Async(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _session.ColdResetAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return new LaunchOutcome(true, autostart ? $"Started {game.FileName}" : $"Attached {game.FileName}");
    }
}
