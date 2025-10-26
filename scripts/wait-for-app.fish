#!/usr/bin/env fish
# Wait for TextEdit.App process to start, then return its PID
# Timeout after 30 seconds

set timeout 30
set count 0

echo "Waiting for TextEdit.App to start..."

while test $count -lt $timeout
    set pid (ps -eo pid,cmd | grep "TextEdit.App.*electronPort" | grep -v grep | awk '{print $1}' | head -n1)
    if test -n "$pid"
        echo "Found TextEdit.App process: $pid"
        echo $pid
        exit 0
    end
    
    sleep 1
    set count (math $count + 1)
    
    if test (math $count % 5) -eq 0
        echo "Still waiting... ($count seconds)"
    end
end

echo "Timeout: TextEdit.App process did not start" 1>&2
exit 1
