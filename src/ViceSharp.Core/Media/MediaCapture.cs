namespace ViceSharp.Core.Media;

/// <summary>
/// FR-MED-005: catalog of capture formats, their quality parameters, and the
/// default format for each capture kind. Muxed video ids that need an external
/// ffmpeg executable are reported unsupported when that executable cannot be
/// found. This is a concrete type, not <c>IMediaCapture</c>, so callers can
/// share the static default-format state.
/// </summary>
public static class MediaCapture
{
    private static readonly object DefaultFormatGate = new();
    private static readonly Dictionary<string, string> DefaultFormats = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Formats the catalog knows for <paramref name="kind"/>
    /// (<c>video</c>, <c>audio</c>, or <c>image</c>).
    /// </summary>
    /// <param name="kind">Capture kind.</param>
    /// <returns>Format ids. Empty when <paramref name="kind"/> is unknown.</returns>
    /// <summary>
    /// FR-MED-005. Audio ids the capture host accepts: wav, flac, and mp3.
    /// An empty format means the wav default.
    /// </summary>
    public static bool IsCatalogAudioFormat(string format)
        => string.IsNullOrEmpty(format)
            || format.Equals("wav", StringComparison.OrdinalIgnoreCase)
            || format.Equals("flac", StringComparison.OrdinalIgnoreCase)
            || format.Equals("mp3", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> GetSupportedFormats(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return [];

        if (kind.Equals("video", StringComparison.OrdinalIgnoreCase))
            return FfmpegIsUsable() ? VideoFormats() : [];

        if (kind.Equals("audio", StringComparison.OrdinalIgnoreCase))
            return FfmpegIsUsable() ? ["wav", "flac", "mp3"] : ["wav"];

        if (kind.Equals("image", StringComparison.OrdinalIgnoreCase))
            return ["png", "bmp", "jpeg"];

        return [];
    }

    /// <summary>
    /// Quality or bitrate parameter names for <paramref name="formatId"/>.
    /// </summary>
    /// <param name="formatId">Format id such as <c>mp4</c>.</param>
    /// <returns>Parameter names. Empty when the format is unknown.</returns>
    public static IReadOnlyList<string> GetFormatParameters(string formatId)
    {
        if (string.IsNullOrWhiteSpace(formatId))
            return [];

        if (formatId.Equals("mp4", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals(FfmpegVideoFormats.Mp4.Id, StringComparison.OrdinalIgnoreCase)
            || formatId.Equals("mkv", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals("avi", StringComparison.OrdinalIgnoreCase))
        {
            // Same knobs FfmpegArgumentBuilder sends: video and audio bitrate.
            return ["videoBitrate", "audioBitrate"];
        }

        if (formatId.Equals("gif", StringComparison.OrdinalIgnoreCase))
            return ["frameDelay", "loopCount"];

        if (formatId.Equals("mp3", StringComparison.OrdinalIgnoreCase))
            return ["audioBitrate"];

        if (formatId.Equals("flac", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals("png", StringComparison.OrdinalIgnoreCase))
            return ["compressionLevel"];

        if (formatId.Equals("jpeg", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals("jpg", StringComparison.OrdinalIgnoreCase))
            return ["quality"];

        if (formatId.Equals("wav", StringComparison.OrdinalIgnoreCase))
            return ["sampleRate", "channels"];

        if (formatId.Equals("bmp", StringComparison.OrdinalIgnoreCase))
            return ["bitDepth"];

        return [];
    }

    /// <summary>
    /// Muxed format ids that cannot be encoded with the current ffmpeg locator.
    /// A <c>VICESHARP_FFMPEG</c> value that does not name an existing file
    /// reports the muxed ids (including <c>mp4</c>) and does not throw. When
    /// ffmpeg is on that override or on <c>PATH</c>, the list is empty.
    /// </summary>
    /// <param name="formatId">Format the caller is asking about.</param>
    /// <returns>Unsupported format ids. Never throws.</returns>
    public static IReadOnlyList<string> GetUnsupportedFormats(string formatId)
    {
        if (FfmpegIsUsable())
            return [];

        var unsupported = new List<string>
        {
            FfmpegVideoFormats.Mp4.Id,
            FfmpegVideoFormats.Avi.Id,
            FfmpegVideoFormats.Mkv.Id,
            "gif",
            "flac",
            "mp3"
        };

        if (!string.IsNullOrWhiteSpace(formatId)
            && !unsupported.Exists(id => string.Equals(id, formatId, StringComparison.OrdinalIgnoreCase))
            && IsMuxedFormat(formatId))
        {
            unsupported.Add(formatId);
        }

        return unsupported;
    }

    /// <summary>
    /// Remember the default format id for <paramref name="kind"/>. Shared across
    /// callers because the catalog is process-wide.
    /// </summary>
    /// <param name="kind">Capture kind (<c>video</c>, <c>audio</c>, or <c>image</c>).</param>
    /// <param name="formatId">Format id to use by default.</param>
    public static void SetDefaultFormat(string kind, string formatId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(formatId);
        lock (DefaultFormatGate)
            DefaultFormats[kind] = formatId;
    }

    /// <summary>
    /// Default format id for <paramref name="kind"/>, or the built-in default
    /// when none has been set.
    /// </summary>
    /// <param name="kind">Capture kind.</param>
    /// <returns>The configured format id, or empty when <paramref name="kind"/> is unknown.</returns>
    public static string GetDefaultFormat(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return "";

        lock (DefaultFormatGate)
        {
            if (DefaultFormats.TryGetValue(kind, out var formatId))
                return formatId;
        }

        if (kind.Equals("video", StringComparison.OrdinalIgnoreCase))
            return FfmpegVideoFormats.Mp4.Id;
        if (kind.Equals("audio", StringComparison.OrdinalIgnoreCase))
            return "wav";
        if (kind.Equals("image", StringComparison.OrdinalIgnoreCase))
            return "png";
        return "";
    }

    /// <summary>
    /// The default set by <see cref="SetDefaultFormat"/>, ignoring the built-in
    /// fallback. The capture host uses this so an unset kind keeps its
    /// historical format.
    /// </summary>
    public static bool TryGetExplicitDefault(string kind, out string formatId)
    {
        formatId = "";
        if (string.IsNullOrWhiteSpace(kind))
            return false;

        lock (DefaultFormatGate)
            return DefaultFormats.TryGetValue(kind, out formatId!);
    }

    /// <summary>
    /// Removes a default set by <see cref="SetDefaultFormat"/>.
    /// </summary>
    public static void ClearDefaultFormat(string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        lock (DefaultFormatGate)
            DefaultFormats.Remove(kind);
    }

    private static IReadOnlyList<string> VideoFormats()
    {
        var formats = new List<string> { "mp4", "avi", "gif" };
        var mp4 = FfmpegVideoFormats.Mp4.Id;
        if (!formats.Contains(mp4, StringComparer.OrdinalIgnoreCase))
            formats.Add(mp4);
        return formats;
    }

    private static bool IsMuxedFormat(string formatId)
    {
        return formatId.Equals("mp4", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals("avi", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals("mkv", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals("gif", StringComparison.OrdinalIgnoreCase)
            || formatId.Equals(FfmpegVideoFormats.Mp4.Id, StringComparison.OrdinalIgnoreCase)
            || formatId.Equals(FfmpegVideoFormats.Avi.Id, StringComparison.OrdinalIgnoreCase)
            || formatId.Equals(FfmpegVideoFormats.Mkv.Id, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when ffmpeg can be used. An explicit <c>VICESHARP_FFMPEG</c> path
    /// is authoritative: a missing file means ffmpeg is unavailable and PATH
    /// is not consulted. Otherwise <see cref="FfmpegLocator.Locate"/> checks PATH.
    /// </summary>
    private static bool FfmpegIsUsable() => FfmpegLocator.IsUsable;
}
