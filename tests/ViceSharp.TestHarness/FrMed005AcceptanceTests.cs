namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Core.Capture;
using ViceSharp.Core.Media;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-MED-005 acceptance tests for capture format selection. Iteration 2.
/// Muxed video formats exist. The named IMediaCapture format catalog does not.
/// </summary>
public sealed class FrMed005AcceptanceTests
{
    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: Video formats include MP4 H.264, AVI, and animated GIF.
    /// Acceptance: The supported catalog lists mp4, avi, and gif.
    /// </summary>
    [Fact]
    public void VideoFormats_IncludeMp4AviAndGif()
    {
        var ids = FormatIds("video");

        Assert.Contains("mp4", ids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("avi", ids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("gif", ids, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: Audio formats include WAV, FLAC, and MP3.
    /// Acceptance: The supported catalog lists wav, flac, and mp3.
    /// </summary>
    [Fact]
    public void AudioFormats_IncludeWavFlacAndMp3()
    {
        var ids = FormatIds("audio");

        Assert.Contains("wav", ids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("flac", ids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("mp3", ids, StringComparer.OrdinalIgnoreCase);
        Assert.True(MediaCapture.IsCatalogAudioFormat("flac"));
        Assert.True(MediaCapture.IsCatalogAudioFormat("mp3"));
        Assert.False(MediaCapture.IsCatalogAudioFormat("ogg"));
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: A FLAC or MP3 capture writes that format, not a WAV stand-in.
    /// Acceptance: After samples are submitted through the host, the FLAC file
    /// starts with fLaC and the MP3 file does not start with RIFF.
    /// </summary>
    [Fact]
    public async Task FlacAndMp3_AreEncodedRatherThanWav()
    {
        Assert.NotNull(FfmpegLocator.Locate());
        var flac = await RecordAudioAsync("flac");
        var mp3 = await RecordAudioAsync("mp3");

        Assert.Equal((byte)'f', flac[0]);
        Assert.Equal((byte)'L', flac[1]);
        Assert.Equal((byte)'a', flac[2]);
        Assert.Equal((byte)'C', flac[3]);
        Assert.False(mp3.Length >= 4 && mp3[0] == (byte)'R' && mp3[1] == (byte)'I' && mp3[2] == (byte)'F' && mp3[3] == (byte)'F');
        Assert.True((mp3.Length >= 2 && mp3[0] == 0xFF && (mp3[1] & 0xE0) == 0xE0) || StartsWithId3(mp3));
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: The requested audio format wins over the target extension.
    /// Acceptance: Format flac with a .wav target starts with fLaC after 100
    /// PCM samples. Format mp3 with a .wav target does not start with RIFF.
    /// Start and stop both return Ok.
    /// </summary>
    [Fact]
    public async Task RequestedAudioFormat_WinsOverWavExtension()
    {
        Assert.NotNull(FfmpegLocator.Locate());
        var flac = await RecordAudioAsync("flac", "wav");
        var mp3 = await RecordAudioAsync("mp3", "wav");

        Assert.Equal((byte)'f', flac[0]);
        Assert.Equal((byte)'L', flac[1]);
        Assert.Equal((byte)'a', flac[2]);
        Assert.Equal((byte)'C', flac[3]);
        Assert.False(mp3.Length >= 4
            && mp3[0] == (byte)'R'
            && mp3[1] == (byte)'I'
            && mp3[2] == (byte)'F'
            && mp3[3] == (byte)'F');
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: Image formats include PNG, BMP, and JPEG.
    /// Acceptance: The supported catalog lists png, bmp, and jpeg.
    /// </summary>
    [Fact]
    public void ImageFormats_IncludePngBmpAndJpeg()
    {
        var ids = FormatIds("image");

        Assert.Contains("png", ids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("bmp", ids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("jpeg", ids, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: A JPEG screenshot keeps the picture, not one frame-wide
    /// luminance sample.
    /// Acceptance: A split black/white block does not encode like a flat
    /// frame of that average, and a red frame does not encode like a gray
    /// frame of the same luminance.
    /// </summary>
    [Fact]
    public async Task Jpeg_EncodesSpatialDetailAndColor()
    {
        const int width = 8;
        const int height = 8;
        var split = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                var white = x >= width / 2;
                split[i] = white ? (byte)255 : (byte)0;
                split[i + 1] = white ? (byte)255 : (byte)0;
                split[i + 2] = white ? (byte)255 : (byte)0;
                split[i + 3] = 255;
            }
        }

        var flat = new byte[split.Length];
        for (var i = 0; i < flat.Length; i += 4)
        {
            flat[i] = 127;
            flat[i + 1] = 127;
            flat[i + 2] = 127;
            flat[i + 3] = 255;
        }

        var red = new byte[split.Length];
        var gray = new byte[split.Length];
        for (var i = 0; i < red.Length; i += 4)
        {
            red[i] = 0;
            red[i + 1] = 0;
            red[i + 2] = 255;
            red[i + 3] = 255;
            gray[i] = 76;
            gray[i + 1] = 76;
            gray[i + 2] = 76;
            gray[i + 3] = 255;
        }

        var splitBytes = await EncodeJpegAsync(split, width, height);
        var flatBytes = await EncodeJpegAsync(flat, width, height);
        var redBytes = await EncodeJpegAsync(red, width, height);
        var grayBytes = await EncodeJpegAsync(gray, width, height);

        Assert.Equal(0xFF, splitBytes[0]);
        Assert.Equal(0xD8, splitBytes[1]);
        Assert.NotEqual(flatBytes, splitBytes);
        Assert.NotEqual(grayBytes, redBytes);
    }

    private static async Task<byte[]> EncodeJpegAsync(byte[] bgra, int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-jpeg-" + Guid.NewGuid().ToString("N") + ".jpg");
        try
        {
            await JpegFrameArtifactWriter.WriteBgraAsync(bgra, width, height, path, TestContext.Current.CancellationToken);
            return await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: Each format exposes its quality or compression parameters.
    /// Acceptance: The MP4 format reports at least one parameter.
    /// </summary>
    [Fact]
    public void EachFormat_ExposesQualityParameters()
    {
        var parameters = Parameters("mp4");

        Assert.NotEmpty(parameters);
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: The advertised MP3 audioBitrate changes the encoding.
    /// Acceptance: The same samples encoded at 32k and at 320k are different
    /// files, and the 32k file is smaller. A fixed 128k pair is the miss. AC4.
    /// </summary>
    [Fact]
    public async Task Mp3Bitrate_ChangesTheEncoding()
    {
        Assert.NotNull(FfmpegLocator.Locate());
        var low = await RecordAudioAsync("mp3", "mp3", new Dictionary<string, string> { ["audioBitrate"] = "32k" });
        var high = await RecordAudioAsync("mp3", "mp3", new Dictionary<string, string> { ["audioBitrate"] = "320k" });

        Assert.NotEqual(low, high);
        Assert.True(low.Length + 100 < high.Length);
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: The advertised JPEG quality changes the screenshot.
    /// Acceptance: Host screenshots requested at quality 20 and quality 90
    /// both start with FF D8 and are not the same bytes. AC4.
    /// </summary>
    [Fact]
    public async Task JpegQuality_ChangesTheHostScreenshot()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        var session = new EmulatorRuntimeSession("media-jpeg-quality", machine.Architecture, machine);
        registry.Add(session);
        var capture = new CaptureServiceHost(registry);

        var low = await CaptureJpegAsync(capture, session.SessionId, 20);
        var high = await CaptureJpegAsync(capture, session.SessionId, 90);

        Assert.Equal(0xFF, low[0]);
        Assert.Equal(0xD8, low[1]);
        Assert.Equal(0xFF, high[0]);
        Assert.Equal(0xD8, high[1]);
        Assert.NotEqual(low, high);
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: IMediaCapture.GetSupportedFormats reports availability.
    /// Acceptance: The method exists and includes the mp4 id.
    /// </summary>
    [Fact]
    public void GetSupportedFormats_ReportsAvailability()
    {
        var ids = FormatIds("video");

        Assert.Contains("mp4", ids, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(FfmpegVideoFormats.Mp4.Id, ids, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: A missing ffmpeg executable reports the muxed formats as
    /// unsupported and does not throw.
    /// Acceptance: With the locator pointed at a missing file, mp4 is
    /// unsupported and the call returns.
    /// </summary>
    [Fact]
    public void MissingFfmpeg_ReportsFormatsUnsupported()
    {
        var previous = Environment.GetEnvironmentVariable("VICESHARP_FFMPEG");
        try
        {
            Environment.SetEnvironmentVariable("VICESHARP_FFMPEG", Path.Combine(Path.GetTempPath(), "missing-ffmpeg.exe"));
            var unsupported = Unsupported("mp4");
            var audio = FormatIds("audio");

            Assert.Contains("mp4", unsupported, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("flac", unsupported, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("mp3", unsupported, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("wav", audio, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("flac", audio, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("mp3", audio, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_FFMPEG", previous);
        }
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: Clients ask which formats can be recorded when ffmpeg
    /// cannot be located.
    /// Acceptance: With VICESHARP_FFMPEG naming a missing file and PATH
    /// empty, Locate is null. The host audio list is wav only, and
    /// GetSupportedFormats("video") omits mp4, avi, and gif. The host
    /// still offers bmpseq. AC5 and AC6. An audio-only unsupported list
    /// does not cover the host or the supported video catalog.
    /// </summary>
    [Fact]
    public async Task MissingFfmpeg_OmitsMuxedFormatsFromHostAndVideoCatalog()
    {
        var previousOverride = Environment.GetEnvironmentVariable("VICESHARP_FFMPEG");
        var previousPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable(
                "VICESHARP_FFMPEG",
                Path.Combine(Path.GetTempPath(), "missing-ffmpeg.exe"));
            Environment.SetEnvironmentVariable("PATH", "");

            Assert.Null(FfmpegLocator.Locate());

            var video = FormatIds("video");
            Assert.DoesNotContain("mp4", video, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("avi", video, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("gif", video, StringComparer.OrdinalIgnoreCase);

            var registry = new EmulatorRuntimeRegistry();
            var machine = MachineTestFactory.CreateC64Machine();
            var session = new EmulatorRuntimeSession("media-missing-ffmpeg", machine.Architecture, machine);
            registry.Add(session);
            var host = new CaptureServiceHost(registry);
            var caps = await host.GetCaptureCapabilitiesAsync(
                new SessionRequest(session.SessionId),
                TestContext.Current.CancellationToken);

            Assert.Contains("wav", caps.AudioFormats, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("flac", caps.AudioFormats, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("mp3", caps.AudioFormats, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(caps.VideoFormats, format => format.Id == "bmpseq");
            Assert.DoesNotContain(caps.VideoFormats, format =>
                format.Id.Equals("mp4", StringComparison.OrdinalIgnoreCase)
                || format.Id.Equals("avi", StringComparison.OrdinalIgnoreCase)
                || format.Id.Equals("gif", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_FFMPEG", previousOverride);
            Environment.SetEnvironmentVariable("PATH", previousPath);
        }
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: The default format for a capture type is configurable.
    /// Acceptance: SetDefaultFormat("video", "avi") makes avi the default.
    /// </summary>
    [Fact]
    public void DefaultFormat_IsConfigurable()
    {
        SetDefault("video", "avi");
        try
        {
            Assert.Equal("avi", DefaultOf("video"));
        }
        finally
        {
            ClearDefault("video");
        }
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: The capture host writes the JPEG and GIF formats it advertises.
    /// Acceptance: A host screenshot of jpeg starts with FF D8, and a host
    /// screenshot of gif starts with GIF89a.
    /// </summary>
    [Fact]
    public async Task JpegAndGif_AreWrittenByTheHost()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        var session = new EmulatorRuntimeSession("media-image", machine.Architecture, machine);
        registry.Add(session);
        var capture = new CaptureServiceHost(registry);

        var jpeg = await CaptureFrameAsync(capture, session.SessionId, "jpeg");
        var gif = await CaptureFrameAsync(capture, session.SessionId, "gif");

        Assert.True(jpeg.Length > 3);
        Assert.Equal(0xFF, jpeg[0]);
        Assert.Equal(0xD8, jpeg[1]);
        Assert.True(gif.Length > 6);
        Assert.Equal((byte)'G', gif[0]);
        Assert.Equal((byte)'I', gif[1]);
        Assert.Equal((byte)'F', gif[2]);
        Assert.Equal((byte)'8', gif[3]);
        Assert.Equal((byte)'9', gif[4]);
        Assert.Equal((byte)'a', gif[5]);
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: SetDefaultFormat selects the format the host writes when the
    /// caller does not name one.
    /// Acceptance: After SetDefaultFormat("image", "jpeg"), a screenshot
    /// request with an empty format starts with FF D8.
    /// </summary>
    [Fact]
    public async Task DefaultImageFormat_IsUsedByTheHost()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        var session = new EmulatorRuntimeSession("media-default", machine.Architecture, machine);
        registry.Add(session);
        var capture = new CaptureServiceHost(registry);
        SetDefault("image", "jpeg");
        try
        {
            var bytes = await CaptureFrameAsync(capture, session.SessionId, "");
            Assert.True(bytes.Length > 3);
            Assert.Equal(0xFF, bytes[0]);
            Assert.Equal(0xD8, bytes[1]);
        }
        finally
        {
            ClearDefault("image");
        }
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: Animated GIF is a video format the host can record.
    /// Acceptance: StartCapture of gif accepts the request when ffmpeg is
    /// present, and the stopped file starts with GIF89a.
    /// </summary>
    [Fact]
    public async Task GifVideo_IsEncodedRatherThanRejected()
    {
        Assert.NotNull(FfmpegLocator.Locate());
        var machine = MachineTestFactory.CreateC64Machine();
        var video = machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
        Assert.NotNull(video);
        var registry = new EmulatorRuntimeRegistry();
        var session = new EmulatorRuntimeSession("media-gif", machine.Architecture, machine);
        registry.Add(session);
        var capture = new CaptureServiceHost(registry);
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-gif-" + Guid.NewGuid().ToString("N") + ".gif");
        var start = await capture.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Video, path, "gif"),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);
        var frame = new byte[video!.FrameWidth * video.FrameHeight * 4];
        frame[2] = 255;
        session.RecordVideoFrameIfActive(frame, video.FrameWidth, video.FrameHeight);
        var stop = await capture.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture!.CaptureId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, stop.Status.Code);
        try
        {
            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.True(bytes.Length > 6);
            Assert.Equal((byte)'G', bytes[0]);
            Assert.Equal((byte)'I', bytes[1]);
            Assert.Equal((byte)'F', bytes[2]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// FR: FR-MED-005.
    /// Use case: A session that can record audio also records animated GIF.
    /// Acceptance: GIF is a video-only container. StartCapture still returns
    /// Ok and the file starts with GIF89a when an audio tap is installed.
    /// Mapping that tap into the GIF mux fails in ffmpeg.
    /// </summary>
    [Fact]
    public async Task GifVideo_WithAudioTap_IsEncodedWithoutAudio()
    {
        Assert.NotNull(FfmpegLocator.Locate());
        var machine = MachineTestFactory.CreateC64Machine();
        var video = machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
        Assert.NotNull(video);
        var registry = new EmulatorRuntimeRegistry();
        var session = new EmulatorRuntimeSession("media-gif-audio", machine.Architecture, machine);
        session.AudioCaptureTap = new CaptureAudioTap(downstream: null);
        registry.Add(session);
        var capture = new CaptureServiceHost(registry);
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-gif-audio-" + Guid.NewGuid().ToString("N") + ".gif");
        var start = await capture.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Video, path, "gif"),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);
        var frame = new byte[video!.FrameWidth * video.FrameHeight * 4];
        frame[2] = 255;
        session.RecordVideoFrameIfActive(frame, video.FrameWidth, video.FrameHeight);
        session.AudioCaptureTap.SubmitSamples(new float[32]);
        var stop = await capture.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture!.CaptureId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, stop.Status.Code);
        try
        {
            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.True(bytes.Length > 6);
            Assert.Equal((byte)'G', bytes[0]);
            Assert.Equal((byte)'I', bytes[1]);
            Assert.Equal((byte)'F', bytes[2]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static async Task<byte[]> CaptureJpegAsync(CaptureServiceHost capture, string sessionId, int quality)
    {
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-jpeg-q-" + Guid.NewGuid().ToString("N") + ".jpg");
        var response = await capture.CaptureFrameAsync(
            new CaptureFrameRequest(sessionId, path, "jpeg", quality),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        try
        {
            return await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static async Task<byte[]> CaptureFrameAsync(CaptureServiceHost capture, string sessionId, string format)
    {
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-img-" + Guid.NewGuid().ToString("N") + ".bin");
        var response = await capture.CaptureFrameAsync(
            new CaptureFrameRequest(sessionId, path, format),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        try
        {
            return await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static Task<byte[]> RecordAudioAsync(string format)
        => RecordAudioAsync(format, format, null);

    private static Task<byte[]> RecordAudioAsync(string format, string extension)
        => RecordAudioAsync(format, extension, null);

    private static async Task<byte[]> RecordAudioAsync(
        string format,
        string extension,
        IReadOnlyDictionary<string, string>? options)
    {
        var registry = new EmulatorRuntimeRegistry();
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        var session = factory.Create(new CreateEmulatorSessionRequest());
        session.AudioCaptureTap = new CaptureAudioTap(downstream: null);
        registry.Add(session);
        var capture = new CaptureServiceHost(registry);
        var path = Path.Combine(Path.GetTempPath(), "vicesharp-aud-" + Guid.NewGuid().ToString("N") + "." + extension);
        var start = await capture.StartCaptureAsync(
            new StartCaptureRequest(session.SessionId, CaptureKind.Audio, path, format, options),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, start.Status.Code);
        var samples = new float[100];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = 0.25f;
        session.AudioCaptureTap!.SubmitSamples(samples);
        var stop = await capture.StopCaptureAsync(
            new StopCaptureRequest(session.SessionId, start.Capture!.CaptureId),
            TestContext.Current.CancellationToken);
        Assert.Equal(RpcStatusCode.Ok, stop.Status.Code);
        try
        {
            return await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static bool StartsWithId3(byte[] bytes)
        => bytes.Length >= 3 && bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3';

    private static IReadOnlyList<string> FormatIds(string kind)
    {
        var method = CaptureMethod("GetSupportedFormats");
        if (method is null)
            return [];

        var target = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!);
        var value = method.Invoke(target, [kind]);
        return value as IReadOnlyList<string> ?? [];
    }

    private static IReadOnlyList<string> Parameters(string formatId)
    {
        var method = CaptureMethod("GetFormatParameters");
        if (method is null)
            return [];

        var target = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!);
        return method.Invoke(target, [formatId]) as IReadOnlyList<string> ?? [];
    }

    private static IReadOnlyList<string> Unsupported(string formatId)
    {
        var method = CaptureMethod("GetUnsupportedFormats");
        if (method is null)
            return [];

        var target = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!);
        return method.Invoke(target, [formatId]) as IReadOnlyList<string> ?? [];
    }

    private static void SetDefault(string kind, string formatId)
    {
        var method = CaptureMethod("SetDefaultFormat");
        Assert.NotNull(method);
        var target = method!.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!);
        method.Invoke(target, [kind, formatId]);
    }

    private static void ClearDefault(string kind)
    {
        var method = CaptureMethod("ClearDefaultFormat");
        Assert.NotNull(method);
        var target = method!.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!);
        method.Invoke(target, [kind]);
    }

    private static string DefaultOf(string kind)
    {
        var method = CaptureMethod("GetDefaultFormat");
        if (method is null)
            return string.Empty;

        var target = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!);
        return method.Invoke(target, [kind]) as string ?? string.Empty;
    }

    private static System.Reflection.MethodInfo? CaptureMethod(string name)
    {
        var type = Type.GetType("ViceSharp.Core.Media.MediaCapture, ViceSharp.Core", throwOnError: false);
        return type?.GetMethod(name);
    }
}
