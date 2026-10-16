namespace ViceSharp.RemoteControlCli.Tests;

using System.CommandLine;
using ViceSharp.RemoteControlCli;
using Xunit;

/// <summary>
/// FR: FR-UISETTINGS-001, TR: TR-UIAXAML-VIEWS-001, TEST: TEST-UISET-002.
/// Use case: Live Settings RemoteControl driving must use System.CommandLine
/// subcommands, not a hand-rolled Program.cs argument ladder.
/// Acceptance: caps, dump, set, click, and frame parse with endpoint/token
/// options and report errors for unknown commands. Parse-only; no gRPC.
/// </summary>
public sealed class RemoteControlCommandFactoryTests
{
    [Fact]
    public void Create_Dump_ParsesEndpointAndToken()
    {
        var parse = RemoteControlCommandFactory.Create().Parse(
            ["dump", "--endpoint", "http://127.0.0.1:47100/", "--token", "vicesharp-debug-local"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("dump", parse.CommandResult.Command.Name);
        Assert.Equal("http://127.0.0.1:47100/", parse.GetValue(RemoteControlCommandFactory.EndpointOption));
        Assert.Equal("vicesharp-debug-local", parse.GetValue(RemoteControlCommandFactory.TokenOption));
    }

    [Fact]
    public void Create_Set_ParsesMatchPropertyAndValue()
    {
        var parse = RemoteControlCommandFactory.Create().Parse(
            ["set", "--match", "Limiter.WarpToggle", "--property", "IsChecked", "--value", "true"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("set", parse.CommandResult.Command.Name);
        Assert.Equal("Limiter.WarpToggle", parse.GetValue(RemoteControlCommandFactory.MatchOption));
        Assert.Equal("IsChecked", parse.GetValue(RemoteControlCommandFactory.PropertyOption));
        Assert.Equal("true", parse.GetValue(RemoteControlCommandFactory.ValueOption));
    }

    [Fact]
    public void Create_Click_ParsesAutomationId()
    {
        var parse = RemoteControlCommandFactory.Create().Parse(
            ["click", "--id", "Settings.Apply"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("click", parse.CommandResult.Command.Name);
        Assert.Equal("Settings.Apply", parse.GetValue(RemoteControlCommandFactory.IdOption));
    }

    [Fact]
    public void Create_Frame_ParsesOutputPath()
    {
        var parse = RemoteControlCommandFactory.Create().Parse(
            ["frame", "--out", "C:\\tmp\\rc-frame.png"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("frame", parse.CommandResult.Command.Name);
        Assert.Equal("C:\\tmp\\rc-frame.png", parse.GetValue(RemoteControlCommandFactory.OutOption));
    }

    [Fact]
    public void Create_Caps_ParsesTransportAndTimeout()
    {
        var parse = RemoteControlCommandFactory.Create().Parse(
            ["caps", "--transport", "grpc", "--timeout-ms", "20000"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("caps", parse.CommandResult.Command.Name);
        Assert.Equal("grpc", parse.GetValue(RemoteControlCommandFactory.TransportOption));
        Assert.Equal(20000, parse.GetValue(RemoteControlCommandFactory.TimeoutMsOption));
    }

    [Fact]
    public void Create_UnknownCommand_ReportsError()
    {
        var parse = RemoteControlCommandFactory.Create().Parse(["not-a-command"]);

        Assert.NotEmpty(parse.Errors);
    }
}
