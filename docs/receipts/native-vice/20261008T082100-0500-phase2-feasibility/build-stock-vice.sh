#!/usr/bin/env bash
# Operator-approved upstream build in a separate pinned materialization.
# Git Bash is the explicit entry point; the stock Windows VICE toolchain is MSYS2.
set -euo pipefail
export MSYSTEM=MINGW64
exec /c/msys64/usr/bin/bash.exe --login -c '
set -euo pipefail
test "$MSYSTEM" = MINGW64
cd /f/GitHub/vice-sharp/artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice
printf "MSYSTEM=%s\n" "$MSYSTEM"
command -v gcc make autoconf automake
gcc --version
./autogen.sh
./configure --enable-option-checking=fatal --enable-headlessui --disable-pdf-docs --disable-html-docs
make -j4
'
