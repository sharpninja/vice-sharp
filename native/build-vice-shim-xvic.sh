#!/usr/bin/env bash
# Build vice_xvic.dll: hosted xvic oracle for ViceSharp Vic20 lockstep.
# Mirrors build-vice-shim.sh (C64 / vice_x64.dll) but links xvic objects and
# compiles vic20cpu.c with -DVICE_SHIM_HOSTED.
set -euo pipefail

script_dir=$(cd "$(dirname "$0")" && pwd)
vice_root="$script_dir/vice/vice"
vice_src="$vice_root/src"
vice_shim_root="$script_dir"
shim_dll="$script_dir/vice_xvic.dll"
vice_patch="$script_dir/patches/vice-shim-runtime.patch"
vice_prompt="$script_dir/patches/vice-shim-runtime.prompt.md"
mingw_bin=$(dirname "$(command -v gcc)")
tmp_makefile=$(mktemp)
vice_shim_include_flags="-I. -I$vice_shim_root -I$vice_src -I$vice_src/sid -I$vice_src/vic20"
linenoise_dir="$vice_src/lib/linenoise-ng"

cleanup() {
  rm -f "$tmp_makefile"
}

trap cleanup EXIT

while IFS= read -r include_dir; do
  vice_shim_include_flags="$vice_shim_include_flags -I$include_dir"
done < <(find "$vice_src" -mindepth 1 -maxdepth 3 -type d | sort)

if [[ -f "$vice_patch" ]]; then
  if git -C "$script_dir/vice" apply --reverse --check "$vice_patch" >/dev/null 2>&1; then
    :
  elif git -C "$script_dir/vice" apply --check "$vice_patch" >/dev/null 2>&1; then
    git -C "$script_dir/vice" apply "$vice_patch"
  else
    echo "VICE hosted runtime patch no longer applies cleanly."
    echo "Use the manual prompt at: $vice_prompt"
    exit 1
  fi
fi

if [[ ! -f "$vice_root/configure" ]]; then
  cd "$vice_root"
  ./autogen.sh
fi

vice_configure_flags=(
  --enable-option-checking=fatal
  --enable-headlessui
  --disable-arch
  --disable-html-docs
  --disable-pdf-docs
  --disable-catweasel
  --disable-hardsid
  --disable-ethernet
  --disable-midi
  --disable-parsid
  --disable-realdevice
  --disable-rs232
  --disable-openmp
  --disable-ipv6
  --with-resid
  --without-flac
  --without-gif
  --without-lame
  --without-mpg123
  --without-portaudio
  --without-vorbis
  --without-libcurl
  --without-libieee1284
  --without-unzip-bin
  --without-png
)

vice_configure_marker="$vice_src/.vice-shim-configure-flags"
if [[ ! -f "$vice_src/config.h" ]] \
   || [[ ! -f "$vice_configure_marker" ]] \
   || [[ "$(cat "$vice_configure_marker" 2>/dev/null)" != "${vice_configure_flags[*]}" ]]; then
  echo "Configuring VICE (config.h missing or shim configure flags changed)."
  cd "$vice_root"
  rm -f "$vice_src/config.h"
  ./configure "${vice_configure_flags[@]}"
  printf '%s' "${vice_configure_flags[*]}" > "$vice_configure_marker"
fi

if [[ -d "$linenoise_dir/.deps" ]]; then
  rm -f "$linenoise_dir/.deps"/*.Po
fi

make -C "$linenoise_dir"
# Ensure xvic objects exist (make xvic-program rebuilds stale targets).
# Hosted CPU overrides libvic20.a:vic20cpu.o at link time (object-before-archive).
rm -f "$vice_src/vic20/vic20cpu.o"
make -C "$vice_src" -j4 xvic-program

cat > "$tmp_makefile" <<EOF
VICE_SHIM_ROOT := $vice_shim_root
VICE_SHIM_INCLUDE_FLAGS := $vice_shim_include_flags
VICE_SHIM_OBJ := \$(VICE_SHIM_ROOT)/vice-shim-vic20-hosted.o
VICE_SHIM_MAINCPU_OBJ := \$(VICE_SHIM_ROOT)/mainviccpu-hosted.o
VICE_SHIM_DLL := \$(VICE_SHIM_ROOT)/vice_xvic.dll

\$(VICE_SHIM_OBJ): \$(VICE_SHIM_ROOT)/vice-shim-vic20.c \$(VICE_SHIM_ROOT)/vice-shim.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -c -o \$@ \$(VICE_SHIM_ROOT)/vice-shim-vic20.c

\$(VICE_SHIM_MAINCPU_OBJ): vic20/vic20cpu.c mainviccpu.c vice-shim-runtime.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -DVICE_SHIM_HOSTED -c -o \$@ vic20/vic20cpu.c

\$(VICE_SHIM_DLL): \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(xvic_OBJECTS) \$(xvic_DEPENDENCIES)
	\$(CCLD) \$(AM_CFLAGS) \$(CFLAGS) \$(xvic_LDFLAGS) \$(LDFLAGS) -shared -static-libgcc -o \$@ \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(xvic_OBJECTS) \$(xvic_LDADD) \$(LIBS)
EOF

make -C "$vice_src" -s -f Makefile -f "$tmp_makefile" "$shim_dll"

for dep in libiconv-2.dll zlib1.dll libstdc++-6.dll libwinpthread-1.dll libgcc_s_seh-1.dll; do
  if [[ -f "$mingw_bin/$dep" ]]; then
    cp -f "$mingw_bin/$dep" "$script_dir/$dep"
  fi
done

echo "Built: $shim_dll"
ls -la "$shim_dll"
