namespace ViceSharp.Xbox.ViewModels;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViceSharp.Protocol;

/// <summary>
/// FEAT-XSETPERSIST-001 (PLAN-XBOXUWP, area XBOXSET). Persists the host-CANONICAL
/// <see cref="SessionSettingsDto"/> to a JSON file so the UWP head can save settings changes
/// in REAL TIME (at the moment each apply succeeds) and reuse them on the next app start
/// (boot the persisted profile, re-apply the rest).
/// </summary>
/// <remarks>
/// <para>
/// Pure file + JSON round-trip (System + Protocol DTOs only, TR-MVVM-001), fully
/// unit-testable headless. Serialization uses a source-generated
/// <see cref="JsonSerializerContext"/> so the path stays reflection-free and Native-AOT-safe
/// (the Release Xbox head publishes AOT).
/// </para>
/// <para>
/// Every member is best-effort and never throws: persistence must never take down the app
/// (a failed save loses nothing but the persisted copy; a missing/corrupt file simply means
/// "no persisted settings" and the head boots its defaults).
/// </para>
/// </remarks>
public static class XboxSettingsStore
{
    /// <summary>
    /// Saves the canonical settings snapshot to <paramref name="path"/> (overwrites).
    /// </summary>
    /// <param name="path">The JSON file path (e.g. LocalState\settings.json).</param>
    /// <param name="settings">The host-canonical settings snapshot to persist.</param>
    /// <returns><c>true</c> when the file was written; <c>false</c> on any failure.</returns>
    public static bool TrySave(string path, SessionSettingsDto settings)
    {
        if (string.IsNullOrEmpty(path) || settings is null)
            return false;

        try
        {
            var json = JsonSerializer.Serialize(settings, XboxSettingsJsonContext.Default.SessionSettingsDto);
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            // Best-effort: a failed save must never take down the app.
            return false;
        }
    }

    /// <summary>
    /// Loads the persisted settings snapshot from <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The JSON file path.</param>
    /// <param name="settings">The loaded snapshot, or <c>null</c>.</param>
    /// <returns>
    /// <c>true</c> when a valid snapshot was loaded; <c>false</c> for a missing, corrupt, or
    /// unreadable file (never throws, never fabricates settings).
    /// </returns>
    public static bool TryLoad(string path, out SessionSettingsDto? settings)
    {
        settings = null;
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            if (!File.Exists(path))
                return false;

            settings = JsonSerializer.Deserialize(
                File.ReadAllText(path), XboxSettingsJsonContext.Default.SessionSettingsDto);
            return settings is not null && !string.IsNullOrWhiteSpace(settings.ProfileId);
        }
        catch
        {
            settings = null;
            return false;
        }
    }
}

/// <summary>
/// Source-generated JSON metadata for <see cref="XboxSettingsStore"/> (reflection-free,
/// Native-AOT-safe serialization of the Protocol settings DTO graph).
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SessionSettingsDto))]
public sealed partial class XboxSettingsJsonContext : JsonSerializerContext
{
}
