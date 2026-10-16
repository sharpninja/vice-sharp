namespace ViceSharp.TestHarness.Audio;

using System.Reflection;
using System.Runtime.CompilerServices;
using ViceSharp.Abstractions;
using ViceSharp.Core.Media;
using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// FR-SID-013 / TR-PLAT-001 / TEST-SID-001: unavailable playback must use vsync pacing.
/// Tests only WinMM lifecycle state; no Windows API or physical playback is invoked.
/// </summary>
public sealed class WinMmAvailabilityTests
{
    /// <summary>Initialization failure and disposal remove timing availability; ordinary pause does not.</summary>
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, true, false)]
    public void Availability_ReflectsDisabledAndDisposedState(bool disabled, bool disposed, bool paused, bool expected)
    {
        var backend = CreateNativeFreeBackend(disabled, disposed, paused);
        Assert.Equal(expected, backend.IsAvailable);
        Assert.Equal(expected, new CaptureAudioTap(backend).IsAvailable);
    }

    /// <summary>Actual Dispose invalidates an available backend and remains safe on repeated calls.</summary>
    [Fact]
    public void Dispose_RevokesTimingAvailability()
    {
        var backend = CreateNativeFreeBackend(false, false, false);
        Assert.True(backend.IsAvailable);
        ((IDisposable)backend).Dispose();
        Assert.False(backend.IsAvailable);
        ((IDisposable)backend).Dispose();
        Assert.False(backend.IsAvailable);
    }

    // Seed only existing managed state so this test executes on Linux and Windows without
    // opening waveOut. Zero native handles make the real Dispose path native-free too.
    private static IAudioBackend CreateNativeFreeBackend(bool disabled, bool disposed, bool paused)
    {
#pragma warning disable CA1416 // Native constructor and APIs are deliberately bypassed.
        var type = typeof(WinMmAudioBackend);
#pragma warning restore CA1416
        var backend = RuntimeHelpers.GetUninitializedObject(type);
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        type.GetField("_lock", fields)!.SetValue(backend, new object());
        type.GetField("_disabled", fields)!.SetValue(backend, disabled);
        type.GetField("_disposed", fields)!.SetValue(backend, disposed);
        type.GetField("_paused", fields)!.SetValue(backend, paused);
        return (IAudioBackend)backend;
    }
}
