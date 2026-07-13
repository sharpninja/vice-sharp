namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S30 (IMPL-XBOXUWP-030), area XBOXUI. One read-only row of the
/// input-mapping display: a controller input label paired with the action it drives.
/// </summary>
/// <remarks>
/// Value type semantics (record): two rows are equal when both their
/// <see cref="InputLabel"/> and <see cref="ActionLabel"/> match, which is what the S30
/// stability guard relies on. This is a display DTO only; it carries no binding
/// behavior and the mapping page never rebinds through it (remap persistence is S12 /
/// S26 territory).
/// </remarks>
/// <param name="InputLabel">The controller input as shown to the player (e.g. "Menu", "LB", "A").</param>
/// <param name="ActionLabel">The action the input drives (e.g. "Open main menu", "JOY2 fire").</param>
public sealed record InputMappingRow(string InputLabel, string ActionLabel);
