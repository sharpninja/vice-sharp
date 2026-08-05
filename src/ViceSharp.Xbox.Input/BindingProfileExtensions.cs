namespace ViceSharp.Xbox.Input;

using System;
using System.Collections.Generic;

/// <summary>
/// Remap helpers for <see cref="BindingProfile"/> (PLAN-XBOXUWP S12,
/// IMPL-XBOXUWP-012). Every helper is PURE and returns a NEW profile; the input
/// profile and its <see cref="BindingProfile.Gameplay"/> list are never mutated,
/// matching the record's by-value contract.
/// </summary>
public static class BindingProfileExtensions
{
    /// <summary>
    /// Returns a copy of <paramref name="profile"/> whose gameplay binding for
    /// <paramref name="input"/> is replaced with (<paramref name="command"/>,
    /// <paramref name="activation"/>), or appended when <paramref name="input"/> is
    /// not yet bound. Row order is preserved (a replaced row keeps its position; a new
    /// row is appended at the end).
    /// </summary>
    /// <param name="profile">The profile to remap (unchanged; a new profile is returned).</param>
    /// <param name="input">The bindable input whose binding is being set.</param>
    /// <param name="command">The command the input should now emit.</param>
    /// <param name="activation">The activation style for the new binding.</param>
    /// <returns>A new profile carrying the remapped binding.</returns>
    public static BindingProfile WithBinding(
        this BindingProfile profile,
        BindableInput input,
        AppCommand command,
        BindingActivation activation)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var updated = new List<ButtonBinding>(profile.Gameplay.Count + 1);
        bool replaced = false;

        foreach (ButtonBinding binding in profile.Gameplay)
        {
            if (binding.Input == input)
            {
                updated.Add(new ButtonBinding(input, command, activation));
                replaced = true;
            }
            else
            {
                updated.Add(binding);
            }
        }

        if (!replaced)
        {
            updated.Add(new ButtonBinding(input, command, activation));
        }

        return profile with { Gameplay = updated };
    }
}
