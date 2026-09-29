using System.CommandLine;
using Avalonia.RemoteControl.Protocol.V1;

namespace ViceSharp.RemoteControlCli.Commands;

internal static class SetCommand
{
    public static Command Create()
    {
        var command = new Command("set", "Set a control property by AutomationId or match.");
        command.Options.Add(RemoteControlCommandFactory.MatchOption);
        command.Options.Add(RemoteControlCommandFactory.PropertyOption);
        command.Options.Add(RemoteControlCommandFactory.ValueOption);
        command.SetAction(async (parse, ct) =>
        {
            await using var session = RemoteControlSession.Open(parse);
            await DumpCommand.WriteCapabilitiesAsync(session);
            var snapshot = await session.Inner.GetSnapshotAsync(session.Token);
            var match = parse.GetValue(RemoteControlCommandFactory.MatchOption) ?? "";
            var propertyName = parse.GetValue(RemoteControlCommandFactory.PropertyOption) ?? "Text";
            var propertyValue = parse.GetValue(RemoteControlCommandFactory.ValueOption) ?? "";
            var found = Find(snapshot.Nodes, match);
            if (found is null)
            {
                Console.Error.WriteLine($"NO_MATCH:{match}");
                return 3;
            }

            Console.WriteLine($"SET {found.Id} {propertyName}={propertyValue} type={found.TypeName}");
            var result = await session.Inner.SetPropertyAsync(found.Id, propertyName, propertyValue, session.Token);
            Console.WriteLine($"ok={result.Succeeded} message={result.Message}");
            return result.Succeeded ? 0 : 4;
        });
        return command;
    }

    internal static TreeNode? Find(IEnumerable<TreeNode> nodes, string match)
    {
        return nodes.FirstOrDefault(n =>
            n.Id == match
            || string.Equals(n.AutomationId, match, StringComparison.Ordinal)
            || n.AutomationName.Contains(match, StringComparison.OrdinalIgnoreCase)
            || n.Name.Contains(match, StringComparison.OrdinalIgnoreCase)
            || n.Properties.Any(p =>
                (p.Name is "Text" or "Watermark" or "Name") &&
                p.Value.Contains(match, StringComparison.OrdinalIgnoreCase)));
    }
}
