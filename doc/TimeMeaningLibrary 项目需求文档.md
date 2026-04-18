# TimeMeaningLibrary 项目需求文档

# 1\. 项目概述

本项目旨在开发一个轻量级跨语言调用示例项目，核心为C\+\+动态库（提供时间戳意境解析API），配套C\#测试代码，覆盖两种主流调用方式（DllImport静态调用、Native Library动态调用），遵循开源项目标准目录结构，用于演示C\#调用C\+\+动态库的完整流程，适用于技术文章示例、跨语言互操作学习场景。

# 2\. 项目目录规范

项目整体目录结构严格遵循以下规范，所有源码、测试代码、文档按目录分类存放，确保结构清晰、可维护：

```plain text
src/
├── cpp.native.dll/       # C++ 动态库源码（核心API实现，含跨平台编译相关代码）
├── csharp.test.static/   # C# 测试代码：DllImport 静态调用方式
├── csharp.test.dynamic/  # C# 测试代码：LoadLibrary 动态调用方式
Lib/                      # Windows 平台库文件输出目录
│   ├── x86/              # Windows 32位 DLL 存放目录
│   └── x64/              # Windows 64位 DLL 存放目录
lib/                      # Linux 平台库文件输出目录
    └── x64/              # Linux 64位 SO 存放目录
doc/                      # 项目文档（API说明、调用教程等）
README.md                 # 项目说明（开源标配）
LICENSE                   # 开源协议（开源标配）
build_win.bat             # Windows 平台一键构建脚本（生成x86、x64 DLL）
build_linux.sh            # Linux 平台一键构建脚本（生成x64 SO）
```

# 3\. 核心需求

## 3\.1 功能需求

- 开发C\+\+动态库，导出核心API：`const char\* GetTimeMeaning\(int timestampSecond\)`，实现秒级时间戳解析并返回对应意境文案，兼容C\+\+11及以上语法，确保跨平台编译兼容性。

- 开发C\#测试代码（基于\.NET 10），分两种调用方式，均能成功调用C\+\+动态库的核心API，获取并输出返回结果，遵循C\# 12最新语法规范，使用\.NET 10专属特性（如顶级语句、集合表达式等）优化代码。

- 确保C\+\+动态库可正常编译生成，C\#测试代码可直接运行，无依赖冲突（除系统基础依赖外）。

- C\+\+动态库支持跨平台编译，适配Windows（32位、64位）和Linux（64位）系统，提供对应一键构建脚本，脚本兼容各平台最新编译环境（Windows下MSVC 2022\+，Linux下GCC 11\+）。

- 编译产出的库文件按平台、位数分类存放，遵循指定目录规范，便于C\#测试代码调用。

## 3\.2 接口需求（核心C\+\+ API）

### 3\.2\.1 接口定义

```cpp
// 导出宏（跨平台适配，兼容Windows/Linux，遵循C++11及以上语法）
#ifdef _WIN32
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif

// C风格导出（确保C#可正常调用，兼容C++11及以上语法）
extern "C" API const char* GetTimeMeaning(int timestampSecond);
```

### 3\.2\.2 接口功能

- 入参：int timestampSecond（秒级时间戳，无范围限制，可传入任意整数）。

- 返回值：const char\*（字符串，根据入参时间戳解析生成的人生/时间意境文案，确保C\#可正常解析字符串编码）。

- 逻辑说明：通过对时间戳取模等简单逻辑，映射不同的意境文案（示例逻辑：timestampSecond % 10 对应10种不同文案，确保输出多样化）。

## 3\.3 测试需求

- C\# 静态调用（csharp\.test\.static）：使用DllImport特性导入C\+\+动态库，调用GetTimeMeaning接口，传入不同时间戳，验证返回文案的正确性和稳定性。

- C\# 动态调用（csharp\.test\.dynamic）：使用LoadLibrary、GetProcAddress动态加载C\+\+动态库，调用GetTimeMeaning接口，实现与静态调用一致的功能，验证动态加载的可行性。

- 测试代码需包含简单的控制台输出，清晰展示入参、调用方式和返回结果，便于直观验证。

# 4\. 非功能需求

- 可移植性：C\+\+动态库支持Windows平台（x86/x64）和Linux平台（x64），提供一键构建脚本，兼容最新编译环境；C\#测试代码基于\.NET 10开发，遵循C\# 12最新语法，支持跨Windows、Linux平台运行，不兼容\.NET Framework、\.NET Core旧版本。

- 稳定性：C\+\+ API无内存泄漏，返回的字符串可被C\#安全解析，无崩溃、乱码问题；C\#代码使用\.NET 10最新原生互操作特性，提升调用稳定性。

- 简洁性：代码简洁易懂，无冗余逻辑，适配技术文章示例场景，便于读者理解和复用；同时结合C\+\+11及以上、C\# 12最新语法简化代码，提升可读性。

- 可构建性：提供Windows、Linux平台一键构建脚本，脚本可自动完成编译、输出库文件到指定目录，无需手动配置编译参数，适配各平台最新编译工具链。

# 5\. 文档需求

- doc目录：存放API说明文档（含接口定义、入参出参说明、调用示例）、项目编译运行教程（含一键构建脚本使用说明）、语法支持说明（C\+\+11及以上、C\# 12、\.NET 10特性说明）。

- README\.md：包含项目介绍、目录结构说明、编译步骤（一键构建脚本使用方法）、调用示例、开源协议说明、最新语法支持及环境要求。

> （注：文档部分内容可能由 AI 生成）
