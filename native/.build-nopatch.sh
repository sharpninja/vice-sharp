#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd "$(dirname "$0")" && pwd)
vice_root="$script_dir/vice/vice"
vice_src="$vice_root/src"
vice_shim_root="$script_dir"
shim_dll="$script_dir/vice_x64.dll"
vice_patch="$script_dir/patches/vice-shim-runtime.patch"
vice_prompt="$script_dir/patches/vice-shim-runtime.prompt.md"
mingw_bin=$(dirname "$(command -v gcc)")
tmp_makefile=$(mktemp)
vice_shim_include_flags="-I. -I$vice_shim_root -I$vice_src -I$vice_src/sid"
linenoise_dir="$vice_src/lib/linenoise-ng"

cleanup() {
  rm -f "$tmp_makefile"
}

trap cleanup EXIT

while IFS= read -r include_dir; do
  vice_shim_include_flags="$vice_shim_include_flags -I$include_dir"
done < <(find "$vice_src" -mindepth 1 -maxdepth 3 -type d | sort)


if [[ ! -f "$vice_root/configure" ]]; then
  cd "$vice_root"
  ./autogen.sh
fi

# Configure flags the shim build requires. reSID is mandatory (the managed
# Sid6581 is modelled on it) and fastsid must stay out of the build entirely.
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

# Reconfigure when config.h is missing OR the flag set changed since the last
# configure. Without the flag-change check a stale config.h (e.g. an older
# fastsid build) would silently survive and the shim would link the wrong SID
# engine. The marker records the exact flags config.h was last built with.
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

# Clean potentially malformed dependency maps that can be produced by previous
# Windows-prefixed build artifacts and break dependency includes in this environment.
if [[ -d "$linenoise_dir/.deps" ]]; then
  rm -f "$linenoise_dir/.deps"/*.Po
fi

make -C "$linenoise_dir"
# Use the automake-generated phony target; plain "x64sc" is not emitted.
make -C "$vice_src" -j4 x64sc-program

cat > "$tmp_makefile" <<EOF
VICE_SHIM_ROOT := $vice_shim_root
VICE_SHIM_INCLUDE_FLAGS := $vice_shim_include_flags
VICE_SHIM_OBJ := \$(VICE_SHIM_ROOT)/vice-shim-hosted.o
VICE_SHIM_MAINCPU_OBJ := \$(VICE_SHIM_ROOT)/mainc64cpu-hosted.o
VICE_SHIM_VICII_CYCLE_OBJ := \$(VICE_SHIM_ROOT)/vicii-cycle-hosted.o
VICE_SHIM_DLL := \$(VICE_SHIM_ROOT)/vice_x64.dll

\$(VICE_SHIM_OBJ): \$(VICE_SHIM_ROOT)/vice-shim.c \$(VICE_SHIM_ROOT)/vice-shim.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -c -o \$@ \$(VICE_SHIM_ROOT)/vice-shim.c

\$(VICE_SHIM_MAINCPU_OBJ): c64/c64cpusc.c mainc64cpu.c vice-shim-runtime.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -DVICE_SHIM_HOSTED -c -o \$@ c64/c64cpusc.c

\$(VICE_SHIM_VICII_CYCLE_OBJ): viciisc/vicii-cycle.c viciisc/vicii-cycle.h vice-shim-runtime.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -DVICE_SHIM_HOSTED -c -o \$@ viciisc/vicii-cycle.c

\$(VICE_SHIM_DLL): \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(VICE_SHIM_VICII_CYCLE_OBJ) \$(x64sc_OBJECTS) \$(x64sc_DEPENDENCIES)
	\$(CCLD) \$(AM_CFLAGS) \$(CFLAGS) \$(x64sc_LDFLAGS) \$(LDFLAGS) -shared -static-libgcc -o \$@ \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(VICE_SHIM_VICII_CYCLE_OBJ) \$(x64sc_OBJECTS) \$(x64sc_LDADD) \$(LIBS)
EOF

make -C "$vice_src" -s -f Makefile -f "$tmp_makefile" "$shim_dll"

for dep in libiconv-2.dll zlib1.dll libstdc++-6.dll libwinpthread-1.dll libgcc_s_seh-1.dll; do
  if [[ -f "$mingw_bin/$dep" ]]; then
    cp -f "$mingw_bin/$dep" "$script_dir/$dep"
  fi
done
