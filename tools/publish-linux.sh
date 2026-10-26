#!/usr/bin/env bash
# Build a self-contained Ubuntu x64 directory and permission-preserving tar archive.
set -euo pipefail
verify() {
    local package="$1"
    for file in DeepSpaceSaga.Client DeepSpaceSaga.Client.dll libSkiaSharp.so libglfw.so.3 libhostfxr.so libcoreclr.so Settings.json; do
        [[ -s "$package/$file" ]] || { printf 'Missing package file: %s\n' "$file" >&2; return 1; }
    done
    find "$package" -type f -name 'libopenal.so*' -print -quit | grep -q . || { printf 'Missing OpenAL Soft native library\n' >&2; return 1; }
    for folder in Data Images UI/Fonts Scenarios; do
        [[ -d "$package/$folder" ]] && find "$package/$folder" -type f -print -quit | grep -q . || { printf 'Missing package content: %s\n' "$folder" >&2; return 1; }
    done
    [[ ! -e "$package/portable.txt" ]] || { printf 'Release package must default to user profiles\n' >&2; return 1; }
}
if [[ ${1:-} == --verify-only && $# == 2 ]]; then verify "$(realpath -- "$2")"; exit; fi
if [[ ${1:-} == --help ]]; then printf 'Usage: %s <out-dir> | --verify-only <package-dir>\n' "$0"; exit 0; fi
[[ $# == 1 ]] || { printf 'Expected one output directory\n' >&2; exit 2; }
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
output="$(realpath -m -- "$1")"
package="$output/DeepSpaceSaga-linux-x64"
mkdir -p -- "$output"
[[ ! -e "$package" ]] || { printf 'Package directory already exists; choose a fresh output\n' >&2; exit 2; }
cd -- "$repo"
dotnet publish src/DeepSpaceSaga.Client -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false -o "$package"
verify "$package"
cp -- tools/linux/deepspacesaga.svg tools/linux/install-desktop.sh "$package/"
chmod +x -- "$package/DeepSpaceSaga.Client" "$package/install-desktop.sh"
bash "$package/install-desktop.sh" --generate-only
tar -czf "$output/DeepSpaceSaga-linux-x64.tar.gz" -C "$output" DeepSpaceSaga-linux-x64
printf 'Published %s\n' "$output/DeepSpaceSaga-linux-x64.tar.gz"
