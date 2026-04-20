using ViceSharp.Abstractions;

namespace ViceSharp.Monitor;

public sealed class Monitor : IMonitor
{
    private readonly IMachine _machine;
    
    public bool IsPaused { get; private set; }

    public Monitor(IMachine machine)
    {
        _machine = machine;
    }

    public string ExecuteCommand(string command)
    {
        var parts = command.Trim().ToLowerInvariant().Split(' ');
        var cmd = parts[0];
        
        return cmd switch
        {
            "help" => "Available commands: help, step, run, break, regs, dump, exit",
            "step" or "s" => StepInstruction(),
            "run" or "r" => "Resuming execution",
            "break" or "b" => SetBreakpoint(parts),
            "regs" or "registers" => GetRegistersFromMachine(),
            "dump" or "d" => DumpMemory(parts),
            "exit" or "q" => "Exiting monitor",
            _ => $"Unknown command: {cmd}"
        };
    }

    private string StepInstruction()
    {
        _machine.StepInstruction();
        var state = _machine.GetState();
        return $"PC: {state.PC:X4} A: {state.A:X2} X: {state.X:X2} Y: {state.Y:X2} S: {state.S:X2} P: {state.P:X2}";
    }

    private string GetRegistersFromMachine()
    {
        var state = _machine.GetState();
        return $"A: {state.A:X2} X: {state.X:X2} Y: {state.Y:X2} S: {state.S:X2} P: {state.P:X2} PC: {state.PC:X4}";
    }

    private string DumpMemory(string[] parts)
    {
        ushort addr = 0;
        if (parts.Length >= 2)
        {
            _ = ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out addr);
        }
        
        var bus = _machine.Bus;
        var bytes = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            bytes[i] = bus.Read((ushort)(addr + i));
        }
        
        return $"{addr:X4}: {string.Join(" ", bytes.Select(b => b.ToString("X2")))}";
    }

    private string SetBreakpoint(string[] parts)
    {
        if (parts.Length < 2)
            return "Usage: break <address>";
        return "Breakpoint set (not yet implemented)";
    }

    public RegisterSnapshot GetRegisters()
    {
        var state = _machine.GetState();
        return new RegisterSnapshot
        {
            A = state.A,
            X = state.X,
            Y = state.Y,
            S = state.S,
            P = state.P,
            PC = state.PC
        };
    }
}
