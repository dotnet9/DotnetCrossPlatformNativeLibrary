@echo off

if exist "publish\avalonia-aot" rmdir /s /q "publish\avalonia-aot"

dotnet publish "src\AvaloniaEncryptedCSharpFileTest\AvaloniaEncryptedCSharpFileTest.csproj" -c Release -f net10.0 -r linux-x64 --self-contained true -o "publish\avalonia-aot\linux-x64\AvaloniaEncryptedCSharpFileTest" /p:PublishAot=false /p:PublishReadyToRun=false /p:PublishSingleFile=true /p:PublishTrimmed=false /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=none /p:DebugSymbols=false

for /r "publish\avalonia-aot" %%f in (*.pdb *.dbg *.xml *.r2rmap) do del /q "%%f"
