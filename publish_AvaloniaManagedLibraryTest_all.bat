@echo off
setlocal enabledelayedexpansion

set "SCRIPT=%~dp0publish_AvaloniaManagedLibraryTest.ps1"
set "FAILED=0"

call :Publish win-x64 net11.0-windows
if errorlevel 1 set "FAILED=1"

call :Publish linux-x64 net11.0
if errorlevel 1 set "FAILED=1"

call :Publish linux-arm64 net11
if errorlevel 1 set "FAILED=1"

if "!FAILED!"=="1" (
    echo Publish failed.
    exit /b 1
)

echo Publish completed successfully.
exit /b 0

:Publish
echo ========================================
echo Publishing AvaloniaManagedLibraryTest %~1
echo ========================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -RuntimeIdentifier %~1 -TargetFramework %~2
exit /b %errorlevel%
