@echo off
setlocal enabledelayedexpansion

set "project_paths=src\AvaloniaDynamicLibraryTest"
set "platforms=linux-x64 linux-arm64 win-x64 win-x86"

for %%p in (%platforms%) do (
    set "tfm="
    set "pubxml="

    if "%%p"=="linux-x64" set "tfm=net10.0" & set "pubxml=FolderProfile_linux-x64.pubxml"
    if "%%p"=="linux-arm64" set "tfm=net10.0" & set "pubxml=FolderProfile_linux-arm64.pubxml"
    if "%%p"=="win-x64" set "tfm=net10.0-windows" & set "pubxml=FolderProfile_win-x64.pubxml"
    if "%%p"=="win-x86" set "tfm=net10.0-windows" & set "pubxml=FolderProfile_win-x86.pubxml"

    echo ========================================
    echo Building %%p...
    echo ========================================

    powershell -ExecutionPolicy Bypass -File "SetPlatformMacro.ps1" -Platform "%%p"

    for %%d in (%project_paths%) do (
        if exist "%%d\Properties\PublishProfiles\!pubxml!" (
            echo Publishing %%d for %%p...
            dotnet publish "%%d" -f !tfm! /p:PublishProfile="%%d\Properties\PublishProfiles\!pubxml!"
        ) else (
            echo Skipping %%d - PublishProfile not found: %%d\Properties\PublishProfiles\!pubxml!
        )
    )
    echo.
)

echo ========================================
echo All platforms published successfully!
echo ========================================
