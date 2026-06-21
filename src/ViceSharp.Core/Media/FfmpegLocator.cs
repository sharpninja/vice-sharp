namespace ViceSharp.Core.Media;

using System;
using System.IO;

/// <summary>
/// FR-MED-004: locate the external <c>ffmpeg</c> executable used for muxed
/// video+audio capture. Resolution order: the <c>VICESHARP_FFMPEG</c> override
/// (full path to the binary), then each directory on <c>PATH</c>. Returns null
/// when ffmpeg is not installed, in which case the capture surface advertises no
/// muxed video formats and StartCapture(Video, mp4/...) is rejected gracefully.
/// </summary>
public static class FfmpegLocator
{
    /// <summary>Environment variable holding an explicit path to the ffmpeg binary.</summary>
    public const string OverrideEnvironmentVariable = "VICESHARP_FFMPEG";

    /// <summary>
    /// Resolve the ffmpeg executable path, or null when it cannot be found.
    /// </summary>
    public static string? Locate()
    {
        var overridePath = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            return overridePath;

        var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
            return null;

        foreach (var rawDir in pathVar.Split(Path.PathSeparator))
        {
            var dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrEmpty(dir))
                continue;

            string candidate;
            try
            {
                candidate = Path.Combine(dir, exeName);
            }
            catch (ArgumentException)
            {
                // A malformed PATH entry (invalid path chars) - skip it.
                continue;
            }

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>True when an ffmpeg executable can be located.</summary>
    public static bool IsAvailable => Locate() is not null;
}
