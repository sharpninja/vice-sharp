using ViceSharp.Architectures;

namespace ViceSharp.TestHarness;

/// <summary>
/// Full system integration test
/// </summary>
public static class SystemTest
{
    /// <summary>
    /// Run system boot test
    /// </summary>
    public static void RunBootTest()
    {
        var c64 = new C64Machine();
        c64.Reset();

        // Run 10 frames
        for (int i = 0; i < 10; i++)
        {
            c64.RunFrame();
        }

        Console.WriteLine("System test completed successfully");
    }
}