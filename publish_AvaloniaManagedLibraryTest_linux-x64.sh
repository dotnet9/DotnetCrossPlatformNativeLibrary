#!/usr/bin/env bash
set -euo pipefail

export DOTNET_ROOT="/tmp/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$SCRIPT_DIR"
APP_PROJECT="$ROOT/src/AvaloniaManagedLibraryTest/AvaloniaManagedLibraryTest.csproj"
LIB_SOURCE_DIR="$ROOT/src/AvaloniaManagedLibraryTest/Lib"

RID="${1:-linux-x64}"
TFM="${2:-net11.0}"

case "$RID" in
    win-*)  LIB_EXT=".dll" ;;
    linux-*) LIB_EXT=".so" ;;
    osx-*|macos-*) LIB_EXT=".dylib" ;;
    *)      echo "无法识别运行时标识符: $RID" >&2; exit 1 ;;
esac

PUBLISH_ROOT="$ROOT/publish/avalonia-managed/$RID/AvaloniaManagedLibraryTest"
NATIVE_BUILD_ROOT="$ROOT/src/AvaloniaManagedLibraryTest/obj/NativeLibPublish/$RID"
OUTPUT_LIB_DIR="$PUBLISH_ROOT/Lib"
FAILED=0

echo "========================================"
echo "Publishing AvaloniaManagedLibraryTest $RID"
echo "========================================"

rm -rf "$PUBLISH_ROOT" "$NATIVE_BUILD_ROOT"
mkdir -p "$PUBLISH_ROOT" "$OUTPUT_LIB_DIR" "$NATIVE_BUILD_ROOT"

echo "正在发布 AvaloniaManagedLibraryTest，目标运行时：$RID..."
dotnet publish "$APP_PROJECT" \
    -c Release \
    -f "$TFM" \
    -r "$RID" \
    --self-contained true \
    -o "$PUBLISH_ROOT" \
    /p:PublishSingleFile=true \
    /p:PublishAot=false \
    /p:PublishReadyToRun=false \
    /p:PublishTrimmed=false \
    /p:DebugType=none \
    /p:DebugSymbols=false || FAILED=1

if [[ "$FAILED" -eq 1 ]]; then
    echo "Avalonia 应用发布失败。"
    exit 1
fi

publish_native_lib() {
    local source_file="$1"
    local class_name
    class_name="$(basename "$source_file" .cs)"
    local project_dir="$NATIVE_BUILD_ROOT/$class_name"
    local project_path="$project_dir/$class_name.Native.csproj"
    local temp_output="$project_dir/publish"

    mkdir -p "$project_dir"
    cp "$source_file" "$project_dir/"

    cat > "$project_path" << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishAot>true</PublishAot>
    <NativeLib>Shared</NativeLib>
    <SelfContained>true</SelfContained>
    <AssemblyName>$class_name</AssemblyName>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>
</Project>
EOF

    rm -rf "$temp_output"

    echo "正在发布本机库 $class_name，目标运行时：$RID..."
    dotnet publish "$project_path" \
        -c Release \
        -r "$RID" \
        -o "$temp_output" \
        /p:DebugType=none \
        /p:DebugSymbols=false || return 1

    local native_lib=""
    for candidate in "$temp_output/$class_name$LIB_EXT" "$temp_output/lib$class_name$LIB_EXT"; do
        if [[ -f "$candidate" ]]; then
            native_lib="$candidate"
            break
        fi
    done

    if [[ -z "$native_lib" ]]; then
        echo "未在 $temp_output 中找到本机库 $class_name 的发布产物。" >&2
        return 1
    fi

    cp "$native_lib" "$OUTPUT_LIB_DIR/$(basename "$native_lib")"
}

for source_file in "$LIB_SOURCE_DIR"/*.cs; do
    [[ -f "$source_file" ]] || continue
    publish_native_lib "$source_file" || FAILED=1
done

find "$PUBLISH_ROOT" \( -name "*.pdb" -o -name "*.dbg" -o -name "*.xml" -o -name "*.r2rmap" \) -delete 2>/dev/null || true

if [[ "$FAILED" -eq 1 ]]; then
    echo "Publish failed."
    exit 1
fi

echo "Publish completed successfully."
exit 0
