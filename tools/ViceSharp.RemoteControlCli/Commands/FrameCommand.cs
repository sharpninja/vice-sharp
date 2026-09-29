using System.CommandLine;

namespace ViceSharp.RemoteControlCli.Commands;

internal static class FrameCommand
{
    public static Command Create()
    {
        var command = new Command("frame", "Capture one RemoteControl PNG frame.");
        command.Options.Add(RemoteControlCommandFactory.OutOption);
        command.SetAction(async (parse, ct) =>
        {
            await using var session = RemoteControlSession.Open(parse);
            await DumpCommand.WriteCapabilitiesAsync(session);
            var path = parse.GetValue(RemoteControlCommandFactory.OutOption)
                ?? Path.Combine(Environment.CurrentDirectory, "remote-frame.png");
            Console.WriteLine("watch-frames...");
            await foreach (var frame in session.Inner.WatchFramesAsync(session.Token))
            {
                await File.WriteAllBytesAsync(path, frame.Png.ToByteArray(), session.Token);
                Console.WriteLine(
                    $"frame seq={frame.Sequence} px={frame.PixelWidth}x{frame.PixelHeight} root={frame.RootWidth}x{frame.RootHeight} scale={frame.RenderScale} path={path} bytes={frame.Png.Length}");
                return 0;
            }

            Console.Error.WriteLine("NO_FRAME");
            return 5;
        });
        return command;
    }
}
