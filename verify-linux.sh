#!/usr/bin/env bash

set -u

configuration=Debug
skip_host_frame=0
rebuild=0

while [ $# -gt 0 ]
do
    case "$1" in
        -c|--configuration) configuration="$2"; shift 2 ;;
        --skip-host-frame) skip_host_frame=1; shift ;;
        --rebuild) rebuild=1; shift ;;
        -h|--help)
            echo "usage: verify-linux.sh [-c Debug|Release] [--skip-host-frame] [--rebuild]"
            exit 0 ;;
        *) echo "unknown argument: $1"; exit 2 ;;
    esac
done

if [ "$(uname -s)" != "Linux" ]
then
    echo "verify-linux.sh runs on Linux. Windows has verify.ps1."
    exit 2
fi

framework="$(cd "$(dirname "$0")" && pwd)"
workspace="$(dirname "$framework")"
work="$(mktemp -d)"

trap 'rm -rf "$work"' EXIT

export DOTNET_CLI_UI_LANGUAGE=en

passed=0
failed=0
skipped=0

Record()
{
    case "$1" in
        OK) passed=$((passed + 1)) ;;
        FAIL) failed=$((failed + 1)) ;;
        SKIP) skipped=$((skipped + 1)) ;;
    esac

    printf '%-4s %-30s %s\n' "$1" "$2" "$3"
}

Show()
{
    sed -n "1,${2}p" "$1" | sed 's/^/     /'
}

Build()
{
    local target=build

    if [ "$rebuild" = "1" ]
    then
        target=rebuild
    fi

    if dotnet "$target" "$2" -c "$configuration" -warnaserror -v q --nologo > "$work/build.txt" 2>&1
    then
        Record OK "$1" "$3"
        return 0
    fi

    Show "$work/build.txt" 8
    Record FAIL "$1" "$3 did not build, or warned"
    return 1
}

Suite()
{
    if dotnet test "$2" -c "$configuration" --no-build > "$work/test.txt" 2>&1
    then
        local count
        local skipped

        count="$(grep -oE 'Passed:[[:space:]]+[0-9]+' "$work/test.txt" | grep -oE '[0-9]+' | head -1)"
        skipped="$(grep -oE 'Skipped:[[:space:]]+[0-9]+' "$work/test.txt" | grep -oE '[0-9]+' | head -1)"

        if [ "${skipped:-0}" -gt 0 ]
        then
            Record OK "$1" "${count:-?} passed, ${skipped} skipped"
        else
            Record OK "$1" "${count:-?} passed"
        fi

        return 0
    fi

    grep -E '^  Failed ' "$work/test.txt" | sed -n '1,8p' | sed 's/^/     /'
    Record FAIL "$1" "the suite is red"
    return 1
}

echo
echo "Ixen, Linux sweep - $configuration - $(date '+%Y-%m-%d %H:%M')"
echo

native="$framework/Ixen.Platform.Linux.Native/build-linux-native.sh"
native_built=0

if [ ! -f "$native" ]
then
    Record SKIP "native library" "build-linux-native.sh is not there"
elif bash "$native" > "$work/native.txt" 2>&1
then
    Record OK "native library" "libixen.so, $(grep -cE '^  WA_' "$work/native.txt") exports"
    native_built=1
else
    Show "$work/native.txt" 8
    Record FAIL "native library" "the .so did not build"
fi

hosts_built=0
core_built=0
controls_built=0

Build "hosts, warning-free" "$framework/Ixen.Platform.Desktop/Ixen.Platform.Desktop.csproj" "the three desktop hosts" && hosts_built=1
Build "core tests, warning-free" "$framework/UnitTests/Ixen.Core.UT/Ixen.Core.UT.csproj" "Ixen.Core.UT" && core_built=1
Build "controls tests, warning-free" "$framework/UnitTests/Ixen.Controls.UT/Ixen.Controls.UT.csproj" "Ixen.Controls.UT" && controls_built=1

if [ "$core_built" = "1" ]
then
    Suite "core tests" "$framework/UnitTests/Ixen.Core.UT/Ixen.Core.UT.csproj"
else
    Record SKIP "core tests" "the test project did not build"
fi

if [ "$controls_built" = "1" ]
then
    Suite "controls tests" "$framework/UnitTests/Ixen.Controls.UT/Ixen.Controls.UT.csproj"
else
    Record SKIP "controls tests" "the test project did not build"
fi

harnesses="$(grep -rl 'GetAllocatedBytesForCurrentThread' "$framework/UnitTests" --include='*.cs' | wc -l)"

