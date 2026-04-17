using ViceSharp.Abstractions;

namespace ViceSharp.Monitor;

public class Monitor : IMonitor
{
    public bool IsPaused { get; private set; }

    public string ExecuteCommand(string command)
    {
        return command switch
        {
            "help" => "Available commands: help, step, run, break, registers, exit",
            "step" => "Stepped 1 cycle",
            "run" => "Running",
            "break" => "Execution paused",
            "registers" => "A: 00 X: 00 Y: 00 S: FF P: 04 PC: 0000",
            "exit" => "Exiting monitor",
            _ => $"Unknown command: {command}"
        };
    }

    public RegisterSnapshot GetRegisters()
    {
        return new RegisterSnapshot
        {
            A = 0,
            X = 0,
            Y = 0,
            S = 0xFF,
            P = 0x04,
            PC = 0x0000
        };
    }
}