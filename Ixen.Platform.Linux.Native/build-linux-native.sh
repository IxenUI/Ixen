#!/bin/bash
set -e

cd "$(dirname "$0")"

CONFIG="Release"

while [ $# -gt 0 ]; do
    case "$1" in
        --debug) CONFIG="Debug"; shift ;;
        --help)
            echo "build-linux-native.sh [--debug]"
            echo
            echo "  --debug  -O0 -g instead of -O2"
            echo
            echo "It needs gcc or clang plus the development packages:"
            echo "  libx11-dev libxcursor-dev libdbus-1-dev libwayland-dev"
            echo "  libwayland-bin wayland-protocols libxkbcommon-dev"
            exit 0
            ;;
        *) echo "unknown flag: $1"; exit 2 ;;
    esac
done

COMPILER=""

for candidate in gcc clang cc; do
    if command -v "$candidate" > /dev/null; then
        COMPILER="$candidate"
        break
    fi
done

if [ -z "$COMPILER" ]; then
    echo "no C compiler on the PATH. Install gcc or clang."
    exit 2
fi

if ! pkg-config --exists x11 xcursor dbus-1 wayland-client wayland-cursor xkbcommon; then
    echo "the development packages are missing. On Debian or Ubuntu:"
    echo "  sudo apt install libx11-dev libxcursor-dev libdbus-1-dev \\"
    echo "                   libwayland-dev wayland-protocols libxkbcommon-dev"
    exit 2
fi

if ! command -v wayland-scanner > /dev/null; then
    echo "wayland-scanner is missing. On Debian or Ubuntu:"
    echo "  sudo apt install libwayland-bin wayland-protocols"
    exit 2
fi

PROTOCOLS="$(pkg-config --variable=pkgdatadir wayland-protocols)"
XDG_SHELL="$PROTOCOLS/stable/xdg-shell/xdg-shell.xml"

if [ ! -f "$XDG_SHELL" ]; then
    echo "xdg-shell.xml was not found under $PROTOCOLS. Install wayland-protocols."
    exit 2
fi

GENERATED="bin/generated"

mkdir -p "$GENERATED"
wayland-scanner client-header "$XDG_SHELL" "$GENERATED/xdg-shell-client-protocol.h"
wayland-scanner private-code "$XDG_SHELL" "$GENERATED/xdg-shell-protocol.c"

OPT="-O2"

if [ "$CONFIG" = "Debug" ]; then
    OPT="-O0 -g"
fi

SOURCES="window/native_window.c window/native_window_x11.c window/native_window_wayland.c"
SOURCES="$SOURCES $GENERATED/xdg-shell-protocol.c accessibility/atspi.c api/window_api.c"
FLAGS="-std=gnu11 -fPIC -fvisibility=hidden -Wall -Wextra $OPT -shared -I$GENERATED"
LIBS="$(pkg-config --cflags --libs x11 xcursor dbus-1 wayland-client wayland-cursor xkbcommon)"

mkdir -p bin

echo "building with $COMPILER"

$COMPILER $FLAGS -o bin/libixen.so $SOURCES $LIBS

echo
echo "bin/libixen.so"
file bin/libixen.so

echo
echo "the exports the managed side asks for:"
nm -D --defined-only bin/libixen.so | grep " T WA_" | sed 's/.* T /  /' | sort
