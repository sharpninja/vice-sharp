namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Core;
using ViceSharp.Monitor;
using Xunit;

public sealed class DeterministicTraceLoggerTests
{
    [Fact]
    public void LogInstruction_IncludesOpcodeBytesAndDisassembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-trace-{Guid.NewGuid():N}.log");

        try
        {
            var machine = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());
            var pc = machine.GetState().PC;
            machine.Bus.Write(pc, 0xA9);
            machine.Bus.Write((ushort)(pc + 1), 0x01);

            using (var logger = new DeterministicTraceLogger(machine, outputPath))
            {
                logger.LogInstruction();
                logger.Flush();
            }

            var line = File.ReadAllText(outputPath);
            Assert.Contains(pc.ToString("X4"), line);
            Assert.Contains("A9 01", line);
            Assert.Contains("LDA #$01", line);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
