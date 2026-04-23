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

if [[ ! -f "$vice_src/config.h" ]]; then
  cd "$vice_root"
  ./configure \
    --enable-option-checking=fatal \
    --enable-headlessui \
    --disable-arch \
    --disable-html-docs \
    --disable-pdf-docs \
    --disable-catweasel \
    --disable-hardsid \
    --disable-ethernet \
    --disable-midi \
    --disable-parsid \
    --disable-realdevice \
    --disable-rs232 \
    --disable-openmp \
    --disable-ipv6 \
    --without-resid \
    --with-fastsid \
    --without-flac \
    --without-gif \
    --without-lame \
    --without-mpg123 \
    --without-portaudio \
    --without-vorbis \
    --without-libcurl \
    --without-libieee1284 \
    --without-unzip-bin \
    --without-png
fi

make -C "$vice_src/lib/linenoise-ng"
# Use the automake-generated phony target; plain "x64sc" is not emitted.
make -C "$vice_src" -j4 x64sc-program

cat > "$tmp_makefile" <<EOF
VICE_SHIM_ROOT := $vice_shim_root
VICE_SHIM_INCLUDE_FLAGS := $vice_shim_include_flags
VICE_SHIM_OBJ := \$(VICE_SHIM_ROOT)/vice-shim-hosted.o
VICE_SHIM_DLL := \$(VICE_SHIM_ROOT)/vice_x64.dll

\$(VICE_SHIM_OBJ): \$(VICE_SHIM_ROOT)/vice-shim.c \$(VICE_SHIM_ROOT)/vice-shim.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -c -o \$@ \$(VICE_SHIM_ROOT)/vice-shim.c

\$(VICE_SHIM_DLL): \$(VICE_SHIM_OBJ) \$(x64sc_OBJECTS) \$(x64sc_DEPENDENCIES)
	\$(CCLD) \$(AM_CFLAGS) \$(CFLAGS) \$(x64sc_LDFLAGS) \$(LDFLAGS) -shared -static-libgcc -o \$@ \$(VICE_SHIM_OBJ) \$(x64sc_OBJECTS) \$(x64sc_LDADD) \$(LIBS)
EOF

make -C "$vice_src" -s -f Makefile -f "$tmp_makefile" CPPFLAGS="-DVICE_SHIM_HOSTED" "$shim_dll"

for dep in libiconv-2.dll zlib1.dll; do
  if [[ -f "$mingw_bin/$dep" ]]; then
    cp -f "$mingw_bin/$dep" "$script_dir/$dep"
  fi
done
