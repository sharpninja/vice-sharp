#!/usr/bin/env bash
# Exact package listed by pinned VICE Windows-MinGW-GTK3-Howto.txt.
set -euo pipefail
export MSYSTEM=MINGW64
exec /c/msys64/usr/bin/bash.exe --login -c '
set -euo pipefail
test "$MSYSTEM" = MINGW64
pacman -S --needed --noconfirm mingw-w64-x86_64-curl
pkg-config --modversion libcurl
'
