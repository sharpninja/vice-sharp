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
