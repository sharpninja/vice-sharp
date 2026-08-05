namespace ViceSharp.Xbox.Input;

using System;
using System.Text.Json;

/// <summary>
/// An in-memory <see cref="IBindingStore"/> that holds the current profile as its
/// serialized JSON (PLAN-XBOXUWP S12, IMPL-XBOXUWP-012). Every <see cref="Save"/>
/// serializes through <see cref="BindingJsonContext"/> and every <see cref="Load"/>
/// deserializes back through it, so the store exercises the REAL source-generated
/// serializer (not a shortcut that keeps the live object graph). The actual
/// file/INI wiring is a later slice (S29); this portable store lets the model and
/// its round-trip be unit-tested with no file-IO or Core dependency.
/// </summary>
public sealed class InMemoryBindingStore : IBindingStore
{
    /// <summary>The serialized current profile, or null when nothing is saved (defaults).</summary>
    private string? _json;

    /// <inheritdoc />
    public BindingProfile Load()
    {
        if (_json is null)
        {
            return BindingProfile.Default;
        }

        // Deserialize through the source-generated JsonTypeInfo (no reflection).
        BindingProfile? profile = JsonSerializer.Deserialize(_json, BindingJsonContext.Default.BindingProfile);
        return profile ?? BindingProfile.Default;
    }

    /// <inheritdoc />
    public void Save(BindingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Serialize through the source-generated JsonTypeInfo (no reflection).
        _json = JsonSerializer.Serialize(profile, BindingJsonContext.Default.BindingProfile);
    }

    /// <inheritdoc />
    public void ResetToDefaults() => _json = null;
}
