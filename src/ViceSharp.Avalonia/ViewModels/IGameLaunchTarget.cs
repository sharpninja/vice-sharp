using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// PLAN-ROMM-001 (AC-LAUNCH-06). The minimal shell surface the RomM game launcher drives: attach + start
/// a downloaded file (autostart) or attach it to a specific slot without starting (attach-only).
/// Implemented by <see cref="ShellViewModel"/>; kept as a seam so the launcher is unit-testable without a
/// full shell.
/// </summary>
public interface IGameLaunchTarget
{
    /// <summary>Attach the file to its extension-inferred slot and boot it.</summary>
    /// <param name="filePath">The local media file path.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<RpcStatus> DropAndStartFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Attach the file to <paramref name="slot"/> without starting.</summary>
    /// <param name="slot">The media slot to attach to.</param>
    /// <param name="filePath">The local media file path.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<RpcStatus> AttachFileAsync(MediaSlot slot, string filePath, CancellationToken ct = default);
}
