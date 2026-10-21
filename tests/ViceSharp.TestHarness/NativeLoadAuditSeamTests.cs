using System.Reflection;
using ViceSharp.Core;
using Xunit;
using Xunit.Sdk;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 load detector, seam level (round 5 N17; round 4 N11). With no sink installed the
/// audit seam allocates nothing and calls nothing across warmed repeated calls; a deliberately allocating or logging
/// double substituted at the same seam is detected by the same measurement; the file sink writes exactly one line
/// per attempt in the documented format and fails loudly on an unwritable file; and the per-cycle step paths of
/// both bindings never reference the audit (source-level control). The real native step measurement is not
/// executable on either host: no shim library exists after R2 A, which the D2 receipt states.
/// </summary>
[Collection("NativeLoadAudit")]
public sealed class NativeLoadAuditSeamTests
{
    private sealed class CountingSink : NativeLoadAudit.ISink
    {
        public int Calls;
        public void Record(string library, string? path, string outcome) => Calls++;
    }

    private sealed class AllocatingSink : NativeLoadAudit.ISink
    {
        public readonly List<string> Lines = new();
        public void Record(string library, string? path, string outcome) => Lines.Add(NativeLoadAudit.FormatLine(library, path, outcome));
    }

    private static long MeasureAllocations(int calls)
    {
        for (var i = 0; i < 1_000; i++)
            NativeLoadAudit.Record("vice_x64", null, NativeLoadAudit.Unresolved);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < calls; i++)
            NativeLoadAudit.Record("vice_x64", null, NativeLoadAudit.Unresolved);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void AuditUnset_WarmedRepeatedCalls_AllocateNothingAndCallNothing()
    {
        var previous = NativeLoadAudit.Replace(null);
        try
        {
            Assert.False(NativeLoadAudit.IsEnabled);
            Assert.Equal(0L, MeasureAllocations(100_000));
            var counting = new CountingSink();
            NativeLoadAudit.Replace(counting);
            Assert.Equal(0, counting.Calls);
        }
        finally
        {
            NativeLoadAudit.Replace(previous);
        }
    }

    [Fact]
    public void AllocatingDouble_IsDetectedByTheSameMeasurement()
    {
        var previous = NativeLoadAudit.Replace(new AllocatingSink());
        try
        {
            var delta = MeasureAllocations(10_000);
            Assert.NotEqual(0L, delta);
            Assert.Throws<EqualException>(() => Assert.Equal(0L, delta));
        }
        finally
        {
            NativeLoadAudit.Replace(previous);
        }
    }

    [Fact]
    public void LoggingDouble_ReceivesEveryCall()
    {
        var counting = new CountingSink();
        var previous = NativeLoadAudit.Replace(counting);
        try
        {
            Assert.True(NativeLoadAudit.IsEnabled);
            for (var i = 0; i < 123; i++)
                NativeLoadAudit.Record("vice_xvic", "/x/libvice_xvic.so", NativeLoadAudit.Loaded);
            Assert.Equal(123, counting.Calls);
        }
        finally
        {
            NativeLoadAudit.Replace(previous);
        }
    }

    [Fact]
    public void FileSink_AppendsOneLinePerAttemptInTheDocumentedFormat()
    {
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-load-audit-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            var sink = new NativeLoadAudit.FileSink(path);
            Assert.True(File.Exists(path));
            Assert.Equal(0L, new FileInfo(path).Length);
            sink.Record("vice_x64", "/p/libvice_x64.so", NativeLoadAudit.Loaded);
            sink.Record("vice_xvic", null, NativeLoadAudit.Fallback);
            var lines = File.ReadAllLines(path);
            Assert.Equal(["vice_x64,/p/libvice_x64.so,loaded", "vice_xvic,none,fallback"], lines);
            Assert.Equal("vice_x64,none,unresolved\n", NativeLoadAudit.FormatLine("vice_x64", null, NativeLoadAudit.Unresolved));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FileSink_UnwritablePath_FailsLoudly()
    {
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-missing-" + Guid.NewGuid().ToString("N"), "sub", "audit.log");
        var error = Assert.Throws<InvalidOperationException>(() => new NativeLoadAudit.FileSink(path));
        Assert.Contains(NativeLoadAudit.EnvironmentVariable, error.Message);
    }

    [Theory]
    [InlineData("ViceNative.cs", "ViceNativeInstance")]
    [InlineData("ViceNative.Xvic.cs", "Vic20NativeInstance")]
    public void InstanceStepPaths_NeverReferenceTheAudit(string file, string instanceClass)
    {
        var sourceDirectory = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "TestHarnessSourceDirectory").Value!;
        var path = Path.GetFullPath(Path.Combine(sourceDirectory, "..", "..", "src", "ViceSharp.Core", file));
        var text = File.ReadAllText(path);
        var start = text.IndexOf($"private sealed class {instanceClass} : IViceNative", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{instanceClass} not found in {file}");
        var body = text[start..];
        Assert.Contains("public void Step() => StepNative(_instance);", body);
        Assert.DoesNotContain("NativeLoadAudit", body);
        Assert.Contains("NativeLoadAudit.Record", text[..start]);
    }
}
