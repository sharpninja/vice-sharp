# ViceSharp.Protocol

The gRPC/protobuf wire contract between ViceSharp UI shells and the ViceSharp.Hosting emulator host.

## What it is

ViceSharp is a C#/.NET 10 port of the VICE Commodore 64 emulator. This package defines the versioned `vice_sharp.v1` gRPC surface (the `emulator_host.proto` contract, compiled for both client and server) plus the hand-written POCO request/response records that UI code binds to. The services cover the full host surface: EmulatorHost (lifecycle and stepping), InputService, VideoService, MediaService, SettingsService, MonitorService, SnapshotService, CaptureService, and DiagnosticsService. It carries contracts only: no emulation logic, no Commodore ROMs, and no host implementation.

## Install

```
dotnet add package ViceSharp.Protocol
```

## Notes

This package is the transport boundary described by TR-MVVM-001: UI shells talk to `ViceSharp.Hosting` exclusively through these versioned gRPC services and never mutate core devices directly. Reference `ViceSharp.Hosting` for the server-side implementation. The package depends on `Google.Protobuf`, `Grpc.Core.Api`, and `Grpc.Tools`.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
