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
    /// <summary>Creates the view, its video surface, and the letterbox performance HUD.</summary>
    public EmulatorView()
    {
        InitializeComponent();

        // FEAT-XPERFHUD-001: the surface raises pre-formatted HUD text (~2 Hz) on the
        // dispatcher thread that owns this view; display it in the left letterbox bar.
        VideoSurface.StatsTextUpdated += text => PerfStats.Text = text;
    }

    /// <summary>The video surface hosting the ~50 Hz Direct3D 11 frame pull.</summary>
    public VideoSurfaceHost SurfaceHost => VideoSurface;

    /// <summary>
    /// Shows or hides the letterbox performance HUD (FEAT-XPERFHUD-001 toggle). The stats
    /// keep aggregating either way; only the display collapses.
    /// </summary>
    /// <param name="visible"><c>true</c> to show the HUD text.</param>
    public void SetPerfStatsVisible(bool visible)
        => PerfStats.Visibility = visible
            ? Windows.UI.Xaml.Visibility.Visible
            : Windows.UI.Xaml.Visibility.Collapsed;
}
#endif
