# Wolf64 RAM dumps 2026-09-15

## Files

- `vicesharp-ram.bin` — 65536 bytes CPU Peek `$0000–$FFFF` after Options/Episode/Difficulty Return (in-game 3D, `$D015=$20`)
- `vicesharp-wolf64.bmp` — that same ViceSharp frame (split 3D playfield)
- `x64sc-ram.bin` — x64sc monitor `save 0000 ffff` (65538 bytes, 2-byte header)
- `x64sc-ram-64k.bin` — header stripped
- `RAM-COMPARE.txt` — region mismatch table

## Compare (not the same VIC play state)

```
cpu01    0001  VS=24  VICE=36
D011     D011  VS=60  VICE=2B
D016     D016  VS=FF  VICE=C8
D018     D018  VS=A5  VICE=09
D015     D015  VS=20  VICE=00
DD00     DD00  VS=80  VICE=86
ALL      0000-FFFF  61474/65536 mismatch (93.8%)
BITMAP   A000-BF3F  7481/8000 mismatch (93.5%)
COLOR    D800-DBE7  1000/1000 mismatch (100.0%)
```

ViceSharp dump is play (`$D015=$20`). x64sc dump is not (menu/load: `$D015=$00`/`$7F`, `$01=$36` KERNAL in). GTK3VICE 3.10 did not take Return via keybuf, SendKeys, or keybd_event into Wolf64’s CIA matrix, so x64sc never entered play. Bitmap/color diffs are therefore **not** a renderer A/B of the same frame.

x64sc PNG screenshot driver is also missing in this Winget build.


Disk: `wolf64.d64` in this folder (copy of `C:\Users\kingd\Downloads\wolf64.d64`).

## Files

| File | What |
|------|------|
| `vicesharp-wolf64.bmp` | ViceSharp 384x272 BGRA after autostart + 3x Return (in-game 3D, matches the bad screenshot) |
| `vicesharp-ram.bin` | 65536 bytes, CPU-visible `Bus.Peek` 0000-FFFF |
| `vicesharp-wolf64-regs.txt` | VIC/CIA peeks at dump time |
| `x64sc-ram.bin` | x64sc `save` 0000-FFFF with 2-byte load header |
| `x64sc-ram-64k.bin` | same, header stripped |

## VIC peeks (not the same machine state)

| Reg | ViceSharp | x64sc |
|-----|-----------|-------|
| $01 | 24 | 35 |
| $D011 | 60 | BB |
| $D016 | FF | C8 |
| $D018 | A5 | 09 |
| $DD00 | 80 | C6 |
| $D015 | 20 | 7F |

ViceSharp dump is the **in-game** playfield (HUD + split 3D). x64sc dump is still **menu/sprite-heavy** (`$D015=$7F`). Returns over the remote monitor did not finish the same menu path.

Whole 64K mismatch 92.6%. That is **state mismatch**, not a VIC renderer diff.

## x64sc screenshot

GTK3VICE 3.10 here has no PNG screenshot driver (`Requested graphics output driver PNG not found`). Raster dumps failed. RAM `save` via remote monitor worked.

## Next for an apples-to-apples RAM diff

Pause x64sc on the same in-game scene and File → Snapshot, or dump `save` while `$D015=$20` and `$D018` matches play (`%00001000` / `%00011000` in bank `$8000`). Then compare `$A000-$BF3F` (bitmap), `$D800-$DBE7` (color), and sprite regs.

Disk: `C:\Users\kingd\Downloads\wolf64.d64` (copy in this folder).

## ViceSharp

Autostart + three Return presses (Options -> Episode -> Difficulty -> play).

- Frame: `vicesharp-wolf64.bmp` (384x272 BGRA)
- VIC peek after dump: `d011=60 d016=FF d018=A5 dd00=80` (peek is mid-frame; framebuffer is last completed frame)
- Playfield matches the user's ViceSharp screenshot: HUD OK, 3D view split with a cyan seam, weapon/player brown instead of pink

Menu screens dumped earlier (`d011=3B` MCM text, `d018=09`) looked like a correct Wolf64 UI. The bug shows after entering play (MCM bitmap).

## x64sc 3.10 GTK3 (winget)

`-exitscreenshot` failed: `Requested graphics output driver PNG not found`. Stub files ~832 bytes. Monitor `screenshot` wrote a 4-bit BMP that did not decode to a usable picture. No aligned x64sc raster dump this run.

Reference remains the user's x64sc window capture (Image 2).

## Conclusion

Not a file-association bug. Menu/text mode is fine. In-game Wolf64 MCM bitmap + sprites is wrong vs x64sc. Next: lockstep or RAM dump of `$A000` bitmap / `$D800` color RAM / sprite regs on a paused in-game frame vs x64sc, then VIC-II MCM bitmap (4+4 cell) and sprite MC colors.
