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

        RpcStatus status = autostart
            ? await _target.DropAndStartFileAsync(game.LocalPath, cancellationToken).ConfigureAwait(false)
            : await _target.AttachFileAsync(slot, game.LocalPath, cancellationToken).ConfigureAwait(false);

        return new LaunchOutcome(status.IsSuccess, status.Message);
    }
}
