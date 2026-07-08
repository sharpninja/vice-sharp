namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.RomFetch;

internal static class MachineTestFactory
{
    public static IMachine CreateC64Machine()
        => CreateC64Machine(new Architectures.C64.C64Descriptor());

    public static IMachine CreateC64Machine(string modelSelector)
        => CreateC64Machine(new Architectures.C64.C64Descriptor(modelSelector));

    public static IMachine CreateC64Machine(Architectures.C64.C64Descriptor descriptor)
    {
        var builder = new ArchitectureBuilder(CreateC64RomProvider());
        return builder.Build(descriptor);
    }

    public static IRomProvider CreateC64RomProvider()
    {
        var dataRoots = ViceDataPathResolver.FindDataRoots();
        foreach (var dataRoot in dataRoots)
        {
            var provider = new RomProvider(dataRoot, dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        // Test-harness fallback: the repo vendors the full VICE data tree at
        // native/vice/vice/data, so a bare checkout (CI agents with no
        // VICESHARP_ROM_PATH and no x64sc on PATH) is self-sufficient. The
        // product resolver deliberately does NOT know about repo layouts.
        var repoData = FindRepoViceDataRoot();
        if (repoData is not null)
        {
            var provider = new RomProvider(repoData, []);
            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        throw new DirectoryNotFoundException(
            "Could not locate a VICE data root with complete C64 ROM resources. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a VICE data root, or put x64sc.exe on PATH.");
    }

    private static string? FindRepoViceDataRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                continue;

            var data = Path.Combine(dir.FullName, "native", "vice", "vice", "data");
            return Directory.Exists(data) ? data : null;
        }

        return null;
    }

    public static ReadOnlyMemory<byte> LoadC64Rom(string romName)
    {
        return CreateC64RomProvider().LoadRom(romName, "C64");
    }

}
