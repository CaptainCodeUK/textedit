@echo off
REM TextEdit Development Runner (CMD wrapper for PowerShell)
REM This is a convenience wrapper that calls the PowerShell script

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%dev.ps1"

REM Check if PowerShell is available
where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    REM PowerShell Core is available
    pwsh -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
) else (
    REM Fall back to Windows PowerShell
    powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
)

exit /b %ERRORLEVEL%
