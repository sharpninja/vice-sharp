using System.CommandLine;
using Avalonia.RemoteControl.Protocol.V1;

namespace ViceSharp.RemoteControlCli.Commands;

internal static class DumpCommand
{
    public static Command Create()
    {
        var command = new Command("dump", "Dump the live visual tree.");
        command.SetAction(async (parse, ct) =>
        {
            await using var session = RemoteControlSession.Open(parse);
            await WriteCapabilitiesAsync(session);
            var snapshot = await session.Inner.GetSnapshotAsync(session.Token);
            Console.WriteLine($"nodes={snapshot.Nodes.Count} sequence={snapshot.Sequence}");
            foreach (var node in snapshot.Nodes)
                WriteNode(node);
            return 0;
        });
        return command;
    }

    internal static async Task WriteCapabilitiesAsync(RemoteControlSession session)
    {
        Console.WriteLine("capabilities...");
        var caps = await session.Inner.GetCapabilitiesAsync(session.Token);
        Console.WriteLine(
            $"protocol={caps.ProtocolVersion} identity={caps.AuthenticatedClientIdentity} " +
            $"snap={caps.SupportsTreeSnapshots} stream={caps.SupportsTreeStreaming} " +
            $"click={caps.SupportsClickInvocation} mutate={caps.SupportsPropertyMutation} " +
            $"frames={caps.SupportsFrameStreaming} input={caps.SupportsRemoteInput} logs={caps.SupportsLogStreaming}");
    }

    internal static void WriteNode(TreeNode node)
    {
        var text = node.Properties.FirstOrDefault(p => p.Name == "Text")?.Value ?? "";
        var tip = node.Properties.FirstOrDefault(p => p.Name is "Tip" or "ToolTip")?.Value ?? "";
        var watermark = node.Properties.FirstOrDefault(p => p.Name == "Watermark")?.Value ?? "";
        var props = string.Join("|", node.Properties.Select(p => p.Name + "=" + p.Value));
        var interesting = !string.IsNullOrWhiteSpace(node.AutomationId)
            || !string.IsNullOrWhiteSpace(node.AutomationName)
            || !string.IsNullOrWhiteSpace(node.Name)
            || !string.IsNullOrWhiteSpace(text)
            || !string.IsNullOrWhiteSpace(tip)
            || !string.IsNullOrWhiteSpace(watermark)
            || node.TypeName.Contains("Button", StringComparison.Ordinal)
            || node.TypeName.Contains("TextBox", StringComparison.Ordinal)
            || node.TypeName.Contains("TextBlock", StringComparison.Ordinal);
        if (!interesting)
            return;

        Console.WriteLine(
            $"{node.Id}\t{node.TypeName}\taid={node.AutomationId}\tauto={node.AutomationName}\tname={node.Name}\ttext={text}\tmark={watermark}\ttip={tip}\tvis={node.IsVisible}\ten={node.IsEnabled}\tfoc={node.IsFocused}\tb={node.Bounds.X},{node.Bounds.Y},{node.Bounds.Width},{node.Bounds.Height}\tab={node.AbsoluteBounds.X},{node.AbsoluteBounds.Y},{node.AbsoluteBounds.Width},{node.AbsoluteBounds.Height}\t{props}");
    }
}
