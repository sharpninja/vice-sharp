using System.CommandLine;
using ViceSharp.RemoteControlCli.Commands;

namespace ViceSharp.RemoteControlCli;

/// <summary>
/// FR: FR-UISETTINGS-001 / TEST-UISET-002. Builds the System.CommandLine
/// graph for the Avalonia RemoteControl driver. Program.cs only invokes this.
/// </summary>
public static class RemoteControlCommandFactory
{
    /// <summary>gRPC endpoint (default loopback 47100).</summary>
    public static readonly Option<string> EndpointOption = new("--endpoint", "-e")
    {
        Description = "RemoteControl gRPC endpoint.",
        Recursive = true,
        DefaultValueFactory = _ => "http://127.0.0.1:47100/",
    };

    /// <summary>Bearer token. Defaults to AVALONIA_REMOTE_TOKEN.</summary>
    public static readonly Option<string?> TokenOption = new("--token")
    {
        Description = "RemoteControl bearer token (or AVALONIA_REMOTE_TOKEN).",
        Recursive = true,
        DefaultValueFactory = _ => Environment.GetEnvironmentVariable("AVALONIA_REMOTE_TOKEN"),
    };

    /// <summary>Transport protocol. Defaults to grpc.</summary>
    public static readonly Option<string> TransportOption = new("--transport")
    {
        Description = "RemoteControl transport protocol.",
        Recursive = true,
        DefaultValueFactory = _ => Environment.GetEnvironmentVariable("AVALONIA_REMOTE_TRANSPORT") ?? "grpc",
    };

    /// <summary>Per-call timeout in milliseconds.</summary>
    public static readonly Option<int> TimeoutMsOption = new("--timeout-ms")
    {
        Description = "Call timeout in milliseconds.",
        Recursive = true,
        DefaultValueFactory = _ =>
        {
            var raw = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_TIMEOUT_MS");
            return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : 20000;
        },
    };

    /// <summary>Tree match (AutomationId, name, or text).</summary>
    public static readonly Option<string> MatchOption = new("--match", "-m")
    {
        Description = "Node match: AutomationId, id, name, or text.",
        Required = true,
    };

    /// <summary>Property name for set.</summary>
    public static readonly Option<string> PropertyOption = new("--property", "-p")
    {
        Description = "Property name to mutate.",
        DefaultValueFactory = _ => "Text",
    };

    /// <summary>Property value for set.</summary>
    public static readonly Option<string> ValueOption = new("--value", "-v")
    {
        Description = "Property value to write.",
        DefaultValueFactory = _ => string.Empty,
    };

    /// <summary>AutomationId or node id for click.</summary>
    public static readonly Option<string> IdOption = new("--id")
    {
        Description = "AutomationId or node id to click.",
        Required = true,
    };

    /// <summary>PNG output path for frame.</summary>
    public static readonly Option<string> OutOption = new("--out", "-o")
    {
        Description = "PNG path for a single captured frame.",
        DefaultValueFactory = _ => Path.Combine(Environment.CurrentDirectory, "remote-frame.png"),
    };

    /// <summary>Build the root command with caps/dump/set/click/frame subcommands.</summary>
    public static RootCommand Create()
    {
        var root = new RootCommand("ViceSharp Avalonia RemoteControl CLI (System.CommandLine).");
        root.Options.Add(EndpointOption);
        root.Options.Add(TokenOption);
        root.Options.Add(TransportOption);
        root.Options.Add(TimeoutMsOption);
        root.Subcommands.Add(CapsCommand.Create());
        root.Subcommands.Add(DumpCommand.Create());
        root.Subcommands.Add(SetCommand.Create());
        root.Subcommands.Add(ClickCommand.Create());
        root.Subcommands.Add(FrameCommand.Create());
        return root;
    }
}
