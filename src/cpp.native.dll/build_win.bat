@echo off
chcp 65001 >nul

set SCRIPT_DIR=D:\github\owner\DotnetCrossPlatformNativeLibrary\src\cpp.native.dll

echo ========================================
echo Windows C++ 动态库构建脚本
echo ========================================
echo.

cd /d "%SCRIPT_DIR%"

echo 工作目录: %CD%
echo.

echo 正在清理之前的构建...
if exist build_win rmdir /s /q build_win
if exist win_release rmdir /s /q win_release

mkdir build_win
mkdir win_release\x86
mkdir win_release\x64

echo.
echo [1/2] Building x86 (32-bit)...
echo.

call "D:\Program Files\Microsoft Visual Studio\18\Insiders\VC\Auxiliary\Build\vcvarsall.bat" x86
cl /utf-8 /std:c++17 /O2 /EHsc /W4 /D_UNICODE /DUNICODE /LD /Fe"win_release\x86\TimeMeaning.dll" /Fo"build_win\x86_" GetTimeMeaning.cpp
if errorlevel 1 (
    echo x86 build failed!
    pause
    exit /b 1
)
echo x86 build complete!

echo.
echo [2/2] Building x64 (64-bit)...
echo.

call "D:\Program Files\Microsoft Visual Studio\18\Insiders\VC\Auxiliary\Build\vcvarsall.bat" x64
cl /utf-8 /std:c++17 /O2 /EHsc /W4 /D_UNICODE /DUNICODE /LD /Fe"win_release\x64\TimeMeaning.dll" /Fo"build_win\x64_" GetTimeMeaning.cpp
if errorlevel 1 (
    echo x64 build failed!
    pause
    exit /b 1
)
echo x64 build complete!

echo.
echo ========================================
echo Build SUCCESS!
echo ========================================
echo.
echo Output:
echo   x86: %CD%\win_release\x86\TimeMeaning.dll
echo   x64: %CD%\win_release\x64\TimeMeaning.dll
echo.

exit /b 0