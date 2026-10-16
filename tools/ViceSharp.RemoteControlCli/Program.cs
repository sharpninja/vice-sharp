using ViceSharp.RemoteControlCli;

return await RemoteControlCommandFactory.Create().Parse(args).InvokeAsync();
