namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
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
        var romBasePath = FindRomBasePath();
        return new RomProvider(romBasePath, FindNativeViceRomFallbacks(romBasePath));
    }

    public static ReadOnlyMemory<byte> LoadC64Rom(string romName)
    {
        return CreateC64RomProvider().LoadRom(romName, "C64");
    }

    private static string FindRomBasePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var romBasePath = Path.Combine(dir.FullName, "roms");
            if (Directory.Exists(Path.Combine(romBasePath, "C64")))
            {
                return romBasePath;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo roms/C64 directory for test run.");
    }

    private static IEnumerable<string> FindNativeViceRomFallbacks(string romBasePath)
    {
        var dir = new DirectoryInfo(romBasePath);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "native", "vice", "vice", "data");
            if (Directory.Exists(Path.Combine(candidate, "C64")))
                yield return candidate;

            dir = dir.Parent;
        }
    }
}
