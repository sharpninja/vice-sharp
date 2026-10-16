// FEAT-XDEFAULTCART-001: PORTABLE (no #if HAS_UWP) so the policy is unit-testable on the
// workload-free net10.0 fallback; the UWP head calls it with LocalState paths.
namespace ViceSharp.Xbox.Platform;

using System;
using System.IO;
using System.Linq;
using ViceSharp.Core.Configuration;
using ViceSharp.Protocol;

/// <summary>
/// FEAT-XDEFAULTCART-001 (operator 2026-07-14): the S-Blox cartridge ships EMBEDDED in
/// this assembly and loads BY DEFAULT until the user selects different media, recorded
/// through the canonical vice.ini exactly the way VICE does it: <c>[C64] CartridgeFile</c>
/// (+ <c>CartridgeType=0</c>, the .crt auto-detect type) via the Core
/// <see cref="ViceSettings"/> INI writer.
/// </summary>
/// <remarks>
/// Policy: on boot, an ABSENT CartridgeFile means first run: the embedded default is
/// extracted to the cartridge directory and written into vice.ini; a PRESENT value is the
/// standing selection (default or user) and is attached as-is; a present value whose file
/// vanished resolves to nothing (no forced re-default: the resource stays user-owned).
/// User selections keep vice.ini normal: attaching another cartridge replaces
/// CartridgeFile, attaching non-cartridge media (disk/tape) or detaching the cartridge
/// clears it, so the default never overrides an explicit media choice.
/// </remarks>
public static class DefaultCartridgeBoot
{
    private const string Section = "C64";
    private const string CartridgeFileKey = "CartridgeFile";
    private const string CartridgeTypeKey = "CartridgeType";

    /// <summary>The extracted default-cartridge file name.</summary>
    public const string DefaultCartridgeFileName = "sblox.CRT";

    /// <summary>
    /// Resolves the cartridge to attach at boot: the standing vice.ini selection, or (on
    /// first run, when no <c>CartridgeFile</c> exists) the embedded S-Blox default,
    /// extracted to <paramref name="cartridgeDirectory"/> and recorded in vice.ini.
    /// </summary>
    /// <param name="settings">The canonical vice.ini settings (Core INI reader/writer).</param>
    /// <param name="cartridgeDirectory">Directory the embedded default extracts into.</param>
    /// <returns>The cartridge path to attach, or <c>null</c> for none.</returns>
    public static string? ResolveBootCartridge(ViceSettings settings, string cartridgeDirectory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(cartridgeDirectory);

        var configured = settings.Get(Section, CartridgeFileKey);
        if (configured is not null)
        {
            // A standing selection (default or user), INCLUDING an explicit empty value
            // (the user moved to disk/tape media): honor it; never re-default.
            return configured.Length > 0 && File.Exists(configured) ? configured : null;
        }

        // First run: extract the embedded default and record it exactly like VICE would.
        Directory.CreateDirectory(cartridgeDirectory);
        var path = Path.Combine(cartridgeDirectory, DefaultCartridgeFileName);
        if (!File.Exists(path))
        {
            var assembly = typeof(DefaultCartridgeBoot).Assembly;
            var resource = assembly.GetManifestResourceNames()
                .Single(n => n.EndsWith(DefaultCartridgeFileName, StringComparison.OrdinalIgnoreCase));
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Embedded cartridge '{resource}' has no stream.");
            using var file = File.Create(path);
            stream.CopyTo(file);
        }

        settings.SetVice(Section, CartridgeFileKey, path);
        settings.SetVice(Section, CartridgeTypeKey, "0");
        settings.Save();
        return path;
    }

    /// <summary>
    /// Records a USER media selection in vice.ini as normal: another cartridge replaces
    /// <c>CartridgeFile</c>; non-cartridge media (disk/tape) or a cartridge detach
    /// (<paramref name="path"/> <c>null</c>) clears it, so the boot default stops
    /// overriding the explicit choice.
    /// </summary>
    /// <param name="settings">The canonical vice.ini settings.</param>
    /// <param name="slot">The media slot the user changed.</param>
    /// <param name="path">The attached media path, or <c>null</c> for a detach.</param>
    public static void NoteUserMediaSelection(ViceSettings settings, MediaSlot slot, string? path)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (slot == MediaSlot.Cartridge && !string.IsNullOrEmpty(path))
        {
            settings.SetVice(Section, CartridgeFileKey, path);
            settings.SetVice(Section, CartridgeTypeKey, "0");
        }
        else
        {
            // Disk/tape selection or cartridge detach: the cartridge default steps aside.
            settings.SetVice(Section, CartridgeFileKey, string.Empty);
        }

        settings.Save();
    }
}
