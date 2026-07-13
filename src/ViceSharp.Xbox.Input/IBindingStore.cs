namespace ViceSharp.Xbox.Input;

/// <summary>
/// The persistence port for the operator's remappable <see cref="BindingProfile"/>
/// (PLAN-XBOXUWP S12, IMPL-XBOXUWP-012). Implementations round-trip the profile
/// through <see cref="BindingJsonContext"/>; the concrete backing store (in-memory
/// here; a versioned <c>bindings.v1.json</c> file wired to the INI in a later slice,
/// S29) is an implementation detail behind this interface.
/// </summary>
public interface IBindingStore
{
    /// <summary>
    /// Loads the persisted profile, or <see cref="BindingProfile.Default"/> when
    /// nothing has been saved (or the store has been reset).
    /// </summary>
    /// <returns>The current binding profile.</returns>
    BindingProfile Load();

    /// <summary>Persists <paramref name="profile"/> as the current binding profile.</summary>
    /// <param name="profile">The profile to persist.</param>
    void Save(BindingProfile profile);

    /// <summary>
    /// Discards any saved profile so a subsequent <see cref="Load"/> returns
    /// <see cref="BindingProfile.Default"/>.
    /// </summary>
    void ResetToDefaults();
}
