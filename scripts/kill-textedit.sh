#!/usr/bin/env bash
# Kill any running TextEdit.App or electronize processes
# Safe to run even if nothing is running

set -euo pipefail

found_any=0

# Kill TextEdit.App .NET processes
textedit_pids=$(ps -eo pid,cmd | grep -E "(TextEdit\.App|electronize)" | grep -v grep | grep -v "kill-textedit" | awk '{print $1}' || true)
if [ -n "$textedit_pids" ]; then
    echo "🛑 Killing TextEdit.App processes: $textedit_pids"
    for pid in $textedit_pids; do
        kill "$pid" 2>/dev/null || kill -9 "$pid" 2>/dev/null || true
    done
    found_any=1
    sleep 0.5
fi

# Kill any dotnet build/test processes related to textedit
dotnet_pids=$(ps -eo pid,cmd | grep -i dotnet | grep -iE "(textedit|test)" | grep -v grep | grep -v "kill-textedit" | grep -v "Code.ServiceHost" | awk '{print $1}' || true)
if [ -n "$dotnet_pids" ]; then
    echo "🛑 Killing dotnet test/build processes: $dotnet_pids"
    for pid in $dotnet_pids; do
        kill "$pid" 2>/dev/null || kill -9 "$pid" 2>/dev/null || true
    done
    found_any=1
    sleep 0.5
fi

# Kill any Electron processes related to TextEdit
electron_pids=$(ps -eo pid,cmd | grep -i electron | grep -iE "textedit" | grep -v grep | grep -v "kill-textedit" | awk '{print $1}' || true)
if [ -n "$electron_pids" ]; then
    echo "🛑 Killing Electron processes: $electron_pids"
    for pid in $electron_pids; do
        kill "$pid" 2>/dev/null || kill -9 "$pid" 2>/dev/null || true
    done
    found_any=1
    sleep 0.5
fi

if [ $found_any -eq 0 ]; then
    echo "✅ No TextEdit processes found"
else
    echo "✅ Cleanup complete"
fi
