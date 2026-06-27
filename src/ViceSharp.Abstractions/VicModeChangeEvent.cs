namespace ViceSharp.Abstractions;

/// <summary>
/// Published by the VIC-II whenever a write actually CHANGES one of the display-mode / border
/// registers - $D011 (BMM/ECM/DEN/RSEL/YSCROLL), $D016 (MCM/CSEL/XSCROLL), $D018 (memory base),
/// $D020 (border colour) or $D021 (background colour) - i.e. the stored value differs from the
/// prior one. Diagnostic seam: a host can trap *who* flips the VIC out of an expected mode (for
/// example a stray char-mode or blue-border write during a frame the host renders as multicolour
/// bitmap). Publish is synchronous from <c>Write</c>, so a subscriber can snapshot the live
/// CPU/machine state at the exact instant of the change. Flag fields are 0/1 bytes so the payload
/// stays blittable for the lock-free pub/sub transport.
/// </summary>
public readonly record struct VicModeChangeEvent(
    byte Register,
    byte OldValue,
    byte NewValue,
    int RasterLine,
    byte BitmapMode,
    byte MulticolorMode,
    byte DisplayEnabled)
{
    /// <summary>
    /// Pub/Sub topic used for VIC display-mode / border change notifications.
    /// </summary>
    public static readonly Topic Topic = Topic.FromName("vic.mode-change");
}
