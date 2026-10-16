namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP, area XSET. A selectable Commodore computer family shown in the 10-foot
/// Settings "Computer" picker. Only the C64 family (<see cref="FamilyId"/> "x64sc") is
/// implemented today; the other families are disabled placeholders
/// (<see cref="IsAvailable"/> <c>false</c>) so the picker advertises the roadmap without
/// letting the operator strand the session on an unported machine.
/// </summary>
/// <param name="DisplayName">The human-readable family name (for example, "Commodore 64").</param>
/// <param name="FamilyId">The emulator machine id (for example, "x64sc" or "xvic").</param>
/// <param name="IsAvailable">Whether the family is implemented and therefore selectable.</param>
public sealed record ComputerOption(string DisplayName, string FamilyId, bool IsAvailable);
