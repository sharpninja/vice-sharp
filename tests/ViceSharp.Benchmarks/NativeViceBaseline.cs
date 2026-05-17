namespace ViceSharp.Benchmarks;

/// <summary>
/// Reserved stub for the native VICE comparison workload.
///
/// The plan is to invoke the existing native shim built from
/// <c>native/vice/...</c> through <c>ViceSharp.Core.ViceNative</c>, drive the
/// same scripted workloads as the managed benchmarks, and emit comparable
/// timing measurements so the Completion Dashboard can ingest both sides.
///
/// TODO(perf-vs-vice): wire up the native shim, run the matching workload,
/// and surface metrics in a shared report schema. Tracked under
/// MCP TODO PERF-BENCHMARK-001 remaining work.
/// </summary>
internal static class NativeViceBaseline
{
    /// <summary>
    /// Placeholder so consumers can reference the type from future BenchmarkDotNet
    /// attribute classes without compile errors. Always throws; will be replaced
    /// by the real shim invocation once the comparison slice lands.
    /// </summary>
    public static double EmulatedCyclesPerSecond()
    {
        throw new NotImplementedException(
            "Native VICE comparison is not implemented in this slice. See PERF-BENCHMARK-001.");
    }
}
