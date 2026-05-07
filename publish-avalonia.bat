@echo off

dotnet publish "src\AvaloniaDynamicLibraryTest\AvaloniaDynamicLibraryTest.csproj" -c Release -f net10.0-windows -r win-x64 --self-contained true -o "publish\avalonia-singlefile\win-x64\AvaloniaDynamicLibraryTest" /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishAot=false /p:PublishReadyToRun=false /p:PublishTrimmed=false /p:DebugType=none /p:DebugSymbols=false
dotnet publish "src\AvaloniaDynamicLibraryTest\AvaloniaDynamicLibraryTest.csproj" -c Release -f net10.0 -r linux-x64 --self-contained true -o "publish\avalonia-singlefile\linux-x64\AvaloniaDynamicLibraryTest" /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishAot=false /p:PublishReadyToRun=false /p:PublishTrimmed=false /p:DebugType=none /p:DebugSymbols=false

for /r "publish\avalonia-singlefile" %%f in (*.pdb *.dbg *.xml *.r2rmap) do del /q "%%f"
