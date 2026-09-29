namespace ViceSharp.TestHarness.Diagnostics;

using System.Globalization;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Architectures.C64;
using ViceSharp.Core.Capture;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Diagnostic dump: autostart wolf64.d64, capture a ViceSharp frame for
/// comparison against x64sc. Not a pixel-equality gate.
/// </summary>
public sealed class Wolf64FrameDumpTests
{
    private const string DiskPath = @"F:\GitHub\vice-sharp\validation-output\wolf64-frame-diff-20260915\wolf64.d64";

    [Fact]
    public async Task DumpAutostartFrame_WritesBmpAndVicRegs()
    {
        if (!File.Exists(DiskPath))
            Assert.Skip($"missing {DiskPath}");
        var outDir = Path.Combine(
            FindRepoRoot(),
            "validation-output",
            "wolf64-frame-diff-20260915");
        Directory.CreateDirectory(outDir);
        var bmpPath = Path.Combine(outDir, "vicesharp-wolf64.bmp");
        var txtPath = Path.Combine(outDir, "vicesharp-wolf64-regs.txt");
        var ramPath = Path.Combine(outDir, "vicesharp-ram.bin");

        var profile = C64MachineProfiles.C64Pal;
        var builder = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider());
        var factory = new DefaultEmulatorRuntimeFactory(builder, [new C64Descriptor(profile)], profile.Id);
        var registry = new EmulatorRuntimeRegistry();
        var media = new MediaServiceHost(registry);
        var session = factory.Create(new CreateEmulatorSessionRequest(profile.Id, TrueDrive: false));
        registry.Add(session);

        var attach = await media.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, DiskPath),
            TestContext.Current.CancellationToken);
        Assert.True(attach.Status.IsSuccess, attach.Status.Message);

        var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();
        var keyboard = session.Machine.Devices.All.OfType<IMachineKeyboardInput>().FirstOrDefault();
        const int frames = 2800;
        for (var frame = 0; frame < frames; frame++)
        {
            session.Machine.RunFrame();
            automation.AdvanceFrame(session.Machine);
            if (frame is 900 or 1200 or 1500)
                keyboard?.SetKeyState("Return", true);
            if (frame is 920 or 1220 or 1520)
                keyboard?.SetKeyState("Return", false);
        }

        var video = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
        Assert.NotNull(video);
        await FrameCapture.CaptureAsync(video, bmpPath, "BMP", TestContext.Current.CancellationToken);

        var bus = session.Machine.Bus;
        var ram = new byte[0x10000];
        for (var a = 0; a < ram.Length; a++)
            ram[a] = bus.Peek((ushort)a);
        File.WriteAllBytes(ramPath, ram);

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"d011={bus.Peek(0xD011):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"d016={bus.Peek(0xD016):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"d018={bus.Peek(0xD018):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"dd00={bus.Peek(0xDD00):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"d015={bus.Peek(0xD015):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"d021={bus.Peek(0xD021):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"d025={bus.Peek(0xD025):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"d026={bus.Peek(0xD026):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"d027={bus.Peek(0xD027):X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"width={video.FrameWidth} height={video.FrameHeight}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"bmp={bmpPath}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"ram={ramPath}");
        File.WriteAllText(txtPath, sb.ToString());
        Assert.True(File.Exists(bmpPath));
        Assert.True(File.Exists(ramPath));
        Assert.Equal(0x10000, ram.Length);
    }

    private static void TryInjectPetscii(IMachine machine, byte petscii)
    {
        try
        {
            if (machine.Bus.Peek(0x00C6) != 0)
                return;
            machine.Bus.Write(0x0277, petscii);
            machine.Bus.Write(0x00C6, 1);
        }
        catch (NotSupportedException)
        {
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
