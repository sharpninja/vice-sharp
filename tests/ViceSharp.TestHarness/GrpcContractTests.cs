namespace ViceSharp.TestHarness;

using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

public sealed class GrpcContractTests
{
    /// <summary>
    /// FR: FR-HOST-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: The ViceSharp.Protocol project must generate gRPC service
    /// clients for every host service so Avalonia and external clients can
    /// communicate with the host across the protocol boundary.
    /// Acceptance: Every required service descriptor (EmulatorHost, Media,
    /// Video, Input, Settings, Monitor, Snapshot, Capture) carries the
    /// expected <c>vice_sharp.v1.&lt;Name&gt;</c> fully-qualified name.
    /// </summary>
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

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-001 / TEST-HOST-DIAG-001.
    /// Use case: local tools such as grpcurl must discover a read-only
    /// diagnostics service without reading repository source.
    /// Acceptance: the generated service descriptor exists with the host-info,
    /// session-list, current-session, snapshot, and streaming snapshot RPCs.
    /// </summary>
    [Fact]
    public void DiagnosticsServiceDescriptor_IsGeneratedWithDiscoveryRpcSurface()
    {
        var serviceType = DiagnosticsReflectionTestHelpers.RequiredType(
            "ViceSharp.Protocol.Grpc.DiagnosticsService, ViceSharp.Protocol");
        var descriptor = serviceType.GetProperty("Descriptor")?.GetValue(null);
        Assert.NotNull(descriptor);
        Assert.Equal("vice_sharp.v1.DiagnosticsService", descriptor.GetType().GetProperty("FullName")?.GetValue(descriptor));

        var methods = Assert.IsAssignableFrom<System.Collections.IEnumerable>(
                descriptor.GetType().GetProperty("Methods")?.GetValue(descriptor))
            .Cast<object>()
            .Select(method => method.GetType().GetProperty("Name")?.GetValue(method)?.ToString())
            .ToArray();

        Assert.Contains("GetHostInfo", methods);
        Assert.Contains("ListSessions", methods);
        Assert.Contains("GetCurrentSession", methods);
        Assert.Contains("GetPerformanceSnapshot", methods);
        Assert.Contains("WatchPerformance", methods);
    }

    /// <summary>
    /// FR: FR-HOST-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: The committed protobuf source must continue to declare the
    /// full set of services, RPCs and message fields that the generated gRPC
    /// surface relies on - any accidental deletion would silently break
    /// downstream clients.
    /// Acceptance: The proto file contains every required service, RPC, and
    /// field name string used by host/runtime/monitor/video/audio surfaces.
    /// </summary>
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
        Assert.Contains("iec_bus_active", proto);
        Assert.Contains("iec_bus_transition_count", proto);
        Assert.Contains("iec_bus_activity_state", proto);
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
        Assert.Contains("capture_microphone", proto);
        Assert.Contains("microphone_device", proto);
        Assert.Contains("microphone_input_format", proto);
        Assert.Contains("supports_microphone", proto);
    }

    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-001 / TEST-HOST-DIAG-001.
    /// Use case: the diagnostics snapshot contract must support callers that
    /// know a specific session id and callers that want the current UI session.
    /// Acceptance: the proto source declares optional-session performance
    /// snapshot messages plus host, process, pump, and UI diagnostic counters.
    /// </summary>
    [Fact]
    public void DiagnosticsSnapshotMessages_ExposeOptionalSessionAndPerformanceShape()
    {
        var repoRoot = RepoRoot;
        var protoPath = Path.Combine(repoRoot, "src", "ViceSharp.Protocol", ViceSharpProtocol.ProtoFile.Replace('/', Path.DirectorySeparatorChar));
        var proto = File.ReadAllText(protoPath);

        Assert.Contains("message PerformanceSnapshotRequest", proto);
        Assert.Contains("string session_id = 1", proto);
        Assert.Contains("message PerformanceSnapshotResponse", proto);
        Assert.Contains("message HostInfoDto", proto);
        Assert.Contains("message SessionSummaryDto", proto);
        Assert.Contains("message ProcessDiagnosticsDto", proto);
        Assert.Contains("message PumpDiagnosticsDto", proto);
        Assert.Contains("message UiDiagnosticsDto", proto);
        Assert.Contains("EmulatorStatusDto emulator_status", proto);
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
