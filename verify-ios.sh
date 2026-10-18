#!/usr/bin/env bash

set -u

configuration=Debug
udid=
incremental=0
keep=0
patience=120

while [ $# -gt 0 ]
do
    case "$1" in
        -c|--configuration) configuration="$2"; shift 2 ;;
        -u|--udid) udid="$2"; shift 2 ;;
        --incremental) incremental=1; shift ;;
        --keep) keep=1; shift ;;
        --patience) patience="$2"; shift 2 ;;
        -h|--help)
            echo "usage: verify-ios.sh [-c Debug|Release] [-u <simulator udid>] [--incremental]"
            echo "                     [--keep] [--patience <seconds>]"
            exit 0 ;;
        *) echo "unknown argument: $1"; exit 2 ;;
    esac
done

framework="$(cd "$(dirname "$0")" && pwd)"
workspace="$(dirname "$framework")"
work="$(mktemp -d)"
console=

Cleanup()
{
    if [ -n "$console" ]
    then
        kill "$console" > /dev/null 2>&1
    fi

    rm -rf "$work"
}

trap Cleanup EXIT

export DOTNET_CLI_UI_LANGUAGE=en

passed=0
failed=0
skipped=0
booted=0

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

Rest()
{
    for gate in "$@"
    do
        Record SKIP "$gate" "$reason"
    done
}

Summary()
{
    if [ "$booted" = "1" ] && [ "$keep" = "0" ]
    then
        xcrun simctl shutdown "$udid" > /dev/null 2>&1
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
}

case "$(uname -m)" in
    arm64) rid=iossimulator-arm64 ;;
    *) rid=iossimulator-x64 ;;
esac

echo
echo "Ixen, iOS sweep - $configuration - $rid - $(date '+%Y-%m-%d %H:%M')"
echo

view="$framework/Ixen.View.iOS/Ixen.View.iOS.csproj"
head="$workspace/Demo App/Ixen.DemoApp.iOS"
project="$head/Ixen.DemoApp.iOS.csproj"
gates=("view, warning-free" "app, warning-free" "simulator ready" "app installed" "host frame matches library")

if [ "$(uname -s)" != "Darwin" ] || ! xcrun --find simctl > /dev/null 2>&1
then
    reason="this is not a Mac with Xcode, and only a Mac can build or run an iOS app"
    Rest "${gates[@]}"
    Summary
fi

if ! dotnet workload list 2>/dev/null | grep -qE '^ios[[:space:]]'
then
    reason="the ios workload is not installed, so run: dotnet workload install ios"
    Rest "${gates[@]}"
    Summary
fi

if dotnet build "$view" -c "$configuration" -warnaserror -v q --nologo > "$work/view.txt" 2>&1
then
    Record OK "view, warning-free" "Ixen.View.iOS"
else
    Show "$work/view.txt" 8
    Record FAIL "view, warning-free" "Ixen.View.iOS did not build, or warned"
    reason="the view did not build"
    Rest "${gates[@]:1}"
    Summary
fi

if [ ! -f "$project" ]
then
    reason="no Demo App beside Framework"
    Rest "${gates[@]:1}"
    Summary
fi

package="$(grep -oE '<ApplicationId>[^<]+' "$project" | cut -d'>' -f2 | head -1)"

if [ -z "$package" ]
then
    reason="the csproj declares no ApplicationId"
    Rest "${gates[@]:1}"
    Summary
fi

if [ "$incremental" = "0" ]
then
    rm -rf "$head/bin" "$head/obj" "$framework/Ixen.View.iOS/bin" "$framework/Ixen.View.iOS/obj"
    built="from clean"
else
    built="incrementally, so a stale AOT image is possible"
fi

if dotnet build "$project" -c "$configuration" -p:RuntimeIdentifier="$rid" -warnaserror -v q --nologo \
        > "$work/app.txt" 2>&1
then
    Record OK "app, warning-free" "$package, $built"
else
    Show "$work/app.txt" 8
    Record FAIL "app, warning-free" "the demo did not build, or warned"
    reason="the app did not build"
    Rest "${gates[@]:2}"
    Summary
fi

app="$(ls -d "$head/bin/$configuration/net10.0-ios/$rid/"*.app 2>/dev/null | head -1)"

if [ -z "$app" ]
then
    Record FAIL "simulator ready" "the build produced no .app under bin/$configuration/net10.0-ios/$rid"
    reason="no app bundle"
    Rest "${gates[@]:3}"
    Summary
fi

if [ -z "$udid" ]
then
    udid="$(xcrun simctl list devices booted 2>/dev/null | grep -i 'iphone' | grep -oE '[0-9A-F-]{36}' | head -1)"
fi

if [ -z "$udid" ]
then
    udid="$(xcrun simctl list devices available 2>/dev/null | grep -i 'iphone' | grep -oE '[0-9A-F-]{36}' | head -1)"
