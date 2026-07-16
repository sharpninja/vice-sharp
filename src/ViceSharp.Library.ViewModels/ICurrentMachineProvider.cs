namespace ViceSharp.Library.ViewModels;

/// <summary>
/// FR-ROMM-BROWSE-001 (AC-BROWSE-02, AC-BROWSE-07). Supplies the active machine's RomM platform slug
/// so the library scopes every query to the machine selected in Settings, and signals when that
/// machine changes so the browser can re-scope and reload. There is no in-library platform picker;
/// the head binds this to the existing Settings machine selection.
/// </summary>
public interface ICurrentMachineProvider
{
    /// <summary>The RomM platform slug for the currently-selected machine (e.g. <c>c64</c>).</summary>
    string GetActivePlatformSlug();

    /// <summary>Raised when the active machine (and therefore the platform slug) changes.</summary>
    event EventHandler PlatformChanged;
}
