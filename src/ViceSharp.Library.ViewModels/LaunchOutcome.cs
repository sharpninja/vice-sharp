namespace ViceSharp.Library.ViewModels;

/// <summary>FR-ROMM-LAUNCH-001. The result of attaching and (optionally) booting a game.</summary>
/// <param name="Success">Whether the attach/boot succeeded.</param>
/// <param name="Message">A short human-readable outcome message.</param>
public sealed record LaunchOutcome(bool Success, string Message);
