# DotnetCrossPlatformNativeLibrary

> 🎯 .NET 跨平台本地库引入实战项目，包含动态加载和静态加载的完整示例代码

## 📋 项目简介

这是一个演示如何在 .NET 项目中优雅引入第三方本地库（Native Library）的实战项目，支持 Windows、Linux 多平台，包含完整的代码示例和避坑指南。

## 🚀 项目特性

- ✅ **两大方案**：动态加载和静态加载
- ✅ **四种实现**：详细展示不同场景的解决方案
- ✅ **跨平台支持**：Windows、Linux 完美兼容
- ✅ **完整示例**：包含所有可运行的代码
- ✅ **详细文档**：丰富的 SVG 图表和说明文档

## 📁 项目结构

```
DotnetCrossPlatformNativeLibrary/
├── doc/                          # 文档和图表
│   ├── dotnet-cross-platform-native-library.md  # 主文档
│   ├── four-solutions-architecture.svg          # 架构对比图
│   ├── solution-comparison.svg                  # 方案对比图
│   ├── solution-three-workflow.svg              # 工作流程图
│   └── timemeaning-structure.svg                # 库结构示意图
├── src/
│   ├── cpp.native.dll/            # C++ 本地库（TimeMeaning）
│   │   ├── GetTimeMeaning.cpp
│   │   ├── GetTimeMeaning.h
│   │   ├── build_win.bat         # Windows 编译脚本
│   │   └── build_linux.sh        # Linux 编译脚本
│   ├── csharp.test.dynamic-success/  # 方案1：动态加载（成功）
│   ├── csharp.test.static-success1/ # 方案2：静态加载-单工程（成功）
│   ├── csharp.test.static-success2/  # 方案3：静态加载-多工程（成功）
│   └── csharp.test.static-success3/  # 方案4：静态加载-多工程（推荐）
├── publish.bat                   # 跨平台批量发布脚本
├── SetPlatformMacro.ps1          # 全局宏定义脚本（配合发布脚本使用）
├── Directory.Build.props         # 全局配置
└── Cross.slnx                    # 解决方案文件
```

## 🎯 四大方案

### 1. 动态加载

使用 `NativeLibrary` API 运行时手动加载本地库，适用于需要灵活控制库路径的场景。

- ✅ 完全灵活的库路径控制
- ✅ 跨平台完美支持

### 2. 静态加载

使用 `DllImport` 特性声明，我们做了 3 种情况测试：

| 情况  | 实现方式                    | 结果    | 推荐   |
| ----- | --------------------------- | ------- | ------ |
| 情况1 | 单工程 + 条件编译           | ✅ 成功 | ⭐⭐   |
| 情况2 | 多工程 + 条件编译           | ✅ 成功 | ⭐⭐⭐ |
| 情况3 | 多工程 + 仅库名（无扩展名） | ✅ 成功 | ⭐⭐⭐ |

**情况2成功的关键**：通过 `publish.bat` 发布脚本，在发布前调用 `SetPlatformMacro.ps1` 修改 `Directory.Build.props` 中的全局条件编译宏，使子工程也能正确获取平台宏定义。

**情况3成功的关键**：子工程中只使用库名（不加扩展名），如 `DllImport("Lib/TimeMeaning")`，Linux 下系统会自动查找 `TimeMeaning`、`TimeMeaning.so`，需要 Linux 库去掉 `lib` 前缀。

## 📖 核心经验

1. **尽量使用 DllImport 常量库名（不加扩展名）**，这是最稳定可靠的方案，重点是简单好理解。方案一动态加载也可行，只是使用上稍微麻烦一点
2. **静态加载使用条件编译宏能处理库名不同的情况**，适用于单工程和多工程（多工程需要配合发布脚本全局设置宏）
3. **多工程场景下宏不继承的问题可以通过发布脚本解决**：使用 `publish.bat` + `SetPlatformMacro.ps1` 在发布前修改全局宏
4. **不要依赖类库编译时的 RuntimeIdentifier**，因为类库编译时可能没有 RuntimeIdentifier 上下文，导致条件编译宏不生效
5. **Linux 下注意去掉 lib 前缀**，通过 csproj 的 `<Link>` 机制重命名
6. **需要支持 Windows 7 时**，安装 VC-LTL 和 YY-Thunks NuGet 包
7. **可以将库文件放在 Lib 子目录**，不一定非要在根目录
8. **⚠️ 重要：Directory.Build.props 全局宏不支持 NuGet 分发**：如果将使用条件编译宏的类库打包为 NuGet，上游项目完全继承不到该宏，且 NuGet 包内部代码也会在打包时就固定编译分支。如需通过 NuGet 分发，推荐使用方案四（仅库名，不依赖条件编译宏）

## 🔧 快速开始

### 前置要求

- .NET 10.0 或更高版本
- Windows 7+ / Linux x64/ARM64
- Visual Studio 2022 或 VS Code

### 运行示例

1. 克隆仓库

   ```bash
   git clone https://github.com/owner/DotnetCrossPlatformNativeLibrary.git
   cd DotnetCrossPlatformNativeLibrary
   ```

2. 打开解决方案

   ```
   Cross.slnx
   ```

3. 选择要测试的项目，设为启动项目并运行

### 编译 C++ 本地库

**Windows:**

```cmd
cd src/cpp.native.dll
build_win.bat
```

**Linux:**

```bash
cd src/cpp.native.dll
chmod +x build_linux.sh
./build_linux.sh
```

## 📚 文档

- [完整文档](doc/dotnet-cross-platform-native-library.md) - 包含所有方案的详细说明和常见问题解答

## 📊 示例效果

`TimeMeaning` C++ 本地库提供了一个简单的 API，根据时间戳返回对应的人生/时间意境文案：

```cpp
const char* GetTimeMeaning(int timestampSecond);
```

- 0 → "黎明破晓，万物苏醒，新的一天带来新的希望"
- 1 → "晨光熹微，思绪清晰，适合规划一天的行程"
- 2 → "日出东方，阳光灿烂，充满活力与朝气"
- 3 → "上午时光，精力充沛，专注做事效率高"
- 4 → "正午时分，阳光明媚，适合休息片刻"
- 5 → "午后暖阳，慵懒惬意，时光静静流淌"
- 6 → "夕阳西下，余晖满天，美好的黄昏时分"
- 7 → "夜幕降临，星光点点，思绪开始沉淀"
- 8 → "夜深人静，皓月当空，适合反思与冥想"
- 9 → "午夜时分，万籁俱寂，梦想在黑暗中萌芽"

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE)。

## 🙏 致谢

- [VC-LTL](https://github.com/Chuyu-MSVC-LTL/VC-LTL) - 开源的 VC 运行时库
- [YY-Thunks](https://github.com/YeXiaoRain/YY-Thunks) - Windows API 兼容层

---

> 💡 如果这个项目对你有帮助，欢迎 Star ⭐ 支持！
