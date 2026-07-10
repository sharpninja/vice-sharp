using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// Convention ratchet: every xUnit test class that drives the native VICE
/// bridge (reSID/x64sc oracle) must join the <c>NativeVice</c> collection so
/// xUnit serializes it. Native VICE is a process-global, non-reentrant shim;
/// two native test classes running in parallel corrupt each other's oracle
/// state. The assembly currently disables all parallelization, but that is a
/// blunt safety net: this convention makes the requirement explicit and
/// survives any future re-enable of parallel collections.
///
/// Exemptions are structural, not a filename allow-list:
///   - non-test-class native users (shared helpers like LockstepValidator, the
///     ViceMachineValidationFixture) carry no test-method attribute, so the
///     "is a test class" gate skips them;
///   - files whose only ViceFact/ViceTheory occurrences are inside doc comments
///     or regex string literals (XmlDocsConventionTests) are not matched,
///     because the native-attribute probe only accepts a real attribute at the
///     start of a line.
/// </summary>
public sealed class NativeCollectionConventionTests
{
    // A real [ViceFact]/[ViceTheory] attribute at the start of a line (not a
    // doc-comment mention or a string literal).
    private static readonly Regex NativeAttribute = new(
        @"^[ \t]*\[\s*(?:ViceFact|ViceTheory)(?:Attribute)?\b",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // Any xUnit test-method attribute at the start of a line (proves the file is
    // an executable test class rather than a helper or fixture).
    private static readonly Regex TestMethodAttribute = new(
        @"^[ \t]*\[\s*(?:Fact|Theory|ViceFact|ViceTheory)(?:Attribute)?\b",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // Direct native-bridge entry points. A file that instantiates the native
    // machine any other way is out of scope for this convention.
    private static readonly string[] NativeBridgeMarkers =
    {
        "ViceNative.CreateInstance",
        "ViceNativeBridge.CreateMachine",
    };

    /// <summary>
    /// FR: FR-QA-NATIVE-COLLECTION, TR: TR-QA-NATIVE-COLLECTION-CONVENTION.
    /// Use case: continuous-integration guard that every native-bridge test
    /// class is serialized under the NativeVice collection, so a future
    /// re-enable of parallel collections cannot race the process-global oracle.
    /// Acceptance: no test class in the harness drives the native VICE bridge
    /// without declaring [Collection("NativeVice")].
    /// </summary>
    [Fact]
    public void NativeBridgeTestClasses_DeclareNativeViceCollection()
    {
        var sourceDirectory = ResolveSourceDirectory();
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (normalized.Contains("/bin/", StringComparison.Ordinal)
                || normalized.Contains("/obj/", StringComparison.Ordinal))
                continue;

            // The convention test cites the markers in its own source; skip it.
            if (Path.GetFileName(file).Equals(
                    typeof(NativeCollectionConventionTests).Name + ".cs",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            var contents = File.ReadAllText(file);

            var usesNative =
                NativeAttribute.IsMatch(contents)
                || NativeBridgeMarkers.Any(m => contents.Contains(m, StringComparison.Ordinal));
            if (!usesNative)
                continue;

            if (!TestMethodAttribute.IsMatch(contents))
                continue; // helper / fixture, not an executable test class.

            if (!contents.Contains("Collection(\"NativeVice\")", StringComparison.Ordinal))
                violations.Add(Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/'));
        }

        violations.Sort(StringComparer.Ordinal);
        Assert.True(
            violations.Count == 0,
            "Native-bridge test classes missing [Collection(\"NativeVice\")]:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations.Select(v => "  - " + v)));
    }

    private static string ResolveSourceDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ViceSharp.TestHarness.csproj")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Unable to resolve ViceSharp.TestHarness source directory for the native-collection scan.");
    }
}
