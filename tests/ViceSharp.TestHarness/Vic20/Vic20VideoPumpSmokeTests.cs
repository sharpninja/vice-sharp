namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Smoke: in-process host + pump must produce usable VIC-20 frames (Xbox black-screen).
/// </summary>
public sealed class Vic20VideoPumpSmokeTests
{
    [Fact]
    public async Task StartVic20Session_Pump_CommitsFullGeometryFrame_Within3Seconds()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = ConsoleHostComposition.BuildDefault();
        var result = host.StartC64Session(new ConsoleSessionOptions("vic20"));
        Assert.True(result.Success, result.Error);

        var sawFullFrame = false;
        var width = 0;
        var height = 0;
        long cycle = 0;
        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(50, ct);
            var dest = new byte[1024 * 1024 * 4];
            if (!host.TryCopyLatestFrame(result.SessionId, dest, out width, out height, out cycle))
                continue;

            // VICE normal PAL: 224*2 x (311-28+1)
            if (width == 224 * 2 && height == 311 - 28 + 1)
            {
                sawFullFrame = true;
                break;
            }
        }

        var status = await host.HostService.GetStatusAsync(new SessionRequest(result.SessionId), ct);
        Assert.True(
            sawFullFrame,
            $"No VICE PAL geometry frame in 3s. Last={width}x{height}@{cycle} RunState={status.EmulatorStatus?.RunState} Cycle={status.EmulatorStatus?.Cycle} Frames={status.EmulatorStatus?.FrameCount}");
        Assert.Equal(448, width);
        Assert.Equal(284, height);
    }

    [Fact]
    public async Task StartVic20NtscSession_Pump_CommitsViceNormalNtscGeometry()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = ConsoleHostComposition.BuildDefault();
        var result = host.StartC64Session(new ConsoleSessionOptions("vic20ntsc"));
        Assert.True(result.Success, result.Error);

        var width = 0;
        var height = 0;
        var saw = false;
        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(50, ct);
            var dest = new byte[1024 * 1024 * 4];
            if (!host.TryCopyLatestFrame(result.SessionId, dest, out width, out height, out _))
                continue;
            // VICE normal NTSC: 200*2 x (261-28+1)
            if (width == 400 && height == 234)
            {
                saw = true;
                break;
            }
        }

        Assert.True(saw, $"No VICE NTSC geometry. Last={width}x{height}");
        Assert.True(host.TryGetVicIPixelAspect(result.SessionId, out var aspect));
        Assert.Equal(ViceSharp.Chips.Vic.Mos6561.GetPixelAspectRatio(ntsc: true), aspect, 5);
    }

    [Fact]
    public void Mos6561_RenderWithZeroRegs_KeepsDefaultGeometry()
    {
        var vic = new ViceSharp.Chips.Vic.Mos6561();
        vic.ConfigureTiming(71, 312, 284, columns: 22, rows: 23);
        // Power-on zeros (no KERNAL program yet).
        vic.RenderNow();
        // VICE normal PAL borders: 224 * VIC_PIXEL_WIDTH, 311-28+1 lines
        Assert.Equal(224 * ViceSharp.Chips.Vic.Mos6561.PixelWidth, vic.FrameWidth);
        Assert.Equal(284, vic.FrameHeight);
    }
}
