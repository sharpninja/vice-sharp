using ViceSharp.Library.ViewModels;

namespace ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 (AC-BROWSE-02). A fixed C64 <see cref="ICurrentMachineProvider"/> for the Xbox head's
/// library (the console runs one machine per session; the library is scoped to it). Pure C#, so it also
/// builds under the net10.0 fallback.
/// </summary>
public sealed class C64MachineProvider : ICurrentMachineProvider
{
    /// <inheritdoc />
    public string GetActivePlatformSlug() => MachinePlatformSlug.ToSlug(LibraryMachine.C64);

    /// <inheritdoc />
    public event EventHandler? PlatformChanged
    {
        add { }
        remove { }
    }
}
