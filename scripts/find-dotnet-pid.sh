#!/usr/bin/env bash
# Prints the PID of the TextEdit.App process (the .NET backend, not Electron shell)
# Returns non-zero if not found

set -euo pipefail

# Use compatible ps command for both Linux and macOS
PS_CMD="ps -axo pid,command"

# Try to find TextEdit.App executable with electronPort argument (the .NET process)
PID=$($PS_CMD | grep "TextEdit.App.*electronPort" | grep -v grep | awk '{print $1}' | head -n1)

if [ -n "${PID:-}" ]; then
  echo "$PID"
  exit 0
fi

# Fallback: look for TextEdit.App in Host/bin directory
PID=$($PS_CMD | grep "Host/bin/TextEdit.App" | grep -v grep | awk '{print $1}' | head -n1)

if [ -n "${PID:-}" ]; then
  echo "$PID"
  exit 0
fi

# Fallback: look for dotnet hosting TextEdit.App.dll
PID=$($PS_CMD | grep -i "dotnet" | grep -i "TextEdit.App.dll" | grep -v grep | awk '{print $1}' | head -n1)

if [ -n "${PID:-}" ]; then
  echo "$PID"
  exit 0
fi

echo "No TextEdit.App process found" >&2
exit 1
