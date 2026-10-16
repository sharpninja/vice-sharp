namespace ViceSharp.TestHarness;

using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Core.Media;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-CFG-003 acceptance tests for palette selection. Iteration 2.
/// Named palette ids exist. Palette files, frame boundaries, and capture
/// metadata do not.
/// </summary>
public sealed class FrCfg003AcceptanceTests
{
    /// <summary>
    /// FR: FR-CFG-003.
    /// Use case: A palette file can be discovered and selected for the
    /// current machine.
    /// Acceptance: Apply sends the discovered file id, not the generic
    /// "vice" fallback.
    /// </summary>
    [Fact]
    public async Task PaletteFile_CanBeDiscoveredAndSelected()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-palette-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "custom.vpl");
        var lines = new string[16];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = "0 0 0 0";
        await File.WriteAllLinesAsync(file, lines, TestContext.Current.CancellationToken);
        try
        {
            UpdateSettingsRequest? sent = null;
            var host = Host(request => sent = request);
            var vm = new AttachPanelViewModel(host);
            var discover = vm.GetType().GetMethod("DiscoverPalettes");
            Assert.NotNull(discover);
            discover!.Invoke(vm, [directory]);
            vm.SelectedPalette = file;

            await vm.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);

            Assert.NotNull(sent);
            Assert.Equal("custom", sent!.Display!.Palette);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// FR: FR-CFG-003.
    /// Use case: An invalid palette file reports a diagnostic and leaves
    /// the active palette in place.
    /// Acceptance: After apply of a missing file, the sent palette id is
    /// still pepto and the status text reports the failure.
    /// </summary>
    [Fact]
    public async Task InvalidPaletteFile_DoesNotReplaceTheActivePalette()
    {
        UpdateSettingsRequest? sent = null;
        var host = Host(request => sent = request);
        var vm = new AttachPanelViewModel(host)
        {
            SelectedPalette = "Pepto"
        };
        await vm.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);
        Assert.Equal("pepto", sent!.Display!.Palette);

        vm.SelectedPalette = Path.Combine(Path.GetTempPath(), "missing.vpl");
        await vm.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);

        Assert.Equal("pepto", sent!.Display!.Palette);
        Assert.Contains("palette", vm.SettingsStatusText, StringComparison.OrdinalIgnoreCase);

        await AssertPaletteRejected(vm, sent, 1);
        await AssertPaletteRejected(vm, sent, 17);

        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-bad-palette-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var malformed = Path.Combine(directory, "broken.vpl");
        await File.WriteAllTextAsync(malformed, "this is not a palette", TestContext.Current.CancellationToken);
        try
        {
            vm.SelectedPalette = malformed;
            await vm.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);

            Assert.Equal("pepto", sent.Display.Palette);
            Assert.Contains("palette", vm.SettingsStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// FR: FR-CFG-003.
    /// Use case: A palette change applies on a frame boundary rather than
    /// mid-frame.
    /// Acceptance: The host apply request says the palette waits for the
    /// next frame.
    /// </summary>
    [Fact]
    public async Task PaletteChange_AppliesAtAFrameBoundary()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var session = new ViceSharp.Host.Runtime.EmulatorRuntimeSession("palette", machine.Architecture, machine);
        var registry = new ViceSharp.Host.Runtime.EmulatorRuntimeRegistry();
        registry.Add(session);
        var host = new ViceSharp.Host.Services.SettingsServiceHost(registry);
        var response = await host.UpdateSettingsAsync(
            new UpdateSettingsRequest(session.SessionId, Display: new DisplaySettingsDto(Palette: "pepto")),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotEqual("pepto", session.DisplaySettings.Palette);
        Assert.False(session.DisplayAppliedAtFrameBoundary);
        session.ApplyFrameBoundarySettings();
        Assert.Equal("pepto", session.DisplaySettings.Palette);
        Assert.True(session.DisplayAppliedAtFrameBoundary);
    }

    /// <summary>
    /// FR: FR-CFG-003.
    /// Use case: A captured frame carries enough palette metadata to
    /// reproduce the colors.
    /// Acceptance: A ViceSharp.Core.Media frame type exposes a palette id.
    /// </summary>
    [Fact]
    public void CapturedFrames_IncludePaletteMetadata()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-frame-meta-" + Guid.NewGuid().ToString("N"));
        using var capture = new FrameSequenceCapture(directory);
        try
        {
            capture.PaletteId = "custom";
            capture.CaptureFrame(new byte[8 * 8 * 4], 8, 8);

            Assert.Equal("custom", capture.LastCapturedFrame.PaletteId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// FR: FR-CFG-003.
    /// Use case: A selected palette file changes the colors the VIC-II draws.
    /// Acceptance: A 16-color file whose first entry is FF0000 is carried on
    /// the settings request and, after a frame-boundary apply, the rendered
    /// border pixel is that red. A named palette sends no color payload.
    /// </summary>
    [Fact]
    public async Task PaletteFile_ColorsReachTheRenderer()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-rgb-palette-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "redblack.vpl");
        var lines = new string[16];
        lines[0] = "FF 00 00";
        for (var i = 1; i < lines.Length; i++)
            lines[i] = "00 00 00";
        await File.WriteAllLinesAsync(file, lines, TestContext.Current.CancellationToken);
        try
        {
            UpdateSettingsRequest? sent = null;
            var vm = new AttachPanelViewModel(Host(request => sent = request));
            vm.SelectedPalette = "Pepto";
            await vm.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);
            Assert.NotNull(sent);
            Assert.Equal("", sent!.Display!.PaletteRgb);

            vm.SelectedPalette = file;
            await vm.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);
            Assert.Equal(96, sent.Display!.PaletteRgb.Length);
            Assert.StartsWith("ff0000", sent.Display.PaletteRgb, StringComparison.OrdinalIgnoreCase);

            var machine = MachineTestFactory.CreateC64Machine();
            var session = new EmulatorRuntimeSession("palette-rgb", machine.Architecture, machine);
            session.StageDisplaySettings(sent.Display);
            session.ApplyFrameBoundarySettings();
            machine.Bus.Write(0xD020, 0x00);
            machine.Bus.Write(0xD021, 0x00);
            machine.Clock.Step(312 * 63);
            var video = Assert.IsAssignableFrom<IVideoChip>(machine.Devices.GetByRole(DeviceRole.VideoChip));
            var frame = video.FrameBuffer;
            Assert.Equal(255, frame[3]);
            Assert.Equal(255, frame[2]);
            Assert.Equal(0, frame[1]);
            Assert.Equal(0, frame[0]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task AssertPaletteRejected(AttachPanelViewModel vm, UpdateSettingsRequest? sent, int colorRows)
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-count-palette-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "count.vpl");
        var lines = new string[colorRows];
        for (var i = 0; i < colorRows; i++)
            lines[i] = "00 00 00";
        await File.WriteAllLinesAsync(file, lines, TestContext.Current.CancellationToken);
        try
        {
            vm.SelectedPalette = file;
            await vm.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);
            Assert.Equal("pepto", sent!.Display!.Palette);
            Assert.Contains("palette", vm.SettingsStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static IHostProtocolClient Host(Action<UpdateSettingsRequest> capture)
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns("palette-session");
        host.UpdateSettingsAsync(Arg.Any<UpdateSettingsRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<UpdateSettingsRequest>();
                capture(request);
                return new UpdateSettingsResponse(RpcStatus.Ok(), null, []);
            });
        return host;
    }
}
