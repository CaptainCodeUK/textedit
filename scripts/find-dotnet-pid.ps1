# Prints the PID of the TextEdit.App process (the .NET backend, not Electron shell)
# Returns non-zero if not found

$ErrorActionPreference = "Stop"

function Find-TextEditProcess {
    # Try to find TextEdit.App executable with electronPort argument (the .NET process)
    $process = Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -like "*TextEdit.App*electronPort*"
    } | Select-Object -First 1
    
    if ($process) {
        return $process.Id
    }
    
    # Fallback: look for TextEdit.App in Host/bin directory
    $process = Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -like "*Host*bin*TextEdit.App*"
    } | Select-Object -First 1
    
    if ($process) {
        return $process.Id
    }
    
    # Fallback: look for dotnet hosting TextEdit.App.dll
    $process = Get-Process -Name dotnet -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -like "*TextEdit.App.dll*"
    } | Select-Object -First 1
    
    if ($process) {
        return $process.Id
    }
    
    return $null
}

$pid = Find-TextEditProcess

if ($pid) {
    Write-Output $pid
    exit 0
}
else {
    Write-Error "No TextEdit.App process found"
    exit 1
}
