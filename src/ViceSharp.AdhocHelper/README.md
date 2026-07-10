# ViceSharp.AdhocHelper

A small Avalonia desktop utility for authoring, validating, and saving ad-hoc Commodore machine definition YAML for ViceSharp.

## What it is

ViceSharp.AdhocHelper is a single-window Avalonia (.NET 10) desktop app. It provides a plain-text YAML editor with Open, Save, Save As, and Validate commands for ad-hoc machine architecture documents (schema v1: a `machine` section, `memory.regions`, `chips`, and optional `interruptLines`). Validate runs the document through `AdhocMachineYamlLoader` from `ViceSharp.Architectures.Adhoc` and reports either an "OK" summary (machine name plus chip and region counts) or the loader's validation error. The editor logic lives in a headless `AdhocHelperViewModel`, so the Open/Save/Validate behavior can be unit tested without spinning up Avalonia.

## Install and run

This is a desktop application, not a library or a global tool, so there is no `dotnet add package` or `dotnet tool install` step. Build and run it from the ViceSharp repository:

```pwsh
dotnet run --project src/ViceSharp.AdhocHelper
```

Requires the .NET 10 SDK. It depends on `ViceSharp.Architectures` (which supplies the ad-hoc machine loader and validator) and `YamlDotNet`.

## Notes

Ad-hoc machine YAML must set `schemaVersion: 1`. Supported chip types are `Mos6502`, `Mos6526`, `Mos6569`, and `Sid6581`; `machine.videoStandard` is `Pal` or `Ntsc`. The helper only edits and validates machine definitions: it does not run an emulated machine, and it ships no Commodore ROMs.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
