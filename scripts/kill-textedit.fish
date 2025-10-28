#!/usr/bin/env fish
# Kill any running TextEdit.App or electronize processes
# Safe to run even if nothing is running

set -l found_any 0

# Kill TextEdit.App .NET processes
set -l textedit_pids (ps -eo pid,cmd | grep -E "(TextEdit\.App|electronize)" | grep -v grep | grep -v "kill-textedit" | awk '{print $1}')
if test -n "$textedit_pids"
    echo "🛑 Killing TextEdit.App processes: $textedit_pids"
    for pid in $textedit_pids
        kill $pid 2>/dev/null || kill -9 $pid 2>/dev/null
    end
    set found_any 1
    sleep 0.5
end

# Kill any dotnet build/test processes related to textedit
set -l dotnet_pids (ps -eo pid,cmd | grep -i dotnet | grep -iE "(textedit|test)" | grep -v grep | grep -v "kill-textedit" | grep -v "Code.ServiceHost" | awk '{print $1}')
if test -n "$dotnet_pids"
    echo "🛑 Killing dotnet test/build processes: $dotnet_pids"
    for pid in $dotnet_pids
        kill $pid 2>/dev/null || kill -9 $pid 2>/dev/null
    end
    set found_any 1
    sleep 0.5
end

# Kill any Electron processes related to TextEdit
set -l electron_pids (ps -eo pid,cmd | grep -i electron | grep -iE "textedit" | grep -v grep | grep -v "kill-textedit" | awk '{print $1}')
if test -n "$electron_pids"
    echo "🛑 Killing Electron processes: $electron_pids"
    for pid in $electron_pids
        kill $pid 2>/dev/null || kill -9 $pid 2>/dev/null
    end
    set found_any 1
    sleep 0.5
end

if test $found_any -eq 0
    echo "✅ No TextEdit processes found"
else
    echo "✅ Cleanup complete"
end
