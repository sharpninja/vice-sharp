namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-002, TR-XPATH-001. An immutable
/// set of <see cref="RomSpec"/> keyed by <see cref="RomRole"/>. <see cref="C64"/> is the
/// production catalog for the three core C64 ROMs.
/// </summary>
/// <remarks>
/// The catalog is injectable (the evaluator/ViewModel take one) rather than hard-wired to
/// <see cref="C64"/> so the off-console suite can exercise the pure classification logic
/// against synthetic byte-sets: ViceSharp ships no Commodore ROMs, so tests cannot reproduce
/// the real pinned digests with real bytes. The default remains <see cref="C64"/>.
/// </remarks>
public sealed class RomCatalog
{
    private readonly IReadOnlyDictionary<RomRole, RomSpec> _byRole;

    /// <summary>
    /// The production core-C64 catalog. The names/sizes/SHA256 are copied verbatim from the
    /// RomFetch <c>RomProvider.cs:127-129</c> download pins; the MD5/size are copied from
    /// <c>C64RomLoader.cs:13-80</c>. ViceSharp ships none of these ROMs.
    /// </summary>
    public static RomCatalog C64 { get; } = new RomCatalog(new[]
    {
        // basic-901226-01.bin: 8 KiB. SHA256 = RomProvider.cs:127; MD5 = C64RomLoader.cs:18.
        new RomSpec(
            RomRole.Basic,
            "basic-901226-01.bin",
            8192,
            "89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D",
            "57af4ae21d4b705c2991d98ed5c1f7b8"),

        // kernal-901227-03.bin: 8 KiB. SHA256 = RomProvider.cs:128; MD5 = C64RomLoader.cs:27.
        new RomSpec(
            RomRole.Kernal,
            "kernal-901227-03.bin",
            8192,
            "83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721",
            "39065497630802346bce17963f13c092"),

        // chargen-901225-01.bin: 4 KiB. SHA256 = RomProvider.cs:129; MD5 = C64RomLoader.cs:78.
        new RomSpec(
            RomRole.Chargen,
            "chargen-901225-01.bin",
            4096,
            "FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420",
            "12a4202f5331d45af846af6c58fba946"),
    });

    /// <summary>Creates a catalog from a set of specs (one per role).</summary>
    /// <param name="specs">The ROM specs; each role must appear at most once.</param>
    /// <exception cref="ArgumentNullException"><paramref name="specs"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">A role is duplicated.</exception>
    public RomCatalog(IReadOnlyList<RomSpec> specs)
    {
        ArgumentNullException.ThrowIfNull(specs);

        Specs = specs.ToArray();
        _byRole = Specs.ToDictionary(spec => spec.Role);
    }

    /// <summary>The ROM specs in declaration order.</summary>
    public IReadOnlyList<RomSpec> Specs { get; }

    /// <summary>Gets the spec for a role.</summary>
    /// <param name="role">The role to look up.</param>
    /// <returns>The matching spec.</returns>
    /// <exception cref="KeyNotFoundException">The role is not in the catalog.</exception>
    public RomSpec GetSpec(RomRole role) => _byRole[role];

    /// <summary>Attempts to get the spec for a role.</summary>
    /// <param name="role">The role to look up.</param>
    /// <param name="spec">The matching spec, when found.</param>
    /// <returns><c>true</c> when the role is in the catalog.</returns>
    public bool TryGetSpec(RomRole role, out RomSpec spec) => _byRole.TryGetValue(role, out spec!);
}
