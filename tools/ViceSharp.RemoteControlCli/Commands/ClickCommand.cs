using System.CommandLine;

namespace ViceSharp.RemoteControlCli.Commands;

internal static class ClickCommand
{
    public static Command Create()
    {
        var command = new Command("click", "Click a control by AutomationId or node id.");
        command.Options.Add(RemoteControlCommandFactory.IdOption);
        command.SetAction(async (parse, ct) =>
        {
            await using var session = RemoteControlSession.Open(parse);
            await DumpCommand.WriteCapabilitiesAsync(session);
            var snapshot = await session.Inner.GetSnapshotAsync(session.Token);
            var id = parse.GetValue(RemoteControlCommandFactory.IdOption) ?? "";
            var found = snapshot.Nodes.FirstOrDefault(n =>
                n.Id == id || string.Equals(n.AutomationId, id, StringComparison.Ordinal));
            if (found is null)
            {
                Console.Error.WriteLine($"NO_MATCH:{id}");
                return 3;
            }

            Console.WriteLine($"CLICK {found.Id} auto={found.AutomationName} type={found.TypeName}");
            var result = await session.Inner.InvokeClickAsync(found.Id, session.Token);
            Console.WriteLine($"ok={result.Succeeded} message={result.Message}");
            return result.Succeeded ? 0 : 4;
        });
        return command;
    }
}
