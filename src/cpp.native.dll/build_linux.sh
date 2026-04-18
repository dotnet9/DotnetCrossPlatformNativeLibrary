#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
LIB_DIR="$PROJECT_ROOT/lib"
SRC_DIR="$SCRIPT_DIR"
CXX="${CXX:-g++}"
CXXFLAGS="-std=c++17 -O2 -Wall -Wextra -fvisibility=hidden"

echo "========================================"
echo "Linux C++ 动态库构建脚本"
echo "========================================"
echo ""

cd "$SCRIPT_DIR"

if [ ! -f "GetTimeMeaning.cpp" ]; then
    echo "错误: 找不到源文件 GetTimeMeaning.cpp"
    exit 1
fi

mkdir -p "$LIB_DIR/x64"

echo "[1/3] 清理旧构建文件..."
rm -f "$LIB_DIR/x64/TimeMeaning.so"

echo "[2/3] 构建 x64 (64位) 动态库..."
$CXX $CXXFLAGS -shared -fPIC -o "$LIB_DIR/x64/TimeMeaning.so" "$SRC_DIR/GetTimeMeaning.cpp"

echo "[3/3] 构建完成！"
echo ""
echo "输出文件:"
echo "  x64: $LIB_DIR/x64/TimeMeaning.so"
echo ""