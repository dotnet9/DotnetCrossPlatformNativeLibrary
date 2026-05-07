param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("win-x64", "linux-x64", "linux-arm64")]
    [string] $RuntimeIdentifier,

    [Parameter(Mandatory = $true)]
    [string] $TargetFramework
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$appProject = Join-Path $root "src\AvaloniaManagedLibraryTest\AvaloniaManagedLibraryTest.csproj"
$libSourceDir = Join-Path $root "src\AvaloniaManagedLibraryTest\Lib"
$publishRoot = Join-Path $root "publish\avalonia-managed\$RuntimeIdentifier\AvaloniaManagedLibraryTest"
$nativeBuildRoot = Join-Path $root "src\AvaloniaManagedLibraryTest\obj\NativeLibPublish\$RuntimeIdentifier"
$outputLibDir = Join-Path $publishRoot "Lib"

function New-FileText($path, $content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

function Get-NativeLibraryExtension($rid) {
    if ($rid.StartsWith("win-", [System.StringComparison]::OrdinalIgnoreCase)) {
        return ".dll"
    }

    if ($rid.StartsWith("linux-", [System.StringComparison]::OrdinalIgnoreCase)) {
        return ".so"
    }

    return ".dylib"
}

function Publish-CalLibrary($sourceFile) {
    $className = [System.IO.Path]::GetFileNameWithoutExtension($sourceFile.Name)
    $projectDir = Join-Path $nativeBuildRoot $className
    $projectPath = Join-Path $projectDir "$className.Native.csproj"
    $tempOutput = Join-Path $projectDir "publish"
    $libraryExtension = Get-NativeLibraryExtension $RuntimeIdentifier

    New-Item -ItemType Directory -Force -Path $projectDir | Out-Null
    Copy-Item -LiteralPath $sourceFile.FullName -Destination (Join-Path $projectDir $sourceFile.Name) -Force

    $projectText = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishAot>true</PublishAot>
    <NativeLib>Shared</NativeLib>
    <SelfContained>true</SelfContained>
    <AssemblyName>$className</AssemblyName>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>
</Project>
"@
    New-FileText $projectPath $projectText

    if (Test-Path -LiteralPath $tempOutput) {
        Remove-Item -LiteralPath $tempOutput -Recurse -Force
    }

    Write-Host "正在发布本机库 $className，目标运行时：$RuntimeIdentifier..."
    dotnet publish $projectPath -c Release -r $RuntimeIdentifier -o $tempOutput /p:DebugType=none /p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) {
        throw "本机库 $className 发布失败。"
    }

    $nativeLibrary = Get-ChildItem -LiteralPath $tempOutput -File |
        Where-Object { $_.Name -ieq "$className$libraryExtension" -or $_.Name -ieq "lib$className$libraryExtension" } |
        Select-Object -First 1

    if ($null -eq $nativeLibrary) {
        throw "未在 $tempOutput 中找到本机库 $className 的发布产物。"
    }

    Copy-Item -LiteralPath $nativeLibrary.FullName -Destination (Join-Path $outputLibDir $nativeLibrary.Name) -Force
}

if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

if (Test-Path -LiteralPath $nativeBuildRoot) {
    Remove-Item -LiteralPath $nativeBuildRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $outputLibDir | Out-Null
New-Item -ItemType Directory -Force -Path $nativeBuildRoot | Out-Null

Write-Host "正在发布 AvaloniaManagedLibraryTest，目标运行时：$RuntimeIdentifier..."
# Win7 下 win-x64 需要 NativeAOT 主程序才能正常运行；Linux 目标保持普通自包含发布。
$publishAot = if ($RuntimeIdentifier.Equals("win-x64", [System.StringComparison]::OrdinalIgnoreCase)) { "true" } else { "false" }
$publishTrimmed = if ($publishAot -eq "true") { "true" } else { "false" }
dotnet publish $appProject -c Release -f $TargetFramework -r $RuntimeIdentifier --self-contained true -o $publishRoot /p:PublishSingleFile=true /p:PublishAot=$publishAot /p:PublishReadyToRun=false /p:PublishTrimmed=$publishTrimmed /p:DebugType=none /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    throw "Avalonia 应用发布失败。"
}

$sourceFiles = Get-ChildItem -LiteralPath $libSourceDir -File -Filter "*.cs" |
    Sort-Object BaseName

if ($sourceFiles.Count -eq 0) {
    throw "未在 $libSourceDir 中找到本机库源码文件。"
}

foreach ($sourceFile in $sourceFiles) {
    Publish-CalLibrary $sourceFile
}

foreach ($pattern in "*.pdb", "*.dbg", "*.xml", "*.r2rmap") {
    Get-ChildItem -LiteralPath $publishRoot -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue |
        Remove-Item -Force
}

Write-Host "已发布 $($sourceFiles.Count) 个本机库到 $outputLibDir。"
Write-Host "已发布 AvaloniaManagedLibraryTest 到 $publishRoot。"
