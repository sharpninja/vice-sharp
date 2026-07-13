// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): code-behind for the In-emulator base view.
// #if HAS_UWP-guarded in full so the fallback build never sees the UWP XAML partial.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Windows.UI.Xaml.Controls;
using ViceSharp.Xbox.Controls;

/// <summary>
/// The always-present base view hosting the C64 video surface. The shell reads
/// <see cref="SurfaceHost"/> to attach the video-pull adapter and start rendering.
/// </summary>
public sealed partial class EmulatorView : UserControl
{
    /// <summary>Creates the view and its video surface.</summary>
    public EmulatorView() => InitializeComponent();

    /// <summary>The video surface hosting the ~50 Hz Win2D frame pull.</summary>
    public VideoSurfaceHost SurfaceHost => VideoSurface;
}
#endif
