#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

# global.json selects the same SDK as the other development entry points.
if ! dotnet --version >/dev/null 2>&1; then
    export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
    export PATH="$DOTNET_ROOT:$PATH"
    if ! dotnet --version >/dev/null 2>&1; then
        installer=$(mktemp)
        trap 'rm -f "$installer"' EXIT
        curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$installer"
        bash "$installer" --jsonfile global.json --install-dir "$DOTNET_ROOT"
        rm -f "$installer"
        trap - EXIT
    fi
fi

export FunHabitTracker__DataDirectory="$PWD/.tmp/browser-data"
export ASPNETCORE_URLS=http://127.0.0.1:5016
exec dotnet run --project src/FunHabitTracker --no-launch-profile
