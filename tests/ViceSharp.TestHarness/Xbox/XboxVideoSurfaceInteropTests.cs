namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FIX-XRENDERCRASH-001 (PLAN-XBOXUWP, area XVIDEO): the UWP head's video surface
/// (src/ViceSharp.Xbox/Controls/VideoSurfaceHost.cs) must QueryInterface the
/// SYSTEM-XAML (UWP, WinUI2-era) ISwapChainPanelNative, never the WinUI 3 one.
///
/// FR: FR-XVIDEO-002 (the render surface binds the composition swap chain to the
/// XAML SwapChainPanel). Use case: the head is a Windows.UI.Xaml (UseUwp) app;
/// UWP does not support WinUI 3, and the two XAML stacks declare DIFFERENT
/// ISwapChainPanelNative interfaces with different IIDs:
///   - UWP    windows.ui.xaml.media.dxinterop.h   MIDL_INTERFACE("F92F19D2-3ADE-45A6-A20C-F6F1EA90554B")
///   - WinUI3 microsoft.ui.xaml.media.dxinterop.h MIDL_INTERFACE("63AAD0B8-7C24-40FF-85A8-640D944CC325")
/// QI'ing a Windows.UI.Xaml SwapChainPanel with the WinUI3 IID fails with
/// E_NOINTERFACE (0x80004002), which the deployed head reproduced verbatim in
/// LocalState\vicesharp.log ("EnsurePanelNative: TryAs(ISwapChainPanelNative)
/// hr=0x80004002 ptr=0x0"), leaving the emulator surface permanently black.
/// Acceptance:
///   TEST-XVIDEO-IID-001a: VideoSurfaceHost declares the UWP system-XAML IID
///     f92f19d2-3ade-45a6-a20c-f6f1ea90554b (verified against the installed
///     Windows SDK 10.0.26100 header, line 854).
///   TEST-XVIDEO-IID-001b: the WinUI3 IID 63aad0b8-7c24-40ff-85a8-640d944cc325
///     appears nowhere in the surface (the regression that shipped the black
///     screen).
/// </summary>
public sealed class XboxVideoSurfaceInteropTests
{
    private const string UwpSwapChainPanelNativeIid = "f92f19d2-3ade-45a6-a20c-f6f1ea90554b";
    private const string WinUi3SwapChainPanelNativeIid = "63aad0b8-7c24-40ff-85a8-640d944cc325";

    [Fact]
    [Trait("Category", "Xbox")]
    public void VideoSurface_QueriesUwpSwapChainPanelNative_NotTheWinUi3One()
    {
        var source = ReadVideoSurfaceHost();

        // TEST-XVIDEO-IID-001a: the UWP (windows.ui.xaml.media.dxinterop.h) IID.
        Assert.Contains(UwpSwapChainPanelNativeIid, source);

        // TEST-XVIDEO-IID-001b: the WinUI3 (microsoft.ui.xaml.media.dxinterop.h) IID
        // must be gone; a Windows.UI.Xaml panel answers it with E_NOINTERFACE.
        Assert.DoesNotContain(WinUi3SwapChainPanelNativeIid, source);
    }

    private static string ReadVideoSurfaceHost()
    {
        var path = Path.Combine(
            RepoRoot, "src", "ViceSharp.Xbox", "Controls", "VideoSurfaceHost.cs");

        Assert.True(File.Exists(path), $"Expected the UWP video surface at '{path}'.");

        // GUID literals in the surface are lowercase by convention; normalize so the
        // assertion cannot be dodged by a casing change.
        return File.ReadAllText(path).ToLowerInvariant();
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
