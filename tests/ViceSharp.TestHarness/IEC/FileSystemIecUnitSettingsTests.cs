namespace ViceSharp.TestHarness.Iec;

using ViceSharp.Architectures.Vic20;
using ViceSharp.Core.Iec;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Prove FileSystemIec unit rebinding via settings and true-drive collision policy.
/// </summary>
public sealed class FileSystemIecUnitSettingsTests
{
    [Fact]
    public void SetUnitNumber_RebindsLiveDevice()
    {
        var fs = new FileSystemIecDevice(9);
        Assert.Equal(9, fs.UnitNumber);
        fs.SetUnitNumber(10);
        Assert.Equal(10, fs.UnitNumber);
    }

    [Fact]
    public void SetUnitNumber_RejectsTrueDriveCollision()
    {
        var fs = new FileSystemIecDevice(9);
        var ex = Assert.Throws<InvalidOperationException>(() => fs.SetUnitNumber(8, reservedTrueDriveUnit: 8));
        Assert.Contains("collides", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(9, fs.UnitNumber); // unchanged on failure
    }

    [Fact]
    public async Task SettingsServiceHost_FileSystemIecUnit_RebindsDeviceOnVic20Session()
    {
        var machine = MachineTestFactory.CreateVic20Machine(new Vic20Descriptor("vic20"));
        var session = new EmulatorRuntimeSession("vic20-fsiec", machine.Architecture, machine)
        {
            FileSystemIecUnit = 9,
        };
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        var settings = new SettingsServiceHost(registry);

        var response = await settings.UpdateSettingsAsync(
            new UpdateSettingsRequest(session.SessionId, FileSystemIecUnit: 10),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(10, session.FileSystemIecUnit);
        var fs = session.Machine.Devices.GetAll<FileSystemIecDevice>().Single();
        Assert.Equal(10, fs.UnitNumber);
        Assert.Contains(response.Diagnostics, d => d.Setting == "iec.filesystem" && d.AppliedLive);
    }

    [Fact]
    public async Task SettingsServiceHost_FileSystemIecRoot_AttachesDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "vicesharp-settings-fsiec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllBytes(Path.Combine(root, "A.PRG"), new byte[] { 0x01, 0x08, 0x00 });

            var machine = MachineTestFactory.CreateVic20Machine(new Vic20Descriptor("vic20"));
            var session = new EmulatorRuntimeSession("vic20-fsiec-root", machine.Architecture, machine);
            var registry = new EmulatorRuntimeRegistry();
            registry.Add(session);
            var settings = new SettingsServiceHost(registry);

            var response = await settings.UpdateSettingsAsync(
                new UpdateSettingsRequest(session.SessionId, FileSystemIecRootPath: root, FileSystemIecUnit: 9),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
            var fs = session.Machine.Devices.GetAll<FileSystemIecDevice>().Single();
            Assert.True(fs.IsAttached);
            Assert.Equal(root, fs.RootPath);
            Assert.Contains("A.PRG", fs.ListFiles());
            Assert.Equal(new byte[] { 0x01, 0x08, 0x00 }, fs.LoadFile("A.PRG"));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { /* best effort */ }
        }
    }
}
