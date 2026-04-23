namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.RomFetch;

internal static class MachineTestFactory
{
    public static IMachine CreateC64Machine()
    {
        var builder = new ArchitectureBuilder(CreateC64RomProvider());
        return builder.Build(new Architectures.C64.C64Descriptor());
    }

    public static IRomProvider CreateC64RomProvider()
    {
        return new RomProvider(FindRomBasePath());
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
}
