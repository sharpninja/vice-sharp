namespace ViceSharp.TestHarness;

using Xunit;

public sealed class BasicBootProofTests
{
    [Fact]
    public void C64_Boot_Does_Not_Fall_Into_Low_Ram_During_Early_Reset()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var recentPcs = new Queue<ushort>();

        for (var instruction = 0; instruction < 100000; instruction++)
        {
            var pc = machine.GetState().PC;

            recentPcs.Enqueue(pc);
            if (recentPcs.Count > 32)
                recentPcs.Dequeue();

            if (pc is >= 0x0400 and < 0xA000)
            {
                var history = string.Join(" -> ", recentPcs.Select(value => value.ToString("X4")));
                Assert.Fail($"Early boot executed low RAM at instruction {instruction}, PC=${pc:X4}. Recent PCs: {history}");
            }

            machine.StepInstruction();
        }
    }

    [Fact]
    public void C64_Boot_Reaches_Ready_Prompt()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        const int maxFrames = 400;

        for (var frame = 0; frame < maxFrames; frame++)
        {
            machine.RunFrame();

            if (ContainsReadyPrompt(machine))
            {
                return;
            }
        }

        var screen = ReadScreenText(machine);
        var state = machine.GetState();
        Assert.Fail($"READY prompt not found after {maxFrames} frames. PC=${state.PC:X4}, cycles={state.Cycle}, screen=[{screen}]");
    }

    private static bool ContainsReadyPrompt(ViceSharp.Abstractions.IMachine machine)
    {
        var screenCodes = ReadScreenCodes(machine);
        ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
        ReadOnlySpan<byte> asciiReady = "READY"u8;

        return screenCodes.AsSpan().IndexOf(screenCodeReady) >= 0
            || screenCodes.AsSpan().IndexOf(asciiReady) >= 0;
    }

    private static byte[] ReadScreenCodes(ViceSharp.Abstractions.IMachine machine)
    {
        var buffer = new byte[1000];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = machine.Bus.Peek((ushort)(0x0400 + i));
        }

        return buffer;
    }

    private static string ReadScreenText(ViceSharp.Abstractions.IMachine machine)
    {
        var screenCodes = ReadScreenCodes(machine);
        var chars = new char[screenCodes.Length];

        for (var i = 0; i < screenCodes.Length; i++)
        {
            chars[i] = DecodeScreenCode(screenCodes[i]);
        }

        return new string(chars);
    }

    private static char DecodeScreenCode(byte code)
    {
        if (code == 0x20)
            return ' ';

        if (code is >= 1 and <= 26)
            return (char)('A' + code - 1);

        if (code is >= 0x30 and <= 0x39)
            return (char)code;

        return code switch
        {
            0x00 => '@',
            0x2E => '.',
            0x2A => '*',
            0x3A => ':',
            _ => '?'
        };
    }
}
