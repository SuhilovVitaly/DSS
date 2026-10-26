#!/usr/bin/env bash
# Ubuntu validation: .NET SDK 8+, Python 3, optional X11/XWayland session.
set -euo pipefail

usage() { printf 'Usage: %s <results-directory>\n' "$0"; }
if [[ ${1:-} == --help ]]; then usage; exit 0; fi
if [[ $# -ne 1 ]]; then usage >&2; exit 2; fi
if [[ $(uname -s) != Linux ]]; then
    printf 'check-linux.sh requires Linux. Use dotnet test on Windows.\n' >&2
    exit 2
fi
command -v dotnet >/dev/null || { printf '.NET SDK 8+ is required.\n' >&2; exit 2; }
command -v python3 >/dev/null || { printf 'Python 3 is required for bounded test partitions.\n' >&2; exit 2; }
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
results_dir="$(realpath -m -- "$1")"
mkdir -p -- "$results_dir"
run_dir="$results_dir/$(date -u +%Y%m%dT%H%M%SZ)-$$"
cd -- "$repo_root"
sdk_version="$(dotnet --version)"
if [[ ! $sdk_version =~ ^([0-9]+)\. ]] || (( ${BASH_REMATCH[1]} < 8 )); then
    printf 'Expected .NET SDK 8+, selected %s.\n' "$sdk_version" >&2
    exit 2
fi
printf 'SDK %s; results: %s\n' "$sdk_version" "$run_dir"
selection=()
if [[ -n ${DSS_TEST_PROJECT:-} ]]; then
    printf 'Explicit CI project selection: %s\n' "$DSS_TEST_PROJECT"
    selection=(--project "$DSS_TEST_PROJECT")
fi
python3 tools/ci/platform-baseline.py "$run_dir" --skip-client-without-display "${selection[@]}"
