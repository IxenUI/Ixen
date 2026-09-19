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
            echo "It needs gcc or clang plus the X11 development packages:"
            echo "  libx11-dev libxcursor-dev"
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

if ! pkg-config --exists x11 xcursor; then
    echo "the X11 development packages are missing. On Debian or Ubuntu:"
    echo "  sudo apt install libx11-dev libxcursor-dev"
    exit 2
fi

OPT="-O2"

if [ "$CONFIG" = "Debug" ]; then
    OPT="-O0 -g"
fi

SOURCES="window/native_window.c api/window_api.c"
FLAGS="-std=gnu11 -fPIC -fvisibility=hidden -Wall -Wextra $OPT -shared"
LIBS="$(pkg-config --cflags --libs x11 xcursor)"

mkdir -p bin

echo "building with $COMPILER"

$COMPILER $FLAGS -o bin/libixen.so $SOURCES $LIBS

echo
echo "bin/libixen.so"
file bin/libixen.so

echo
echo "the exports the managed side asks for:"
nm -D --defined-only bin/libixen.so | grep " T WA_" | sed 's/.* T /  /' | sort
