using ViceSharp.Architectures;
using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// Full system integration test
/// </summary>
public static class SystemTest
{
    /// <summary>
    /// Run system boot test using ArchitectureBuilder
    /// </summary>
    public static void RunBootTest()
    {
        var builder = new ArchitectureBuilder();
        var c64 = builder.Build(new C64NtscDescriptor());
        c64.Reset();

        // Run 10 frames
        for (int i = 0; i < 10; i++)
        {
            c64.RunFrame();
        }

        var state = c64.GetState();
        Console.WriteLine($"After 10 frames: PC=${state.PC:X4}, A=${state.A:X2}, X=${state.X:X2}");
        Console.WriteLine("System test completed successfully");
    }
}
