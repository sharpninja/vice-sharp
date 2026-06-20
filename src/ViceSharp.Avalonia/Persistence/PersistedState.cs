namespace ViceSharp.Avalonia.Persistence;

/// <summary>
/// Snapshot of UI state persisted between runs in <c>vice-sharp.ini</c>. The two
/// "on exit" toggles gate BOTH saving and restoring: <see cref="Settings"/> is
/// non-null only when <see cref="SaveSettingsOnExit"/> was enabled at the last
/// save, and <see cref="Transient"/> only when <see cref="SaveTransientValuesOnExit"/>
/// was. That way disabling a toggle takes effect immediately on the next launch
/// even if stale keys remain in the file.
/// </summary>
public sealed record PersistedState(
    bool SaveSettingsOnExit,
    bool SaveTransientValuesOnExit,
    PersistedSettings? Settings,
    PersistedTransient? Transient);

/// <summary>Durable user preferences (the Settings tab).</summary>
public sealed record PersistedSettings(
    double LimiterRatePercent,
    bool LimiterEnabled,
    string MachineProfileId,
    string Renderer,
    string DisplayScale,
    string CropMode,
    string AspectMode,
    string Palette,
    string AudioMode,
    string InputMode,
    string PrimaryJoystickPort,
    bool SwapJoystickPorts,
    string ResourceMode,
    int DockSide,
    string PacingStrategy = "Semaphore",
    double MasterVolumePercent = 100,
    bool Muted = false);

/// <summary>Per-session runtime state: attached media + selected keyboard map.</summary>
public sealed record PersistedTransient(
    IReadOnlyList<PersistedAttachment> Attachments,
    string? KeyboardMapId,
    string? KeyboardMapSourcePath);

/// <summary>One attached medium to re-apply on startup.</summary>
public sealed record PersistedAttachment(
    string Slot,
    string FilePath,
    bool IsReadOnly,
    bool TrueDrive);
