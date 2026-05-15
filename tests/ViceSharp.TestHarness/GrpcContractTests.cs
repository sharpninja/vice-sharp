namespace ViceSharp.TestHarness;

using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

public sealed class GrpcContractTests
{
    [Fact]
    public void ProtocolProject_GeneratesGrpcServiceClients()
    {
        Assert.Equal("vice_sharp.v1.EmulatorHost", GrpcContracts.EmulatorHost.Descriptor.FullName);
        Assert.Equal("vice_sharp.v1.MediaService", GrpcContracts.MediaService.Descriptor.FullName);
        Assert.Equal("vice_sharp.v1.VideoService", GrpcContracts.VideoService.Descriptor.FullName);
        Assert.Equal("vice_sharp.v1.InputService", GrpcContracts.InputService.Descriptor.FullName);
        Assert.Equal("vice_sharp.v1.SettingsService", GrpcContracts.SettingsService.Descriptor.FullName);
        Assert.Equal("vice_sharp.v1.MonitorService", GrpcContracts.MonitorService.Descriptor.FullName);
        Assert.Equal("vice_sharp.v1.SnapshotService", GrpcContracts.SnapshotService.Descriptor.FullName);
        Assert.Equal("vice_sharp.v1.CaptureService", GrpcContracts.CaptureService.Descriptor.FullName);
    }

    [Fact]
    public void ProtocolProject_KeepsProtoSourceContract()
    {
        var repoRoot = RepoRoot;
        var protoPath = Path.Combine(repoRoot, "src", "ViceSharp.Protocol", ViceSharpProtocol.ProtoFile.Replace('/', Path.DirectorySeparatorChar));
        var proto = File.ReadAllText(protoPath);

        Assert.Contains("service EmulatorHost", proto);
        Assert.Contains("service MediaService", proto);
        Assert.Contains("service VideoService", proto);
        Assert.Contains("service InputService", proto);
        Assert.Contains("service SettingsService", proto);
        Assert.Contains("service MonitorService", proto);
        Assert.Contains("service SnapshotService", proto);
        Assert.Contains("service CaptureService", proto);
        Assert.Contains("rpc StepCycle", proto);
        Assert.Contains("rpc RewindCycle", proto);
        Assert.Contains("rpc ResetAndAutostartDrive8", proto);
        Assert.Contains("rpc SetLimiterRate", proto);
        Assert.Contains("power_state", proto);
        Assert.Contains("limiter_rate_percent", proto);
        Assert.Contains("measured_fps", proto);
        Assert.Contains("nominal_clock_hz", proto);
        Assert.Contains("effective_clock_hz", proto);
        Assert.Contains("effective_clock_percent", proto);
        Assert.Contains("frame_count", proto);
        Assert.Contains("model_id", proto);
        Assert.Contains("pc", proto);
        Assert.Contains("host_automation_description", proto);
        Assert.Contains("host_automation_active", proto);
        Assert.Contains("last_host_automation_error", proto);
        Assert.Contains("scale", proto);
        Assert.Contains("crop_mode", proto);
        Assert.Contains("aspect_mode", proto);
        Assert.Contains("mode", proto);
        Assert.Contains("audio", proto);
        Assert.Contains("resources", proto);
        Assert.Contains("is_enabled", proto);
        Assert.Contains("INPUT_PORT_PRIMARY_JOYSTICK", proto);
        Assert.Contains("rpc ReadRegisters", proto);
        Assert.Contains("MonitorRegistersResponse", proto);
        Assert.Contains("rpc Disassemble", proto);
        Assert.Contains("MonitorDisassemblyRequest", proto);
        Assert.Contains("MonitorDisassemblyLineDto", proto);
        Assert.Contains("instruction_bytes", proto);
        Assert.Contains("rpc ListBreakpoints", proto);
        Assert.Contains("rpc AddBreakpoint", proto);
        Assert.Contains("rpc RemoveBreakpoint", proto);
        Assert.Contains("MonitorBreakpointRequest", proto);
        Assert.Contains("MonitorBreakpointsResponse", proto);
        Assert.Contains("rpc ReadMemory", proto);
        Assert.Contains("rpc WriteMemory", proto);
        Assert.Contains("MonitorReadMemoryRequest", proto);
        Assert.Contains("MonitorMemoryWriteResponse", proto);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