if [ "$harnesses" = "1" ]
then
    Record OK "one allocation harness" "$(grep -rl 'GetAllocatedBytesForCurrentThread' "$framework/UnitTests" --include='*.cs' | xargs -n1 basename)"
else
    grep -rl 'GetAllocatedBytesForCurrentThread' "$framework/UnitTests" --include='*.cs' | sed 's/^/     /'
    Record FAIL "one allocation harness" "$harnesses files measure allocation, there should be one"
fi

tools="$workspace/Tools/Ixen.Docs/Ixen.Docs.csproj"
demo="$workspace/Demo App/Ixen.DemoApp.Desktop/Ixen.DemoApp.Desktop.csproj"
tools_built=0
demo_built=0

if [ ! -f "$tools" ]
then
    Record SKIP "Ixen.Docs, warning-free" "no Tools beside Framework"
else
    Build "Ixen.Docs, warning-free" "$tools" "the figure renderer" && tools_built=1
fi

Record SKIP "figures byte-identical" "a figure's bytes are a property of the host's fonts, and the committed ones were rendered on Windows"

if [ ! -f "$demo" ]
then
    Record SKIP "demo, warning-free" "no Demo App beside Framework"
else
    Build "demo, warning-free" "$demo" "Ixen.DemoApp.Desktop" && demo_built=1
fi

if [ "$skip_host_frame" = "1" ]
then
    Record SKIP "host frame matches library" "asked to skip"
elif [ "$demo_built" != "1" ] || [ "$tools_built" != "1" ] || [ "$native_built" != "1" ]
then
    Record SKIP "host frame matches library" "the demo, Ixen.Docs or the .so is missing"
elif [ -z "${DISPLAY:-}" ]
then
    Record SKIP "host frame matches library" "no DISPLAY, so there is no window to capture"
else
    exe="$workspace/Demo App/Ixen.DemoApp.Desktop/bin/$configuration/net10.0/Ixen.DemoApp.Desktop"
    library="$workspace/Demo App/Ixen.DemoApp/bin/$configuration/net10.0/Ixen.DemoApp.dll"
    shot="$work/host.png"
    said="$work/host.png.txt"
    rendered="$work/library.png"
    moved="the pointer was left where it was"

    if command -v xdotool > /dev/null 2>&1
    then
        wide="$(xdotool getdisplaygeometry 2>/dev/null | cut -d' ' -f1)"
        tall="$(xdotool getdisplaygeometry 2>/dev/null | cut -d' ' -f2)"

        if xdotool mousemove "$((${wide:-1920} - 1))" "$((${tall:-1080} - 1))" > /dev/null 2>&1
        then
            moved="pointer parked off the window"
        fi
    fi

    timeout 120 "$exe" --capture "$shot" > "$work/capture.txt" 2>&1
    code=$?

    if [ ! -s "$shot" ] || [ ! -s "$said" ]
    then
        Show "$work/capture.txt" 6
        Record FAIL "host frame matches library" "the host produced no frame, exit $code - a runner with no X server needs --skip-host-frame"
    else
        read -r device_w device_h scale fonts backend < "$said"
        fonts="${fonts:-1}"
        backend="${backend:-unknown}"

        if dotnet run --project "$tools" -c "$configuration" --no-build -- --render "$library" "$device_w" "$device_h" "$rendered" --component MainComponent --scale "$scale" --font-scale "$fonts" > "$work/render.txt" 2>&1 && [ -s "$rendered" ]
        then
            one="$(md5sum "$shot" | cut -d' ' -f1)"
            two="$(md5sum "$rendered" | cut -d' ' -f1)"

            if [ "$one" = "$two" ]
            then
                Record OK "host frame matches library" "$backend, $device_w x $device_h at scale $scale, font scale $fonts, $one, $moved"
            else
                dotnet run --project "$tools" -c "$configuration" --no-build -- --diff "$shot" "$rendered" 2>&1 | grep '^difference' | sed 's/^/     /'
                Record FAIL "host frame matches library" "the $backend path and the library disagree at $device_w x $device_h scale $scale font scale $fonts ($moved)"
            fi
        else
            Show "$work/render.txt" 6
            Record FAIL "host frame matches library" "the library render failed"
        fi
    fi
fi

echo
echo "$passed passed, $failed failed, $skipped skipped"

if [ "$skipped" -gt 0 ]
then
    echo "a skip is something this run did not check, not something it approved"
fi

echo

if [ "$failed" -gt 0 ]
then
    exit 1
fi

exit 0
