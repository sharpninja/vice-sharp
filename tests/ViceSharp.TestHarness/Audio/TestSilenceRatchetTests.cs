namespace ViceSharp.TestHarness.Audio;

using System;
using System.IO;
using System.Linq;
using Xunit;

/// <summary>
/// TR-QA-TESTSILENCE-001 ratchet (operator 2026-07-14: "I can STILL hear the unit
/// tests"). The per-class <see cref="WindowsAudioSessionMute"/> fixtures were attached
/// everywhere, yet the box still played sound: xUnit runs test classes in PARALLEL, so
/// a fast audio class disposing its fixture restored the pre-engage (unmuted) session
/// state while a long device test (the 3-second WinMM throughput diagnostic) was still
/// playing. The fix is an ASSEMBLY-level mute spanning the entire test run; the class
/// fixtures remain as harmless nesting for single-class runs.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class TestSilenceRatchetTests
{
    [Fact]
    public void EveryRealAudioDeviceTestClass_AttachesTheMuteFixture()
    {
        var testRoot = Path.Combine(RepoRoot, "tests", "ViceSharp.TestHarness");

        var offenders = Directory.EnumerateFiles(testRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("TestSilenceRatchetTests.cs", StringComparison.OrdinalIgnoreCase))
            .Select(f => (Path: f, Source: File.ReadAllText(f)))
            .Where(f => f.Source.Contains("new WinMmAudioBackend()", StringComparison.Ordinal)
                        || f.Source.Contains("new XAudio2SourceVoiceBackend()", StringComparison.Ordinal))
            .Where(f => !f.Source.Contains("WindowsAudioSessionMute", StringComparison.Ordinal))
            .Select(f => Path.GetFileName(f.Path))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Real-audio-device test classes without the WindowsAudioSessionMute fixture "
            + $"(TR-QA-TESTSILENCE-001): {string.Join(", ", offenders)}");
    }

    [Fact]
    public void TheWholeAssemblyRun_IsMuted_NotJustAudioClasses()
    {
        // The assembly fixture must be registered (it spans every parallel class, so a
        // class-fixture dispose can never unmute a still-running device test).
        var attribute = typeof(TestSilenceRatchetTests).Assembly
            .GetCustomAttributes(typeof(AssemblyFixtureAttribute), inherit: false)
            .Cast<AssemblyFixtureAttribute>()
            .SingleOrDefault(a => a.AssemblyFixtureType == typeof(AssemblyAudioSilence));

        Assert.NotNull(attribute);

        // Executable receipt on a Windows box with a render endpoint: the process
        // session is ACTUALLY muted while tests run.
        if (OperatingSystem.IsWindows()
            && WindowsAudioSession.TryReadProcessMute(out var muted))
        {
            Assert.True(muted, "The test process's audio session must be muted for the whole run.");
        }
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
