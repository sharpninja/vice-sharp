namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001, TR-XPATH-001. The pure
/// first-run ROM-provisioning classifier. It reads the three core C64 ROM files in a given
/// directory and reports per-role presence and the overall <see cref="RomProvisionState"/>.
/// </summary>
/// <remarks>
/// <para>
/// The only side effect is reading the files in the supplied directory: there is no static
/// mutable state, no network, and no clock. Presence is classified against the injected
/// <see cref="RomCatalog"/> (defaulting to <see cref="RomCatalog.C64"/>) using the same
/// SHA256 the head's verified download pins (<c>RomProvider.cs:127-129</c> parity).
/// </para>
/// <para>
/// Overall state (per the S28 rules): any present-but-wrong file makes the whole set
/// <see cref="RomProvisionState.Invalid"/>; otherwise all required roles present is
/// <see cref="RomProvisionState.Complete"/>, no roles present is
/// <see cref="RomProvisionState.NotProvisioned"/>, and anything between is
/// <see cref="RomProvisionState.Partial"/>. Under <see cref="RomProfile.Ultimax"/> the KERNAL
/// is not a required role, so its absence does not block <c>Complete</c>
/// (<c>C64RomLoader.cs:192-193</c> parity).
/// </para>
/// </remarks>
public sealed class RomProvisionEvaluator
{
    private static readonly RomRole[] AllRoles = { RomRole.Basic, RomRole.Kernal, RomRole.Chargen };

    /// <summary>Creates an evaluator over a ROM catalog.</summary>
    /// <param name="catalog">The catalog of expected ROM specs, or <c>null</c> to use <see cref="RomCatalog.C64"/>.</param>
    public RomProvisionEvaluator(RomCatalog? catalog = null)
    {
        Catalog = catalog ?? RomCatalog.C64;
    }

    /// <summary>The catalog this evaluator classifies against (also used by the import validator).</summary>
    public RomCatalog Catalog { get; }

    /// <summary>
    /// Evaluates ROM provisioning in <paramref name="c64Directory"/> under a profile.
    /// </summary>
    /// <param name="c64Directory">The C64 ROM directory to inspect (need not exist).</param>
    /// <param name="profile">The requirement profile that governs which roles are required.</param>
    /// <returns>The provisioning assessment.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="c64Directory"/> is <c>null</c>.</exception>
    public RomProvisionAssessment Evaluate(string c64Directory, RomProfile profile)
    {
        ArgumentNullException.ThrowIfNull(c64Directory);

        var roles = new List<RomRoleStatus>(AllRoles.Length);
        var anyInvalid = false;
        var presentCount = 0;
        var requiredCount = 0;
        var requiredPresentCount = 0;

        foreach (var role in AllRoles)
        {
            var required = IsRequired(role, profile);
            if (required)
            {
                requiredCount++;
            }

            if (!Catalog.TryGetSpec(role, out var spec))
            {
                // A role absent from the catalog cannot be required or classified; treat as missing.
                roles.Add(new RomRoleStatus(role, string.Empty, RomPresence.Missing, required));
                continue;
            }

            var presence = Classify(c64Directory, spec);
            roles.Add(new RomRoleStatus(role, spec.FileName, presence, required));

            switch (presence)
            {
                case RomPresence.Invalid:
                    anyInvalid = true;
                    break;
                case RomPresence.Present:
                    presentCount++;
                    if (required)
                    {
                        requiredPresentCount++;
                    }

                    break;
            }
        }

        var state = anyInvalid
            ? RomProvisionState.Invalid
            : requiredPresentCount == requiredCount
                ? RomProvisionState.Complete
                : presentCount == 0
                    ? RomProvisionState.NotProvisioned
                    : RomProvisionState.Partial;

        var isBootBlocked = state != RomProvisionState.Complete;
        return new RomProvisionAssessment(state, roles, isBootBlocked);
    }

    private static bool IsRequired(RomRole role, RomProfile profile) =>
        !(profile == RomProfile.Ultimax && role == RomRole.Kernal);

    private static RomPresence Classify(string c64Directory, RomSpec spec)
    {
        var path = Path.Combine(c64Directory, spec.FileName);
        if (!File.Exists(path))
        {
            return RomPresence.Missing;
        }

        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (IOException)
        {
            // An unreadable present file is a provisioning problem, not an absence.
            return RomPresence.Invalid;
        }
        catch (UnauthorizedAccessException)
        {
            return RomPresence.Invalid;
        }

        if (data.Length != spec.ExpectedSize)
        {
            return RomPresence.Invalid;
        }

        var actual = Convert.ToHexString(SHA256.HashData(data));
        return string.Equals(actual, spec.ExpectedSha256, StringComparison.OrdinalIgnoreCase)
            ? RomPresence.Present
            : RomPresence.Invalid;
    }
}
