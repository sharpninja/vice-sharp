# ViceSharp.Host

The composition boundary of ViceSharp: it owns emulator sessions and exposes them over a versioned gRPC host surface.

## What it is

ViceSharp.Host is the hosting layer of ViceSharp, a C#/.NET 10 port of the VICE Commodore 64 emulator. It wires the core, chips, architectures, monitor, and protocol libraries into runnable emulator sessions and owns their lifecycle: media capture, video frames, input, settings, snapshots, and diagnostics. It surfaces these as ASP.NET Core gRPC services (Grpc.AspNetCore with server reflection), so UI shells talk to the host, never to core devices directly.

## Install

```
dotnet add package ViceSharp.Host
```

## Usage

Register the host services and map their gRPC endpoints in an ASP.NET Core app:

```csharp
using ViceSharp.Host.Services;

builder.Services.AddViceSharpGrpcHost();

var app = builder.Build();
app.MapViceSharpGrpcHost();
app.Run();
```

`AddViceSharpGrpcHost` registers `IEmulatorHost` plus the media, video, input, settings, monitor, snapshot, capture, and diagnostics services and the host-owned emulation worker; `MapViceSharpGrpcHost` publishes their gRPC endpoints.

## Notes

ViceSharp ships no Commodore ROMs. Point `VICESHARP_ROM_PATH` at a VICE data root (or put `x64sc` on PATH) so sessions can load KERNAL, BASIC, and character ROMs.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
