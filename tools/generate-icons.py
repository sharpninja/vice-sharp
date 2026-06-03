#!/usr/bin/env python3
"""Generate every platform app-icon for ViceSharp from the approved logo.svg.

Single source of truth: ../logo.svg (the modernized Commodore chicken-head C).
Re-run after any logo change to refresh all sizes in one pass:

    python tools/generate-icons.py

Requires: cairosvg, pillow  (pip install cairosvg pillow)

Output map:
  Windows  : src/ViceSharp.Avalonia/Assets/vicesharp.ico       (multi-res, transparent)
             src/ViceSharp.Avalonia/Assets/vicesharp-icon.png  (256, transparent, Avalonia WindowIcon)
  macOS    : src/ViceSharp.Host.MacOS/Assets/AppIcon.icns      (transparent)
  iOS      : src/ViceSharp.Host.iOS/Assets.xcassets/AppIcon.appiconset/AppIcon-1024.png + Contents.json
             (opaque navy: iOS forbids alpha in app icons)
  Android  : src/ViceSharp.Host.Android/Resources/mipmap-*/ic_launcher[_round|_foreground].png
             + mipmap-anydpi-v26/ic_launcher[_round].xml + values/colors.xml
             (legacy icons opaque navy; adaptive foreground transparent on a navy bg layer)
"""

import io
from pathlib import Path

import cairosvg
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
SVG = ROOT / "logo.svg"

# Brand background for platforms that need an opaque / layered icon.
NAVY = (13, 19, 32, 255)            # #0D1320 - the badge's mid stop
NAVY_HEX = "#0D1320"

# Render the SVG once at high resolution, then trim to the mark's bbox so we
# can re-pad consistently regardless of the SVG's own viewBox aspect ratio.
_MARK_CACHE: Image.Image | None = None


def mark() -> Image.Image:
    global _MARK_CACHE
    if _MARK_CACHE is None:
        png = cairosvg.svg2png(url=str(SVG), output_width=1024)
        img = Image.open(io.BytesIO(png)).convert("RGBA")
        bbox = img.getbbox()
        if bbox:
            img = img.crop(bbox)
        _MARK_CACHE = img
    return _MARK_CACHE


def square(size: int, bg: tuple | None, margin: float = 0.12, opaque: bool = False) -> Image.Image:
    """Center the trimmed mark on a `size` square with `margin` padding.

    bg=None -> transparent canvas. opaque=True -> drop the alpha channel
    (required for iOS marketing icons).
    """
    m = mark()
    avail = max(1, int(size * (1 - 2 * margin)))
    w, h = m.size
    scale = min(avail / w, avail / h)
    nw, nh = max(1, round(w * scale)), max(1, round(h * scale))
    resized = m.resize((nw, nh), Image.LANCZOS)

    canvas = Image.new("RGBA", (size, size), bg if bg is not None else (0, 0, 0, 0))
    canvas.paste(resized, ((size - nw) // 2, (size - nh) // 2), resized)
    if opaque:
        flat = Image.new("RGB", (size, size), (bg or NAVY)[:3])
        flat.paste(canvas, (0, 0), canvas)
        return flat
    return canvas


def round_mask(img: Image.Image) -> Image.Image:
    """Mask an image to a circle (Android legacy round launcher icon)."""
    size = img.size[0]
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, size - 1, size - 1), fill=255)
    out = img.convert("RGBA")
    out.putalpha(mask)
    return out


def write(path: Path, img: Image.Image, **save_kwargs) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, **save_kwargs)
    print(f"  {path.relative_to(ROOT)}  ({img.size[0]}x{img.size[1]})")


def gen_windows() -> None:
    print("Windows (Avalonia):")
    assets = ROOT / "src" / "ViceSharp.Avalonia" / "Assets"
    base = square(256, bg=None)
    write(assets / "vicesharp.ico", base, format="ICO",
          sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    write(assets / "vicesharp-icon.png", base, format="PNG")


def gen_macos() -> None:
    print("macOS:")
    assets = ROOT / "src" / "ViceSharp.Host.MacOS" / "Assets"
    base = square(1024, bg=None)
    write(assets / "AppIcon.icns", base, format="ICNS")


def gen_ios() -> None:
    print("iOS:")
    appiconset = ROOT / "src" / "ViceSharp.Host.iOS" / "Assets.xcassets" / "AppIcon.appiconset"
    # Modern single-size universal icon (iOS 14+/Xcode 14): one 1024 opaque PNG.
    write(appiconset / "AppIcon-1024.png", square(1024, bg=NAVY, opaque=True), format="PNG")
    contents = (
        '{\n'
        '  "images" : [\n'
        '    {\n'
        '      "filename" : "AppIcon-1024.png",\n'
        '      "idiom" : "universal",\n'
        '      "platform" : "ios",\n'
        '      "size" : "1024x1024"\n'
        '    }\n'
        '  ],\n'
        '  "info" : { "author" : "xcode", "version" : 1 }\n'
        '}\n'
    )
    (appiconset / "Contents.json").write_text(contents, encoding="utf-8")
    print(f"  {(appiconset / 'Contents.json').relative_to(ROOT)}")
    # Asset-catalog root manifest (actool expects one).
    xcassets_info = (
        '{\n  "info" : { "author" : "xcode", "version" : 1 }\n}\n'
    )
    (appiconset.parent / "Contents.json").write_text(xcassets_info, encoding="utf-8")


# Android density -> (legacy launcher px, adaptive foreground px).
# Legacy icons are 48dp; adaptive foreground/background layers are 108dp.
ANDROID = {
    "mipmap-mdpi": (48, 108),
    "mipmap-hdpi": (72, 162),
    "mipmap-xhdpi": (96, 216),
    "mipmap-xxhdpi": (144, 324),
    "mipmap-xxxhdpi": (192, 432),
}


def gen_android() -> None:
    print("Android:")
    res = ROOT / "src" / "ViceSharp.Host.Android" / "Resources"
    for density, (legacy, fg) in ANDROID.items():
        # Legacy square + round launcher icons (opaque navy).
        write(res / density / "ic_launcher.png", square(legacy, bg=NAVY, opaque=True), format="PNG")
        write(res / density / "ic_launcher_round.png", round_mask(square(legacy, bg=NAVY, opaque=True)), format="PNG")
        # Adaptive foreground: transparent, extra margin for the 33% safe zone
        # the launcher crops to. Background is a flat colour (colors.xml).
        write(res / density / "ic_launcher_foreground.png", square(fg, bg=None, margin=0.28), format="PNG")

    adaptive = (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        '<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">\n'
        '    <background android:drawable="@color/ic_launcher_background" />\n'
        '    <foreground android:drawable="@mipmap/ic_launcher_foreground" />\n'
        '</adaptive-icon>\n'
    )
    for name in ("ic_launcher.xml", "ic_launcher_round.xml"):
        p = res / "mipmap-anydpi-v26" / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(adaptive, encoding="utf-8")
        print(f"  {p.relative_to(ROOT)}")

    colors = (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        '<resources>\n'
        f'    <color name="ic_launcher_background">{NAVY_HEX}</color>\n'
        '</resources>\n'
    )
    p = res / "values" / "colors.xml"
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(colors, encoding="utf-8")
    print(f"  {p.relative_to(ROOT)}")


def main() -> None:
    if not SVG.exists():
        raise SystemExit(f"Source logo not found: {SVG}")
    print(f"Generating ViceSharp app icons from {SVG.relative_to(ROOT)}\n")
    gen_windows()
    gen_macos()
    gen_ios()
    gen_android()
    print("\nDone.")


if __name__ == "__main__":
    main()
