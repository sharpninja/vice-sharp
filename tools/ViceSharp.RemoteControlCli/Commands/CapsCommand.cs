using System.CommandLine;

namespace ViceSharp.RemoteControlCli.Commands;

internal static class CapsCommand
{
    public static Command Create()
    {
        var command = new Command("caps", "Print RemoteControl capabilities.");
        command.SetAction(async (parse, ct) =>
        {
            await using var session = RemoteControlSession.Open(parse);
            var caps = await session.Inner.GetCapabilitiesAsync(session.Token);
            Console.WriteLine("capabilities...");
            Console.WriteLine(
                $"protocol={caps.ProtocolVersion} identity={caps.AuthenticatedClientIdentity} " +
                $"snap={caps.SupportsTreeSnapshots} stream={caps.SupportsTreeStreaming} " +
                $"click={caps.SupportsClickInvocation} mutate={caps.SupportsPropertyMutation} " +
                $"frames={caps.SupportsFrameStreaming} input={caps.SupportsRemoteInput} logs={caps.SupportsLogStreaming}");
            return 0;
        });
        return command;
    }
}
