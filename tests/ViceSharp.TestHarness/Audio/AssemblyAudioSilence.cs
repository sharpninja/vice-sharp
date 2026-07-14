using Xunit;

[assembly: AssemblyFixture(typeof(ViceSharp.TestHarness.Audio.AssemblyAudioSilence))]

namespace ViceSharp.TestHarness.Audio;

using System;

/// <summary>
/// TR-QA-TESTSILENCE-001 (operator 2026-07-14: "I can STILL hear the unit tests").
/// ASSEMBLY-level audio silence: mutes the test host process's Windows audio session
/// once for the ENTIRE test run and restores the prior state when the run ends.
/// </summary>
/// <remarks>
/// The per-class <see cref="WindowsAudioSessionMute"/> fixtures are correct but raced
/// under xUnit's parallel class execution: a fast audio class disposing restored the
/// unmuted state while the 3-second WinMM throughput diagnostic was still playing.
/// This fixture brackets every class, so nested class fixtures always capture (and
/// restore) an already-muted state. Best-effort like the class fixture: a headless /
/// non-Windows host engages nothing and never throws.
/// </remarks>
public sealed class AssemblyAudioSilence : IDisposable
{
    private readonly WindowsAudioSessionMute _mute = new();

    /// <summary>True when the process session was acquired and muted for the run.</summary>
    public bool IsEngaged => _mute.IsEngaged;

    /// <summary>Restores the pre-run mute state. Idempotent.</summary>
    public void Dispose() => _mute.Dispose();
}
