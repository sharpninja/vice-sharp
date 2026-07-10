# ViceSharp.SourceGen

A Roslyn source generator that emits device-registration boilerplate at compile time, replacing runtime reflection on the emulator hot path.

## What it is

ViceSharp.SourceGen is a compile-time analyzer package (a Roslyn `IIncrementalGenerator`), not a runtime library or a dotnet tool. It scans for classes marked with `ViceSharpDeviceAttribute` and generates a partial-class `Register()` and `Create()` pair for each one, so device wiring is resolved by the compiler instead of by reflection at startup. The package ships as a development dependency: its assembly goes to `analyzers/dotnet/cs` and carries no runtime payload. It is part of ViceSharp, a C#/.NET 10 port of the VICE Commodore 64 emulator.

## Install

```
dotnet add package ViceSharp.SourceGen
```

The generator activates automatically during build once referenced; there is no command to run.

## Usage

Mark a partial device class with the attribute, and the generator emits its registration and factory members:

```csharp
[ViceSharpDevice(role: 0)]
public partial class MyDevice : IDevice
{
    public static partial void Register();
}
```

## Notes

This is a build-time-only analyzer package with no runtime code; it produces source, not a library you call into. The generated `Register()` and `Create()` members reference `ViceSharp.Core` and `ViceSharp.Abstractions`, so consuming projects reference those packages as usual.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
