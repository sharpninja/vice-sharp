using ViceSharp.Protocol;

namespace ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-LAUNCH-05). The minimal in-process session surface the Xbox game launcher drives:
/// attach media from an in-memory payload (never an arbitrary path, per the UWP sandbox), autostart the
/// disk in Drive 8, or cold-reset (cartridge/tape boot). Implemented by the head over
/// <c>InProcessSessionFacade</c> + the console host; kept as a seam so the launcher is unit-testable
/// without the UWP workload.
/// </summary>
public interface IXboxLaunchSession
{
    /// <summary>Attaches media to <paramref name="slot"/> from an in-memory payload. Returns success.</summary>
    /// <param name="slot">The media slot.</param>
    /// <param name="filePath">The cache path (label only; the payload carries the bytes).</param>
    /// <param name="isReadOnly">Whether to attach read-only.</param>
    /// <param name="payload">The media bytes.</param>
    /// <param name="displayName">A display name for the media.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask<bool> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>Boots the disk currently in Drive 8 (reset + autostart).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask AutostartDrive8Async(CancellationToken cancellationToken = default);

    /// <summary>Cold-resets so an attached cartridge/tape takes over the reset vector and boots.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask ColdResetAsync(CancellationToken cancellationToken = default);
}
