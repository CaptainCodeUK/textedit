# Kill any running TextEdit.App or electronize processes (PowerShell)
# Safe to run even if nothing is running

$ErrorActionPreference = "Continue"

$foundAny = $false

Write-Host "Checking for TextEdit processes..." -ForegroundColor Cyan

# Kill TextEdit.App .NET processes
$texteditProcesses = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    ($_.ProcessName -like "*TextEdit*" -or $_.CommandLine -like "*TextEdit.App*" -or $_.CommandLine -like "*electronize*") -and
    $_.ProcessName -notlike "*Code*" -and
    $_.CommandLine -notlike "*kill-textedit*"
}

if ($texteditProcesses) {
    $pids = $texteditProcesses | ForEach-Object { $_.Id } | Join-String -Separator ", "
    Write-Host "🛑 Killing TextEdit.App processes: $pids" -ForegroundColor Yellow
    $texteditProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force -ErrorAction Stop
        }
        catch {
            # Silently ignore if process already exited
        }
    }
    $foundAny = $true
    Start-Sleep -Milliseconds 500
}

# Kill any dotnet build/test processes related to textedit
$dotnetProcesses = Get-Process -Name dotnet -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -match "textedit|test" -and
    $_.CommandLine -notlike "*Code.ServiceHost*" -and
    $_.CommandLine -notlike "*kill-textedit*"
}

if ($dotnetProcesses) {
    $pids = $dotnetProcesses | ForEach-Object { $_.Id } | Join-String -Separator ", "
    Write-Host "🛑 Killing dotnet test/build processes: $pids" -ForegroundColor Yellow
    $dotnetProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force -ErrorAction Stop
        }
        catch {
            # Silently ignore if process already exited
        }
    }
    $foundAny = $true
    Start-Sleep -Milliseconds 500
}

# Kill any Electron processes related to TextEdit
$electronProcesses = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    ($_.ProcessName -like "*Electron*" -or $_.ProcessName -like "*electron*") -and
    $_.CommandLine -like "*textedit*" -and
    $_.CommandLine -notlike "*kill-textedit*"
}

if ($electronProcesses) {
    $pids = $electronProcesses | ForEach-Object { $_.Id } | Join-String -Separator ", "
    Write-Host "🛑 Killing Electron processes: $pids" -ForegroundColor Yellow
    $electronProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force -ErrorAction Stop
        }
        catch {
            # Silently ignore if process already exited
        }
    }
    $foundAny = $true
    Start-Sleep -Milliseconds 500
}

if (-not $foundAny) {
    Write-Host "✅ No TextEdit processes found" -ForegroundColor Green
}
else {
    Write-Host "✅ Cleanup complete" -ForegroundColor Green
}
