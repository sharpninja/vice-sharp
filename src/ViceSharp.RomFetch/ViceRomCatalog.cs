namespace ViceSharp.RomFetch;

/// <summary>
/// Catalog of candidate ROM filenames per VICE system and logical ROM role.
///
/// Grounded in the GTK3 VICE 3.8 data-tree layout, where each system has a data
/// subdirectory (<c>&lt;dir&gt;/&lt;file&gt;</c>) and a role (kernal, basic,
/// chargen, ...) may be satisfied by any of several dumped revisions. The
/// single-name-per-role approach in <see cref="RomProvider"/>/the per-architecture
/// rom sets is brittle: a given install or revision ships only some of these
/// files. A resolver should try each candidate for a role in order
/// (most-preferred first - the model VICE ships as that system's default) and
/// accept the first that exists and validates.
///
/// A system's <see cref="RomSystem.DataDirectory"/> is usually its key, but the
/// TED family (C16/C116) shares Plus/4's <c>PLUS4</c> directory, so the key and
/// directory are tracked separately.
/// </summary>
public static class ViceRomCatalog
{
    /// <summary>A VICE system: the data subdirectory it loads from and its ROM roles.</summary>
    public sealed record RomSystem(
        string DataDirectory,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Roles);

    private static readonly IReadOnlyDictionary<string, RomSystem> _systems =
        new Dictionary<string, RomSystem>(StringComparer.OrdinalIgnoreCase)
        {
            // C64 / C64C / SCPU64 share these ROMs. KERNAL rev3 (901227-03) is
            // the default; other dumps are SX-64, GS, 4064/Educator, etc.
            ["C64"] = Sys("C64",
                Role("kernal", "kernal-901227-03.bin", "kernal-901227-02.bin", "kernal-901227-01.bin",
                               "kernal-251104-04.bin", "kernal-906145-02.bin", "kernal-901246-01.bin",
                               "kernal-390852-01.bin"),
                Role("basic", "basic-901226-01.bin"),
                Role("chargen", "chargen-901225-01.bin", "chargen-906143-02.bin")),

            ["C64DTV"] = Sys("C64DTV",
                Role("kernal", "kernal-901227-03.bin"),
                Role("basic", "basic-901226-01.bin"),
                Role("chargen", "chargen-901225-01.bin")),

            // C128 native mode (kernal/basiclo/basichi/chargen) plus its built-in
            // C64 mode (kernal64/basic64). Localized kernals (fi/fr/it/no) are
            // extensionless in the VICE tree.
            ["C128"] = Sys("C128",
                Role("kernal", "kernal-318020-05.bin", "kernal-315078-03.bin", "kernal-318034-01.bin",
                               "kernal-325172-01.bin", "kernalfi", "kernalfr", "kernalit", "kernalno"),
                Role("kernal64", "kernal64-901227-03.bin", "kernal64-325179-01.bin", "kernal64-325182-01.bin"),
                Role("basiclo", "basiclo-318018-04.bin"),
                Role("basichi", "basichi-318019-04.bin"),
                Role("basic64", "basic64-901226-01.bin"),
                Role("chargen", "chargen-390059-01.bin", "chargen-315079-01.bin", "chargen-325078-02.bin",
                                "chargen-325167-01.bin", "chargen-325167-02.bin", "chargen-325173-01D.bin",
                                "chargen-325181-01.bin")),

            // VIC-20 kernals carry a dot rather than a dash in the VICE tree.
            ["VIC20"] = Sys("VIC20",
                Role("kernal", "kernal.901486-07.bin", "kernal.901486-06.bin", "kernal.901486-02.bin"),
                Role("basic", "basic-901486-01.bin"),
                Role("chargen", "chargen-901460-03.bin", "chargen-901460-02.bin")),

            // Plus/4: TED family with the 3plus1 built-in software (function ROM).
            ["PLUS4"] = Sys("PLUS4",
                Role("kernal", "kernal-318004-05.bin", "kernal-318004-01.bin", "kernal-318005-05.bin",
                               "kernal-364.bin"),
                Role("basic", "basic-318006-01.bin"),
                Role("function-lo", "3plus1-317053-01.bin"),
                Role("function-hi", "3plus1-317054-01.bin")),

            // C16 / C116: same TED family and PLUS4 data directory as Plus/4, same
            // KERNAL/BASIC, but no 3plus1 function ROM (no built-in software).
            ["C16"] = Sys("PLUS4",
                Role("kernal", "kernal-318004-05.bin", "kernal-318004-01.bin", "kernal-318005-05.bin"),
                Role("basic", "basic-318006-01.bin")),

            ["C116"] = Sys("PLUS4",
                Role("kernal", "kernal-318004-05.bin", "kernal-318004-01.bin", "kernal-318005-05.bin"),
                Role("basic", "basic-318006-01.bin")),

            // PET: BASIC/KERNAL/EDIT are versioned by BASIC generation (1/2/4)
            // and screen width / refresh. Defaults here are BASIC 4, 80-col.
            ["PET"] = Sys("PET",
                Role("kernal", "kernal-4.901465-22.bin", "kernal-2.901465-03.bin", "kernal-1.901439-04-07.bin"),
                Role("basic", "basic-4.901465-23-20-21.bin", "basic-2.901465-01-02.bin",
                              "basic-1.901439-09-05-02-06.bin"),
                Role("edit", "edit-4-80-b-60Hz.901474-03.bin", "edit-4-80-b-50Hz.901474-04.bin",
                             "edit-4-40-n-60Hz.901499-01.bin", "edit-4-40-n-50Hz.901498-01.bin",
                             "edit-2-n.901447-24.bin", "edit-1-n.901439-03.bin")),

            // CBM-II (6x0/7x0) - "+" in dump names spans multiple part numbers.
            ["CBM-II"] = Sys("CBM-II",
                Role("kernal", "kernal-901234-02.bin", "kernal-901244-04a.bin"),
                Role("basic", "basic-901235+6-02.bin", "basic-901240+1-03.bin", "basic-901242+3-04a.bin"),
                Role("chargen", "chargen-901225-01.bin", "chargen-901232-01.bin", "chargen-901237-01.bin")),

            // Disk-drive DOS ROMs, keyed by drive model.
            ["DRIVES"] = Sys("DRIVES",
                Role("1540", "dos1540-325302+3-01.bin"),
                Role("1541", "dos1541-325302-01+901229-05.bin"),
                Role("1541ii", "dos1541ii-251968-03.bin"),
                Role("1551", "dos1551-318008-01.bin"),
                Role("1570", "dos1570-315090-01.bin"),
                Role("1571", "dos1571-310654-05.bin"),
                Role("1571cr", "dos1571cr-318047-01.bin"),
                Role("1581", "dos1581-318045-02.bin"),
                Role("2031", "dos2031-901484-03+05.bin"),
                Role("2040", "dos2040-901468-06+07.bin"),
                Role("3040", "dos3040-901468-11-13.bin"),
                Role("4040", "dos4040-901468-14-16.bin"),
                Role("1001", "dos1001-901887+8-01.bin"),
                Role("9000", "dos9000-300516+7-revC.bin")),
        };

