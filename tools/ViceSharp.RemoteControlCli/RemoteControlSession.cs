using System.CommandLine;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Protocol;

namespace ViceSharp.RemoteControlCli;

internal sealed class RemoteControlSession : IAsyncDisposable
{
    private RemoteControlSession(RemoteControlDesktopSession inner, CancellationTokenSource timeout)
    {
        Inner = inner;
        Timeout = timeout;
    }

    public RemoteControlDesktopSession Inner { get; }

    public CancellationToken Token => Timeout.Token;

    private CancellationTokenSource Timeout { get; }

    public static RemoteControlSession Open(ParseResult parse)
    {
        var endpoint = parse.GetValue(RemoteControlCommandFactory.EndpointOption)
            ?? "http://127.0.0.1:47100/";
        var token = parse.GetValue(RemoteControlCommandFactory.TokenOption);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Set --token or AVALONIA_REMOTE_TOKEN.");

        var transport = parse.GetValue(RemoteControlCommandFactory.TransportOption) ?? "grpc";
        var timeoutMs = parse.GetValue(RemoteControlCommandFactory.TimeoutMsOption);
        if (timeoutMs <= 0)
            timeoutMs = 20000;

        Console.WriteLine($"transport={transport} endpoint={endpoint} timeoutMs={timeoutMs}");
        var timeout = new CancellationTokenSource(timeoutMs);
        var inner = RemoteControlDesktopSession.Create(
            new Uri(endpoint),
            token,
            transportProtocol: transport);
        return new RemoteControlSession(inner, timeout);
    }

    public async ValueTask DisposeAsync()
    {
        Inner.Dispose();
        Timeout.Dispose();
        await ValueTask.CompletedTask;
    }
}
