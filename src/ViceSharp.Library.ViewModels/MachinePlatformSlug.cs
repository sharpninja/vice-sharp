namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001 (AC-BROWSE-02). Maps the active <see cref="LibraryMachine"/> to the RomM
/// platform slug used to scope every library query. The slug always follows the machine selected in
/// Settings; there is no in-library platform picker.
/// </summary>
public static class MachinePlatformSlug
{
    private static readonly Dictionary<LibraryMachine, string> ToSlugMap = new()
    {
        [LibraryMachine.C64] = "c64",
        [LibraryMachine.C128] = "c128",
        [LibraryMachine.Plus4] = "c-plus-4",
        [LibraryMachine.Vic20] = "vic-20",
        [LibraryMachine.Pet] = "cpet",
    };

    /// <summary>
    /// The RomM platform slug for a machine (e.g. <see cref="LibraryMachine.C64"/> -&gt; <c>c64</c>).
    /// </summary>
    /// <param name="machine">The active machine.</param>
    /// <exception cref="ArgumentOutOfRangeException">The machine has no known slug.</exception>
    public static string ToSlug(LibraryMachine machine) =>
        ToSlugMap.TryGetValue(machine, out string? slug)
            ? slug
            : throw new ArgumentOutOfRangeException(nameof(machine), machine, "Unknown machine.");

    /// <summary>Attempts the reverse mapping from a RomM platform slug to a <see cref="LibraryMachine"/>.</summary>
    /// <param name="slug">The RomM platform slug (case-insensitive).</param>
    /// <param name="machine">The resolved machine when the slug is known.</param>
    /// <returns><c>true</c> when the slug is recognized.</returns>
    public static bool TryFromSlug(string slug, out LibraryMachine machine)
    {
        foreach (KeyValuePair<LibraryMachine, string> pair in ToSlugMap)
        {
            if (string.Equals(pair.Value, slug, StringComparison.OrdinalIgnoreCase))
            {
                machine = pair.Key;
                return true;
            }
        }

        machine = default;
        return false;
    }
}
