using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// PLAN-ROMM-001 (AC-LAUNCH-06). The desktop implementation of <see cref="IGameLauncher"/>: it delegates
/// to the shell's <see cref="IGameLaunchTarget.DropAndStartFileAsync"/> (attach + boot) when autostart is
/// requested, otherwise to <see cref="IGameLaunchTarget.AttachFileAsync"/> (attach only), and maps the
/// resulting <see cref="RpcStatus"/> to a <see cref="LaunchOutcome"/>.
/// </summary>
public sealed class AvaloniaGameLauncher : IGameLauncher
{
    private readonly IGameLaunchTarget _target;

    /// <summary>Creates the launcher.</summary>
    /// <param name="target">The shell launch surface.</param>
    public AvaloniaGameLauncher(IGameLaunchTarget target) =>
        _target = target ?? throw new ArgumentNullException(nameof(target));

    /// <inheritdoc />
    public async Task<LaunchOutcome> LaunchAsync(
        AcquiredGame game,
        MediaSlot slot,
        bool autostart,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);

        try
        {
            // Stay on the UI SynchronizationContext: DropAndStart/Attach update AttachPanelView
            // controls. ConfigureAwait(false) caused InvalidOperationException (cross-thread) on
            // attach + autoplay after gRPC resumed on a thread-pool thread.
            RpcStatus status = autostart
                ? await _target.DropAndStartFileAsync(game.LocalPath, cancellationToken).ConfigureAwait(true)
                : await _target.AttachFileAsync(slot, game.LocalPath, cancellationToken).ConfigureAwait(true);

            return new LaunchOutcome(status.IsSuccess, status.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new LaunchOutcome(false, "Launch canceled.");
        }
        catch (Exception ex)
        {
            // FR-ROMM-LAUNCH-001 / AC-LAUNCH-06: host/media failures must not escape
            // an async UI event handler and terminate the desktop application.
            string action = autostart ? "Launch" : "Attach";
            return new LaunchOutcome(false, $"{action} failed for {game.FileName}: {ex.Message}");
        }
    }
}
