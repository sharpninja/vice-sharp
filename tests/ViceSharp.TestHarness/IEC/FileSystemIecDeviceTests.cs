namespace ViceSharp.TestHarness.Iec;

using ViceSharp.Core.Iec;
using Xunit;

/// <summary>
/// uIEC/fsdevice-style host directory attach + sequential LOAD/SAVE.
/// </summary>
public sealed class FileSystemIecDeviceTests
{
    [Fact]
    public void AttachDirectory_ListAndLoadFile_ReturnsPayload()
    {
        var root = Path.Combine(Path.GetTempPath(), "vicesharp-fsiec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var payload = new byte[] { 0x01, 0x08, 0x0B, 0x08, 0x0A, 0x00, 0x99, 0x22, 0x48, 0x49, 0x22, 0x00, 0x00, 0x00 };
            var path = Path.Combine(root, "HELLO.PRG");
            File.WriteAllBytes(path, payload);

            var dev = new FileSystemIecDevice(unitNumber: 9);
            dev.AttachDirectory(root);
            Assert.True(dev.IsAttached);
            Assert.Contains("HELLO.PRG", dev.ListFiles());

            var loaded = dev.LoadFile("HELLO.PRG");
            Assert.Equal(payload, loaded);

            // name without extension
            var loaded2 = dev.LoadFile("HELLO");
            Assert.Equal(payload, loaded2);

            var listing = dev.FormatDirectoryListing();
            Assert.Contains("HELLO.PRG", listing);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void SaveFile_WritesHostPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "vicesharp-fsiec-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var dev = new FileSystemIecDevice(8);
            dev.AttachDirectory(root);
            var data = new byte[] { 0x00, 0x10, 0xA9, 0x01 };
            dev.SaveFile("TEST.PRG", data);
            Assert.Equal(data, File.ReadAllBytes(Path.Combine(root, "TEST.PRG")));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void UnitPolicy_RejectsInvalidUnit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileSystemIecDevice(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileSystemIecDevice(12));
    }
}
