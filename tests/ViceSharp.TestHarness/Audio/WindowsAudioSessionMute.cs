namespace ViceSharp.TestHarness.Audio;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Core Audio (WASAPI) helper that reads and toggles the mute of the current
/// process's default Windows audio session - the per-application slider you see
/// in the Windows Volume Mixer. It is driven entirely through raw
/// function-pointer vtable calls (no <c>[ComImport]</c>, no reflection,
/// no <c>Activator</c>) so it stays NativeAOT/trim clean under the repo-wide
/// analyzers, mirroring the shipping <c>WinMmAudioBackend</c>. Every entry point
/// degrades to a graceful no-op on a non-Windows or headless (no render
/// endpoint) host rather than throwing.
///
/// FR: TR-QA-TESTSILENCE-001 (audio-producing test classes self-mute the host
/// process's Windows audio session). This is test infrastructure, not product
/// behaviour: it changes nothing under test, it only silences the developer box.
/// </summary>
internal static unsafe partial class WindowsAudioSession
{
    private static readonly Guid ClsidMMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IidIMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid IidIAudioSessionManager = new("BFA971F1-4D5E-40BB-935E-967039BFBEE4");

    private const uint ClsCtxAll = 0x17;
    private const int ERender = 0;    // EDataFlow.eRender
    private const int EConsole = 0;   // ERole.eConsole
    private const uint CoinitMultithreaded = 0x0;

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint reserved, uint flags);

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);

    // IUnknown::Release is vtable slot 2 on every COM interface.
    private static void Release(nint p)
    {
        if (p != 0)
            _ = ((delegate* unmanaged<nint, uint>)(*(void***)p)[2])(p);
    }

    /// <summary>
    /// Acquires the current process's <c>ISimpleAudioVolume</c> (the default
    /// audio session). Returns 0 on any failure. On success the caller owns the
    /// returned COM pointer and MUST hand it back to <see cref="ReleaseVolume"/>.
    /// </summary>
    internal static nint TryAcquireProcessVolume()
    {
        if (!OperatingSystem.IsWindows())
            return 0;

        nint enumerator = 0, device = 0, manager = 0;
        try
        {
            // Best effort: COM may already be initialised on this thread in a
            // different apartment (RPC_E_CHANGED_MODE) - the MMDevice API works
            // regardless, so the return code is intentionally ignored.
            _ = CoInitializeEx(0, CoinitMultithreaded);

            if (CoCreateInstance(in ClsidMMDeviceEnumerator, 0, ClsCtxAll, in IidIMMDeviceEnumerator, out enumerator) < 0 || enumerator == 0)
                return 0;

            // IMMDeviceEnumerator::GetDefaultAudioEndpoint -> vtable slot 4.
            var getDefaultEndpoint = (delegate* unmanaged<nint, int, int, nint*, int>)(*(void***)enumerator)[4];
            nint dev;
            if (getDefaultEndpoint(enumerator, ERender, EConsole, &dev) < 0 || dev == 0)
                return 0;
            device = dev;

            // IMMDevice::Activate -> vtable slot 3.
            var activate = (delegate* unmanaged<nint, Guid*, uint, nint, nint*, int>)(*(void***)device)[3];
            Guid managerIid = IidIAudioSessionManager;
            nint mgr;
            if (activate(device, &managerIid, ClsCtxAll, 0, &mgr) < 0 || mgr == 0)
                return 0;
            manager = mgr;

            // IAudioSessionManager::GetSimpleAudioVolume -> vtable slot 4.
            // A null session GUID selects the process's default session.
            var getSimpleVolume = (delegate* unmanaged<nint, nint, int, nint*, int>)(*(void***)manager)[4];
            nint volume;
            if (getSimpleVolume(manager, 0, 0, &volume) < 0 || volume == 0)
                return 0;

            return volume; // ownership transferred to the caller; not released below
        }
        catch
        {
            return 0;
        }
        finally
        {
            Release(manager);
            Release(device);
            Release(enumerator);
        }
    }

    /// <summary>Sets the mute flag on a volume acquired from <see cref="TryAcquireProcessVolume"/>. Returns the HRESULT.</summary>
    internal static int SetMute(nint volume, bool mute)
    {
        // ISimpleAudioVolume::SetMute -> vtable slot 5.
        return ((delegate* unmanaged<nint, int, nint, int>)(*(void***)volume)[5])(volume, mute ? 1 : 0, 0);
    }

    /// <summary>Reads the mute flag from a volume pointer. Returns false when the call fails.</summary>
    internal static bool TryGetMute(nint volume, out bool muted)
    {
        // ISimpleAudioVolume::GetMute -> vtable slot 6.
        int value;
        var hr = ((delegate* unmanaged<nint, int*, int>)(*(void***)volume)[6])(volume, &value);
        muted = value != 0;
        return hr >= 0;
    }

    /// <summary>Releases a volume pointer acquired from <see cref="TryAcquireProcessVolume"/>.</summary>
    internal static void ReleaseVolume(nint volume) => Release(volume);

    /// <summary>
    /// Reads the current process's session mute via a fresh acquire+release.
    /// Returns false (and <paramref name="muted"/> = false) when no endpoint is
    /// available or the query fails.
    /// </summary>
    internal static bool TryReadProcessMute(out bool muted)
    {
        muted = false;
        var volume = TryAcquireProcessVolume();
        if (volume == 0)
            return false;

        try
        {
            return TryGetMute(volume, out muted);
        }
        finally
        {
            ReleaseVolume(volume);
        }
    }
}

/// <summary>
/// xUnit class fixture that mutes the test host process's Windows audio session
/// for the lifetime of an audio-producing test class and restores the prior
/// mute state on dispose. Attach with
/// <c>IClassFixture&lt;WindowsAudioSessionMute&gt;</c>; it coexists with any
/// <c>[Collection]</c> a class already belongs to. On a non-Windows or headless
/// host it engages nothing and never throws.
///
/// FR: TR-QA-TESTSILENCE-001. Use case: keep a developer box silent while the
/// SID/audio tests exercise the real <c>WinMmAudioBackend</c>. Acceptance:
/// engaged on Windows with a render endpoint, a no-op otherwise, and it never
/// leaves the process muted after the run.
/// </summary>
public sealed class WindowsAudioSessionMute : IDisposable
{
    private nint _volume;
    private readonly bool _previousMute;
    private bool _disposed;

    /// <summary>True when the process audio session was acquired and muted.</summary>
    public bool IsEngaged { get; }

    /// <summary>Acquires the process audio session and mutes it (best effort).</summary>
    public WindowsAudioSessionMute()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var volume = WindowsAudioSession.TryAcquireProcessVolume();
        if (volume == 0)
            return;

        if (!WindowsAudioSession.TryGetMute(volume, out _previousMute)
            || WindowsAudioSession.SetMute(volume, true) < 0)
        {
            WindowsAudioSession.ReleaseVolume(volume);
            return;
        }

        _volume = volume;
        IsEngaged = true;
    }

    /// <summary>Restores the pre-engage mute state and releases the session. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_volume == 0)
            return;

        _ = WindowsAudioSession.SetMute(_volume, _previousMute);
        WindowsAudioSession.ReleaseVolume(_volume);
        _volume = 0;
    }
}
