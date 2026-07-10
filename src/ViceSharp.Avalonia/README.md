# ViceSharp.Avalonia

The ViceSharp Avalonia desktop GUI for the Commodore 64, packaged as a .NET global tool.

## What it is

ViceSharp.Avalonia is the cross-platform desktop front end for ViceSharp, a C#/.NET 10 port of the VICE `x64sc` emulator. It boots a full C64 host in-process: attach and detach disk, tape, and cartridge media, drive with True Drive emulation, pause/resume, step and rewind by cycle or frame, cold/warm reset and autostart, and inspect state through the built-in monitor. It also captures screenshots (PNG/BMP), sound (WAV), and video (mp4/mkv/avi via ffmpeg, or a numbered BMP sequence). The UI talks to the emulation core over an in-process gRPC host (ViceSharp.Host / ViceSharp.Protocol), so nothing pokes the chips directly.

## Install

```
dotnet tool install --global ViceSharp.Avalonia
```

Then run the tool by its command name:

```
vicesharp
```

## Notes

ViceSharp ships no Commodore ROMs. Point `VICESHARP_ROM_PATH` at a VICE data root (or put `x64sc.exe` on PATH) before launching. Live SID audio is on by default; set `VICESHARP_AUDIO=0` to run silently. Muxed video capture requires `ffmpeg` on PATH (or `VICESHARP_FFMPEG`).

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
