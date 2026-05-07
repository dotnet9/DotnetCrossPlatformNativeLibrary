@echo off

dotnet publish "src\AvaloniaDynamicLibraryTest\AvaloniaDynamicLibraryTest.csproj" -c Release -f net10.0-windows -r win-x64 --self-contained true -o "publish\avalonia-aot\win-x64\AvaloniaDynamicLibraryTest" /p:PublishAot=true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=true /p:DebugType=none /p:DebugSymbols=false
dotnet publish "src\AvaloniaDynamicLibraryTest\AvaloniaDynamicLibraryTest.csproj" -c Release -f net10.0 -r linux-x64 --self-contained true -o "publish\avalonia-aot\linux-x64\AvaloniaDynamicLibraryTest" /p:PublishReadyToRun=true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=true /p:DebugType=none /p:DebugSymbols=false

for /r "publish\avalonia-aot" %%f in (*.pdb *.dbg *.xml *.r2rmap) do del /q "%%f"
