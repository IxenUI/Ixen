#!/bin/bash
set -e

cd "$(dirname "$0")"

ARCH="arm64"
CONFIG="Release"
UNIVERSAL="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --arch) ARCH="$2"; shift 2 ;;
        --universal) UNIVERSAL="yes"; shift ;;
        --debug) CONFIG="Debug"; shift ;;
        --help)
            echo "build-mac-native.sh [--arch arm64|x86_64] [--universal] [--debug]"
            echo
            echo "  --arch       the one architecture to build, arm64 by default"
            echo "  --universal  build arm64 and x86_64 and join them with lipo"
            echo "  --debug      -O0 -g instead of -O2"
            exit 0
            ;;
        *) echo "unknown flag: $1"; exit 2 ;;
    esac
done

if ! command -v clang++ > /dev/null; then
    echo "clang++ is not on the PATH. This script only runs on macOS, with the Xcode command line tools."
    exit 2
fi

OPT="-O2"

if [ "$CONFIG" = "Debug" ]; then
    OPT="-O0 -g"
fi

SOURCES="window/native_window.mm api/window_api.mm"
FLAGS="-std=c++17 -fobjc-arc -fvisibility=hidden $OPT -framework Cocoa -dynamiclib"

mkdir -p bin

build_one () {
    echo "building $1"
    clang++ $FLAGS -arch "$1" -o "bin/libixen-$1.dylib" $SOURCES
}

if [ "$UNIVERSAL" = "yes" ]; then
    build_one arm64
    build_one x86_64
    lipo -create "bin/libixen-arm64.dylib" "bin/libixen-x86_64.dylib" -output "bin/libixen.dylib"
    rm "bin/libixen-arm64.dylib" "bin/libixen-x86_64.dylib"
else
    build_one "$ARCH"
    mv "bin/libixen-$ARCH.dylib" "bin/libixen.dylib"
fi

echo
echo "bin/libixen.dylib"
lipo -info "bin/libixen.dylib"

echo
echo "the exports the managed side asks for:"
nm -gU "bin/libixen.dylib" | grep " _WA_" | sed 's/.* _/  /' | sort
