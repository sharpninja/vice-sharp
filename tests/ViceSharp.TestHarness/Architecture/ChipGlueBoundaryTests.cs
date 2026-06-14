namespace ViceSharp.TestHarness.Architecture;

using Xunit;

/// <summary>
/// TR-SYSTEM-CORE-001 / TEST-ARCH-CHIPGLUE-001 / ARCH-CHIPGLUE-001.
/// Use case: protect reusable chip implementations from machine-specific
/// board and device glue after the Iteration 1 chip-glue remediation.
/// Acceptance: every check below covers one or more audit acceptance criteria
/// from <c>docs/requirements/traceability/ARCH-CHIPGLUE-001-Chip-Audit-2026-06-12.md</c>.
/// </summary>
public sealed class ChipGlueBoundaryTests
{
    [Fact]
    public void SharedVia6522Core_DoesNotContainDriveOrMachineGlue()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "IEC", "Via6522.cs");

        Assert.DoesNotContain("1541", source);
        Assert.DoesNotContain("C1541", source);
        Assert.DoesNotContain("C64", source);
        Assert.DoesNotContain("CIA1", source);
        Assert.DoesNotContain("CIA2", source);
        Assert.DoesNotContain("VIC20", source);
        Assert.DoesNotContain("VIC-20", source);
        Assert.DoesNotContain("0x1800", source);
        Assert.DoesNotContain("0x0400", source);
    }

    [Fact]
    public void SharedMos6526Core_DoesNotDescribeBoardSpecificWiringOrCadence()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "Cia", "Mos6526.cs");

        Assert.DoesNotContain("CIA1", source);
        Assert.DoesNotContain("CIA2", source);
        Assert.DoesNotContain("C64", source);
        Assert.DoesNotContain("datasette READ", source);
        Assert.DoesNotContain("user-port", source);
        Assert.DoesNotContain("0xDC00", source);
        Assert.DoesNotContain("985_248", source);
    }

    [Fact]
    public void RetiredDuplicateCiaCore_DoesNotExistInSharedChipPackage()
    {
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Interface", "Cia6526.cs");
    }

    [Fact]
    public void SharedMos6502Core_DoesNotContainC64VicRegisterPolicy()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "Cpu", "Mos6502.cs");

        Assert.DoesNotContain("0xD016", source);
        Assert.DoesNotContain("D016", source);
    }

    [Fact]
    public void SharedSidCore_DoesNotContainC64RegisterWindowDefault()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "Audio", "Sid6581.cs");

        Assert.DoesNotContain("0xD400", source);
        Assert.DoesNotContain("$D400", source);
        Assert.DoesNotContain("D400", source);
    }

    [Fact]
    public void SharedPlaCore_DoesNotContainC64ProcessorPortResetPolicy()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "PLA", "Mos906114.cs");

        Assert.DoesNotContain("C64 power-up", source);
        Assert.DoesNotContain("0x2F", source);
        Assert.DoesNotContain("0x37", source);
        Assert.DoesNotContain("$2F", source);
        Assert.DoesNotContain("$37", source);
        Assert.False(source.Contains("tape", StringComparison.OrdinalIgnoreCase), "Shared PLA source should not describe board-specific tape wiring.");
    }

    [Fact]
    public void VicIiCore_DoesNotContainAlternateHostFramePresentationPath()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "VicIi", "Mos6569.cs");

        Assert.DoesNotContain("GenerateFrame", source);
        Assert.DoesNotContain("GetPixelColor", source);
        Assert.DoesNotContain("320 * 200", source);
        Assert.DoesNotContain("VicBank", source);
        Assert.DoesNotContain("TranslateVicAddress", source);
    }

    [Fact]
    public void RetiredLegacyIecDriveStubs_DoNotExistInSharedChipPackage()
    {
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "IEC", "DiskController.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "IEC", "Mos6502DiskCpu.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "IEC", "IecBus.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "IEC", "IecD64Attachment.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "IEC", "IecDrive.cs");
    }

    [Fact]
    public void C64JoystickPort_DoesNotContainHostEnumerationOrAutofirePolicy()
    {
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Input", "C64JoystickPort.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Input", "C64KeyboardMap.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Input", "C64KeyboardMatrix.cs");

        var source = ReadSource("src", "ViceSharp.Core", "Input", "C64JoystickPort.cs");

        Assert.DoesNotContain("EnumerateDevices", source);
        Assert.DoesNotContain("AutoFire", source);
        Assert.DoesNotContain("autofire", source);
    }

    [Fact]
    public void StandardC64CartridgeMapping_LivesInCoreInsteadOfSharedCartridgePackage()
    {
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Cartridges", "StandardCartridgeImage.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Cartridges", "StandardCartridgeSize.cs");
        AssertSourceContains(
            "public sealed class StandardCartridgeImage",
            "src", "ViceSharp.Core", "StandardCartridgeImage.cs");
    }

    [Fact]
    public void DatasetteDevice_LivesInCoreInsteadOfSharedTapePackage()
    {
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Tape", "Datasette.cs");
        AssertSourceContains(
            "public sealed class Datasette",
            "src", "ViceSharp.Core", "Datasette.cs");
    }

    [Fact]
    public void MediaCaptureInfrastructure_LivesOutsideSharedChipPackage()
    {
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Media", "FrameSequenceCapture.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Media", "RecordingAudioBackend.cs");
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Media", "WavAudioRecorder.cs");
        AssertSourceContains(
            "public sealed class WavAudioRecorder",
            "src", "ViceSharp.Core", "Media", "WavAudioRecorder.cs");
    }

    [Fact]
    public void C64AndC1541GlueLivesInCoreDeviceAdapters()
    {
        AssertSourceContains(
            "public sealed class C64Cia2InterfaceDevice",
            "src", "ViceSharp.Core", "C64Cia2InterfaceDevice.cs");
        AssertSourceContains(
            "public sealed class C1541IecInterfaceDevice",
            "src", "ViceSharp.Core", "C1541IecInterfaceDevice.cs");
        AssertSourceContains(
            "public sealed class C1541DriveMechanismDevice",
            "src", "ViceSharp.Core", "C1541DriveMechanismDevice.cs");
        AssertSourceContains(
            "public sealed class IecDrive",
            "src", "ViceSharp.Core", "IecDrive.cs");
        AssertSourceContains(
            "public sealed class IecD64Attachment",
            "src", "ViceSharp.Core", "IecD64Attachment.cs");
    }

    [Fact]
    public void C64CpuAndCiaBoardPolicyLivesInC64Assembly()
    {
        AssertSourceContains(
            "ShouldDeferCpuAbsoluteStore",
            "src", "ViceSharp.Core", "C64MemoryMap.cs");
        AssertSourceContains(
            "cpu.ShouldDeferAbsoluteStore = memory.ShouldDeferCpuAbsoluteStore",
            "src", "ViceSharp.Core", "ArchitectureBuilder.cs");
        AssertSourceContains(
            "CreateC64Cia",
            "src", "ViceSharp.Core", "ArchitectureBuilder.cs");
        AssertSourceContains(
            "new Sid6581(bus) { BaseAddress = 0xD400 }",
            "src", "ViceSharp.Core", "ArchitectureBuilder.cs");
        AssertSourceContains(
            "_pla.WriteDataDirection(0x2F)",
            "src", "ViceSharp.Core", "C64MemoryMap.cs");
        AssertSourceContains(
            "_pla.WriteDataPort(0x37)",
            "src", "ViceSharp.Core", "C64MemoryMap.cs");
        AssertSourceContains(
            "private ushort TranslateVicAddress",
            "src", "ViceSharp.Core", "C64MemoryMap.cs");
        AssertSourceContains(
            "private int _vicBank",
            "src", "ViceSharp.Core", "C64MemoryMap.cs");
    }

    private static void AssertSourceContains(string expected, params string[] pathParts)
    {
        var source = ReadSource(pathParts);

        Assert.Contains(expected, source);
    }

    private static void AssertSourceFileDoesNotExist(params string[] pathParts)
    {
        var combined = new string[pathParts.Length + 1];
        combined[0] = RepoRoot();
        pathParts.CopyTo(combined, 1);

        Assert.False(File.Exists(Path.Combine(combined)), string.Join(Path.DirectorySeparatorChar, pathParts));
    }

    private static string ReadSource(params string[] pathParts)
    {
        var combined = new string[pathParts.Length + 1];
        combined[0] = RepoRoot();
        pathParts.CopyTo(combined, 1);
        return File.ReadAllText(Path.Combine(combined));
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate ViceSharp.slnx by walking up from " +
            AppContext.BaseDirectory);
    }
}
