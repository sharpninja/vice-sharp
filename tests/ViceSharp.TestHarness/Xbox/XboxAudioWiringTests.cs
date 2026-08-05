namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// FIX-XNOAUDIO-001 (PLAN-XBOXUWP S38 surfaced by the S-Blox default cartridge).
/// Operator 2026-07-14: "No audio!" Two structural gaps made the UWP head silent BY
/// CONSTRUCTION: (1) the shared VICESHARP_AUDIO opt-in gate is set by the DESKTOP
/// head's Program.cs but never by the UWP App, so XboxAudioBackendFactory always
/// returned null; (2) even a created backend went nowhere: the head built its host via
/// ConsoleHostComposition.BuildDefault() which accepts no backend, so the SID was never
/// connected to a device.
/// </summary>
/// <remarks>
/// Acceptance:
///   TEST-XAUDIO-001a: ConsoleHostComposition exposes a BuildDefault overload that
///     carries an IAudioBackend into the default runtime factory.
///   TEST-XAUDIO-001b (structural): the App defaults VICESHARP_AUDIO to enabled
///     (explicit 0 still wins, mirroring the desktop Program.cs) and passes its
///     AudioBackend into BuildDefault; the boot log reports the audio presence.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxAudioWiringTests
{
    [Fact]
    public void ConsoleHostComposition_CarriesTheAudioBackend()
    {
        // TEST-XAUDIO-001a: the overload exists and accepts the backend.
        var method = typeof(ConsoleHostComposition).GetMethods()
            .SingleOrDefault(m =>
                m.Name == "BuildDefault"
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IAudioBackend));

        Assert.NotNull(method);

        // And it composes a host (a null backend stays valid: silent construction).
        var host = ConsoleHostComposition.BuildDefault((IAudioBackend?)null);
        Assert.NotNull(host);
    }

    [Fact]
    public void Head_EnablesTheAudioGate_AndWiresTheBackend()
    {
        // TEST-XAUDIO-001b.
        var app = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "App.xaml.cs"));

        Assert.Contains("VICESHARP_AUDIO", app);
        Assert.Contains("BuildDefault(AudioBackend)", app);
        Assert.Contains("audio={Audio", app);
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
