using BenchmarkDotNet.Running;

namespace ViceSharp.Benchmarks;

/// <summary>
/// BenchmarkDotNet harness entry point.
/// Run via: dotnet run -c Release --project tests/ViceSharp.Benchmarks
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
        switcher.Run(args);
        return 0;
    }
}
