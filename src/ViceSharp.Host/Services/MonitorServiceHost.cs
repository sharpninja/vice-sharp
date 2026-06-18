using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class MonitorServiceHost : IMonitorService
{
    private const int AddressSpaceSize = 0x10000;
    private const int MaxDisassemblyCount = 256;

    private readonly EmulatorRuntimeRegistry _registry;

    public MonitorServiceHost(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ValueTask<MonitorCommandResponse> ExecuteCommandAsync(
        ExecuteMonitorCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorCommandResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), string.Empty, null));

        if (string.IsNullOrWhiteSpace(request.Command))
            return ValueTask.FromResult(new MonitorCommandResponse(RpcStatus.InvalidArgument("Command is required."), string.Empty, null));

        lock (session.SyncRoot)
        {
            var monitor = new ViceSharp.Monitor.Monitor(session.Machine);
            var output = monitor.ExecuteCommand(request.Command);
            return ValueTask.FromResult(new MonitorCommandResponse(RpcStatus.Ok(), output, HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<MonitorRegistersResponse> ReadRegistersAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorRegistersResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), null, null));

        lock (session.SyncRoot)
        {
            var state = session.Machine.GetState();
            return ValueTask.FromResult(new MonitorRegistersResponse(
                RpcStatus.Ok(),
                HostProtocolMapper.ToMachineStateDto(state),
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<MonitorDisassemblyResponse> DisassembleAsync(
        MonitorDisassemblyRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorDisassemblyResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), [], null));

        var validation = ValidateDisassemblyRequest(request.Address, request.Count);
        if (validation is not null)
            return ValueTask.FromResult(new MonitorDisassemblyResponse(validation, [], null));

        lock (session.SyncRoot)
        {
            var monitor = new ViceSharp.Monitor.Monitor(session.Machine);
            var lines = monitor.Disassemble((ushort)request.Address, request.Count)
                .Select(entry => new MonitorDisassemblyLineDto(
                    entry.Address,
                    entry.Bytes,
                    entry.Text,
                    entry.Length,
                    entry.NextAddress))
                .ToArray();

            return ValueTask.FromResult(new MonitorDisassemblyResponse(
                RpcStatus.Ok(),
                lines,
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<MonitorBreakpointsResponse> ListBreakpointsAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorBreakpointsResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), [], null));

        lock (session.SyncRoot)
        {
            return ValueTask.FromResult(new MonitorBreakpointsResponse(
                RpcStatus.Ok(),
                ToBreakpointDtos(session),
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<MonitorBreakpointsResponse> AddBreakpointAsync(
        MonitorBreakpointRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorBreakpointsResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), [], null));

        var validation = ValidateAddress(request.Address);
        if (validation is not null)
            return ValueTask.FromResult(new MonitorBreakpointsResponse(validation, [], null));

        lock (session.SyncRoot)
        {
            session.Breakpoints.Add((ushort)request.Address);
            return ValueTask.FromResult(new MonitorBreakpointsResponse(
                RpcStatus.Ok(),
                ToBreakpointDtos(session),
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<MonitorBreakpointsResponse> RemoveBreakpointAsync(
        MonitorBreakpointRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorBreakpointsResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), [], null));

        var validation = ValidateAddress(request.Address);
        if (validation is not null)
            return ValueTask.FromResult(new MonitorBreakpointsResponse(validation, [], null));

        lock (session.SyncRoot)
        {
            session.Breakpoints.Remove((ushort)request.Address);
            return ValueTask.FromResult(new MonitorBreakpointsResponse(
                RpcStatus.Ok(),
                ToBreakpointDtos(session),
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<MonitorMemoryResponse> ReadMemoryAsync(
        MonitorReadMemoryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorMemoryResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), request.Address, [], null));

        var validation = ValidateMemoryRange(request.Address, request.Length);
        if (validation is not null)
            return ValueTask.FromResult(new MonitorMemoryResponse(validation, request.Address, [], null));

        lock (session.SyncRoot)
        {
            var data = new byte[request.Length];
            for (var offset = 0; offset < data.Length; offset++)
                data[offset] = session.Machine.Bus.Peek((ushort)(request.Address + offset));

            return ValueTask.FromResult(new MonitorMemoryResponse(
                RpcStatus.Ok(),
                request.Address,
                data,
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<MonitorMemoryWriteResponse> WriteMemoryAsync(
        MonitorWriteMemoryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorMemoryWriteResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), request.Address, 0, null));

        if (request.Data is null || request.Data.Length == 0)
        {
            return ValueTask.FromResult(new MonitorMemoryWriteResponse(
                RpcStatus.InvalidArgument("Data is required."),
                request.Address,
                0,
                null));
        }

        var validation = ValidateMemoryRange(request.Address, request.Data.Length);
        if (validation is not null)
            return ValueTask.FromResult(new MonitorMemoryWriteResponse(validation, request.Address, 0, null));

        lock (session.SyncRoot)
        {
            for (var offset = 0; offset < request.Data.Length; offset++)
                session.Machine.Bus.Write((ushort)(request.Address + offset), request.Data[offset]);

            return ValueTask.FromResult(new MonitorMemoryWriteResponse(
                RpcStatus.Ok(),
                request.Address,
                request.Data.Length,
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<GetTickHistoryResponse> GetTickHistoryAsync(
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new GetTickHistoryResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), []));

        var ticks = session.TickHistory.Snapshot();
        var dtos = new TickHistoryEntryDto[ticks.Count];
        for (var i = 0; i < ticks.Count; i++)
        {
            var r = ticks[i].Registers;
            dtos[i] = new TickHistoryEntryDto(
                i,
                r.InstructionAddress,
                r.Opcode,
                r.A, r.X, r.Y, r.S, r.P,
                r.Pc,
                ticks[i].Writes.Length);
        }

        return ValueTask.FromResult(new GetTickHistoryResponse(RpcStatus.Ok(), dtos));
    }

    public ValueTask<MonitorMemoryResponse> ReadMemoryAtTickAsync(
        ReadMemoryAtTickRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new MonitorMemoryResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), request.Address, [], null));

        var validation = ValidateMemoryRange(request.Address, request.Length);
        if (validation is not null)
            return ValueTask.FromResult(new MonitorMemoryResponse(validation, request.Address, [], null));

        lock (session.SyncRoot)
        {
            var ticks = session.TickHistory.Snapshot();
            if (request.TickIndex < 0 || request.TickIndex >= ticks.Count)
                return ValueTask.FromResult(new MonitorMemoryResponse(
                    RpcStatus.InvalidArgument($"Tick index {request.TickIndex} is out of range (0..{ticks.Count - 1})."),
                    request.Address,
                    [],
                    HostProtocolMapper.ToStatusDto(session)));

            // Snapshot the current (paused) memory image, then reverse-apply later ticks'
            // write-deltas to reconstruct RAM as it was at the requested tick.
            var image = new byte[AddressSpaceSize];
            for (var addr = 0; addr < AddressSpaceSize; addr++)
                image[addr] = session.Machine.Bus.Peek((ushort)addr);

            TickHistoryReconstruction.ReconstructInto(image, ticks, request.TickIndex);

            var data = new byte[request.Length];
            Array.Copy(image, request.Address, data, 0, request.Length);

            return ValueTask.FromResult(new MonitorMemoryResponse(
                RpcStatus.Ok(),
                request.Address,
                data,
                HostProtocolMapper.ToStatusDto(session)));
        }
    }

    public ValueTask<GetChipStateAtTickResponse> GetChipStateAtTickAsync(
        GetChipStateAtTickRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(request.SessionId, out var session))
            return ValueTask.FromResult(new GetChipStateAtTickResponse(HostProtocolMapper.MissingSessionStatus(request.SessionId), []));

        lock (session.SyncRoot)
        {
            var ticks = session.TickHistory.Snapshot();
            if (request.TickIndex < 0 || request.TickIndex >= ticks.Count)
                return ValueTask.FromResult(new GetChipStateAtTickResponse(
                    RpcStatus.InvalidArgument($"Tick index {request.TickIndex} is out of range (0..{ticks.Count - 1})."),
                    []));

            var chipState = ticks[request.TickIndex].ChipState;
            // Decode by iterating the stateful devices in the same registry order the recorder
            // captured them, slicing the blob by each device's StateSize.
            var devices = session.Machine.Devices.All.OfType<IStatefulDevice>().ToArray();
            var chips = new List<ChipStateDto>(devices.Length);
            var offset = 0;
            foreach (var device in devices)
            {
                if (offset + device.StateSize > chipState.Length)
                    break;

                var fields = device.DecodeState(chipState.AsSpan(offset, device.StateSize))
                    .Select(f => new ChipStateFieldDto(f.Name, f.Value, f.Width))
                    .ToArray();
                chips.Add(new ChipStateDto(device.StateName, fields));
                offset += device.StateSize;
            }

            return ValueTask.FromResult(new GetChipStateAtTickResponse(RpcStatus.Ok(), chips));
        }
    }

    private static RpcStatus? ValidateMemoryRange(int address, int length)
    {
        var addressValidation = ValidateAddress(address);
        if (addressValidation is not null)
            return addressValidation;

        if (length <= 0)
            return RpcStatus.InvalidArgument("Length must be greater than zero.");

        if (length > AddressSpaceSize || address + length > AddressSpaceSize)
            return RpcStatus.InvalidArgument("Memory range must stay within the 16-bit address space.");

        return null;
    }

    private static RpcStatus? ValidateDisassemblyRequest(int address, int count)
    {
        var addressValidation = ValidateAddress(address);
        if (addressValidation is not null)
            return addressValidation;

        if (count <= 0)
            return RpcStatus.InvalidArgument("Count must be greater than zero.");

        if (count > MaxDisassemblyCount)
            return RpcStatus.InvalidArgument($"Count must be less than or equal to {MaxDisassemblyCount}.");

        return null;
    }

    private static RpcStatus? ValidateAddress(int address)
    {
        return address is < 0 or > 0xFFFF
            ? RpcStatus.InvalidArgument("Address must be within the 16-bit address space.")
            : null;
    }

    private static IReadOnlyList<MonitorBreakpointDto> ToBreakpointDtos(EmulatorRuntimeSession session)
    {
        return session.Breakpoints
            .Select(address => new MonitorBreakpointDto(address, true))
            .ToArray();
    }
}
