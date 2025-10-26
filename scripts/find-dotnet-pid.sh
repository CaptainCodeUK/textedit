#!/usr/bin/env bash
# Prints the PID of the TextEdit.App process (the .NET backend, not Electron shell)
# Returns non-zero if not found

set -euo pipefail

# Try to find TextEdit.App executable with electronPort argument (the .NET process)
PID=$(ps -eo pid,cmd | grep "TextEdit.App.*electronPort" | grep -v grep | awk '{print $1}' | head -n1)

if [ -n "${PID:-}" ]; then
  echo "$PID"
  exit 0
fi

# Fallback: look for TextEdit.App in Host/bin directory
PID=$(ps -eo pid,cmd | grep "Host/bin/TextEdit.App" | grep -v grep | awk '{print $1}' | head -n1)

if [ -n "${PID:-}" ]; then
  echo "$PID"
  exit 0
fi

# Fallback: look for dotnet hosting TextEdit.App.dll
PID=$(ps -eo pid,cmd | grep -i "dotnet" | grep -i "TextEdit.App.dll" | grep -v grep | awk '{print $1}' | head -n1)

if [ -n "${PID:-}" ]; then
  echo "$PID"
  exit 0
fi

echo "No TextEdit.App process found" >&2
exit 1