    /// <summary>All systems, each mapping a logical ROM role to ordered candidate filenames.</summary>
    public static IReadOnlyDictionary<string, RomSystem> Systems => _systems;

    /// <summary>Known system keys.</summary>
    public static IReadOnlyCollection<string> SystemKeys => (IReadOnlyCollection<string>)_systems.Keys;

    /// <summary>The VICE data subdirectory a system loads ROMs from (null if unknown).</summary>
    public static string? DataDirectory(string system) =>
        _systems.TryGetValue(system, out var entry) ? entry.DataDirectory : null;

    /// <summary>
    /// Candidate filenames for a system's ROM role, ordered most-preferred first.
    /// Returns an empty list for an unknown system or role.
    /// </summary>
    public static IReadOnlyList<string> Candidates(string system, string role) =>
        _systems.TryGetValue(system, out var entry) && entry.Roles.TryGetValue(role, out var candidates)
            ? candidates
            : Array.Empty<string>();

    private static RomSystem Sys(string dataDirectory, params (string Role, string[] Candidates)[] roles)
    {
        var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (role, candidates) in roles)
            map[role] = candidates;
        return new RomSystem(dataDirectory, map);
    }

    private static (string Role, string[] Candidates) Role(string role, params string[] candidates) =>
        (role, candidates);
}
