#!/usr/bin/env bash

set -u

configuration=Debug
abi=android-x64
serial=
skip_install=0
patience=180

while [ $# -gt 0 ]
do
    case "$1" in
        -c|--configuration) configuration="$2"; shift 2 ;;
        --abi) abi="$2"; shift 2 ;;
        -s|--serial) serial="$2"; shift 2 ;;
        --skip-install) skip_install=1; shift ;;
        --patience) patience="$2"; shift 2 ;;
        -h|--help)
            echo "usage: verify-android.sh [-c Debug|Release] [--abi android-x64|android-arm64]"
            echo "                         [-s <serial>] [--skip-install] [--patience <seconds>]"
            exit 0 ;;
        *) echo "unknown argument: $1"; exit 2 ;;
    esac
done

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

Summary()
{
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

Adb()
{
    if [ -n "$serial" ]
    then
        "$adb" -s "$serial" "$@"
    else
        "$adb" "$@"
    fi
}

echo
echo "Ixen, Android sweep - $configuration - $abi - $(date '+%Y-%m-%d %H:%M')"
echo

adb="$(command -v adb 2>/dev/null)"

for candidate in "${ANDROID_HOME:-}/platform-tools/adb" "${ANDROID_SDK_ROOT:-}/platform-tools/adb" "/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe"
do
    if [ -z "$adb" ] && [ -x "$candidate" ]
    then
        adb="$candidate"
    fi
done

project="$workspace/Demo App/Ixen.DemoApp.Android/Ixen.DemoApp.Android.csproj"

if [ -z "$adb" ]
then
    Record SKIP "apk installed" "adb is not on PATH, and neither ANDROID_HOME nor ANDROID_SDK_ROOT names it"
    Record SKIP "host frame matches library" "no adb"
    Summary
fi

if [ ! -f "$project" ]
then
    Record SKIP "apk installed" "no Demo App beside Framework"
    Record SKIP "host frame matches library" "no demo"
    Summary
fi

devices="$(Adb devices 2>/dev/null | grep -cE '[[:space:]]device$')"

if [ "$devices" = "0" ]
then
    Record SKIP "apk installed" "no device is attached, so start an emulator or plug a phone in"
    Record SKIP "host frame matches library" "no device"
    Summary
fi

package="$(grep -oE '<ApplicationId>[^<]+' "$project" | cut -d'>' -f2 | head -1)"

if [ -z "$package" ]
then
    Record SKIP "apk installed" "the csproj declares no ApplicationId"
    Record SKIP "host frame matches library" "no package id"
    Summary
fi

installed=0

if [ "$skip_install" = "1" ]
then
    Record SKIP "apk installed" "asked to skip, so whatever is on the device is what runs"
    installed=1
elif dotnet build "$project" -c "$configuration" -t:Install -warnaserror -v q --nologo \
        -p:EmbedAssembliesIntoApk=true -p:RuntimeIdentifier="$abi" > "$work/install.txt" 2>&1
then
    Record OK "apk installed" "$package, $abi"
    installed=1
    dotnet restore "$project" > /dev/null 2>&1
else
    Show "$work/install.txt" 8
    Record FAIL "apk installed" "the apk did not build, warned, or would not install"
    dotnet restore "$project" > /dev/null 2>&1
fi

if [ "$installed" != "1" ]
then
    Record SKIP "host frame matches library" "nothing was installed"
    Summary
fi

component="$(Adb shell cmd package resolve-activity --brief "$package" 2>/dev/null | tr -d '\r' | grep '/' | tail -1)"

if [ -z "$component" ]
then
    Record FAIL "host frame matches library" "the device knows no launchable activity for $package"
    Summary
fi

Adb shell am force-stop "$package" > /dev/null 2>&1
Adb logcat -c > /dev/null 2>&1
Adb shell am start -n "$component" --ez ixen_capture true > "$work/start.txt" 2>&1

verdict=
deadline=$(( $(date +%s) + patience ))

while [ "$(date +%s)" -lt "$deadline" ]
do
    verdict="$(Adb logcat -d -s IxenCapture:I 2>/dev/null | tr -d '\r' | grep -oE '(match|mismatch) .*' | tail -1)"

    if [ -n "$verdict" ]
    then
        break
    fi

    sleep 2
done

Adb shell am force-stop "$package" > /dev/null 2>&1

if [ -z "$verdict" ]
then
    Show "$work/start.txt" 6
    Record FAIL "host frame matches library" "the app produced no frame within ${patience}s"
elif [ "${verdict%% *}" = "match" ]
then
    Record OK "host frame matches library" "${verdict#match }"
else
    frames="$workspace/ixen-android-frame"

    mkdir -p "$frames"

    for picture in host.png library.png
    do
        Adb pull "//sdcard/Android/data/$package/files/$picture" "$frames/$picture" > /dev/null 2>&1
    done

    Record FAIL "host frame matches library" "${verdict#mismatch }"
    echo "     the two frames are in $frames, and Ixen.Docs --diff makes the picture"
fi

Summary
