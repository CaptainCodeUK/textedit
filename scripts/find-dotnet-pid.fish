#!/usr/bin/env fish
# Prints the PID of the TextEdit.App process (the .NET backend, not Electron shell)
# Returns non-zero if not found

# Try to find TextEdit.App executable with electronPort argument (the .NET process)
set pid (ps -eo pid,cmd | grep "TextEdit.App.*electronPort" | grep -v grep | awk '{print $1}' | head -n1)
if test -n "$pid"
    echo $pid
    exit 0
end

# Fallback: look for TextEdit.App in Host/bin directory
set pid (ps -eo pid,cmd | grep "Host/bin/TextEdit.App" | grep -v grep | awk '{print $1}' | head -n1)
if test -n "$pid"
    echo $pid
    exit 0
end

# Fallback: look for dotnet hosting TextEdit.App.dll
set pid (ps -eo pid,cmd | grep -i dotnet | grep -i TextEdit.App.dll | grep -v grep | awk '{print $1}' | head -n1)
if test -n "$pid"
    echo $pid
    exit 0
end

echo "No TextEdit.App process found" 1>&2
exit 1