fi

if [ -z "$udid" ]
then
    reason="Xcode knows no iPhone simulator, so add one in Xcode's device list"
    Rest "${gates[@]:2}"
    Summary
fi

name="$(xcrun simctl list devices 2>/dev/null | grep "$udid" | sed -E 's/^[[:space:]]*//; s/ \(.*//' | head -1)"

if [ -z "$name" ]
then
    name="$udid"
fi

if ! xcrun simctl list devices booted 2>/dev/null | grep -q "$udid"
then
    if ! xcrun simctl boot "$udid" > "$work/boot.txt" 2>&1
    then
        Show "$work/boot.txt" 4
        Record FAIL "simulator ready" "$name would not boot"
        reason="no simulator"
        Rest "${gates[@]:3}"
        Summary
    fi

    booted=1
fi

if xcrun simctl bootstatus "$udid" -b > /dev/null 2>&1
then
    if [ "$booted" = "1" ]
    then
        Record OK "simulator ready" "$name, booted for this run"
    else
        Record OK "simulator ready" "$name, already running"
    fi
else
    Record FAIL "simulator ready" "$name did not finish booting"
    reason="no simulator"
    Rest "${gates[@]:3}"
    Summary
fi

if xcrun simctl install "$udid" "$app" > "$work/install.txt" 2>&1
then
    Record OK "app installed" "$(basename "$app")"
else
    Show "$work/install.txt" 6
    Record FAIL "app installed" "the simulator refused the bundle"
    reason="nothing was installed"
    Rest "${gates[@]:4}"
    Summary
fi

xcrun simctl terminate "$udid" "$package" > /dev/null 2>&1

xcrun simctl launch --console-pty --terminate-running-process "$udid" "$package" --ixen-capture \
    > "$work/console.txt" 2>&1 &
console=$!

verdict=
deadline=$(( $(date +%s) + patience ))

while [ "$(date +%s)" -lt "$deadline" ]
do
    verdict="$(tr -d '\r' < "$work/console.txt" | grep -aoE 'IxenCapture: (match|mismatch) .*' | tail -1)"

    if [ -n "$verdict" ]
    then
        break
    fi

    if grep -aq 'Native Crash Reporting' "$work/console.txt"
    then
        break
    fi

    if ! kill -0 "$console" > /dev/null 2>&1
    then
        sleep 1
        verdict="$(tr -d '\r' < "$work/console.txt" | grep -aoE 'IxenCapture: (match|mismatch) .*' | tail -1)"
        break
    fi

    sleep 1
done

xcrun simctl terminate "$udid" "$package" > /dev/null 2>&1
kill "$console" > /dev/null 2>&1
console=

verdict="${verdict#IxenCapture: }"

if [ -z "$verdict" ]
then
    if grep -aq 'Native Crash Reporting' "$work/console.txt"
    then
        cause="$(tr -d '\r' < "$work/console.txt" | sed -nE "s/.*uncaught exception '([^']+)', reason: '([^(]*) \\(.*/\\1: \\2/p" | head -1)"
        place="$(tr -d '\r' < "$work/console.txt" | grep -aoE '^   at [^ ]+' | head -1 | sed 's/^   at //')"

        if [ -n "$place" ]
        then
            cause="$cause, in $place"
        fi

        if grep -aq 'load_aot_module' "$work/console.txt"
        then
            Record FAIL "host frame matches library" "the app crashed loading an AOT image, which after a change means a stale one, so drop --incremental"
        elif [ -n "$cause" ]
        then
            Record FAIL "host frame matches library" "the app crashed before its frame: $cause"
        else
            Record FAIL "host frame matches library" "the app crashed before its frame, with no managed exception in its console"
        fi
    else
        tr -d '\r' < "$work/console.txt" | grep -av 'Socket error while connecting to IDE' > "$work/shown.txt"
        Show "$work/shown.txt" 6
        if [ "$(date +%s)" -lt "$deadline" ]
        then
            Record FAIL "host frame matches library" "the app exited before producing a frame"
        else
            Record FAIL "host frame matches library" "the app produced no frame within ${patience}s"
        fi
    fi
elif [ "${verdict%% *}" = "match" ]
then
    Record OK "host frame matches library" "${verdict#match }"
else
    frames="$workspace/ixen-ios-frame"
    data="$(xcrun simctl get_app_container "$udid" "$package" data 2>/dev/null)"

    mkdir -p "$frames"

    for picture in host.png library.png
    do
        cp "$data/Documents/$picture" "$frames/$picture" > /dev/null 2>&1
    done

    Record FAIL "host frame matches library" "${verdict#mismatch }"
    echo "     the two frames are in $frames, and Ixen.Docs --diff makes the picture"
fi

Summary
