# DotnetCrossPlatformNativeLibrary


该目录包含 .NET NativeAOT 和 Avalonia 示例工程，用于跨平台原生库相关实验。当前已提供 `DotnetCrossPlatformNativeLibrary.slnx` 解决方案，并通过 `Directory.Packages.props` 启用 NuGet 中央包管理，方便统一 restore/build 验证。

## 第三方开源组件审计（2026-05-20）

检查方式：`dotnet restore DotnetCrossPlatformNativeLibrary.slnx --configfile <local-nuget-config>`、NuGet `.nuspec`、`project.assets.json`、NuGet.org 与源码仓库信息。优先接受 MIT / Apache-2.0 / BSD；其它开源协议在源码与传递依赖均可追溯时单独标注。

整改：

- 新增 `DotnetCrossPlatformNativeLibrary.slnx`。
- 新增 `Directory.Packages.props`，直接依赖统一走中央包管理。
- `AvaloniaEncryptedCSharpFileTest` 移除 `Semi.Avalonia.AvaloniaEdit`，改用 MIT 的 `Avalonia.AvaloniaEdit` 与官方 Fluent 样式。
- `System.Drawing.Common`、`System.Security.Permissions`、`System.Windows.Extensions` pin 到 `10.0.8`，消除 `System.Drawing.Common 4.7.0` 漏洞链。

| 包 | 使用范围 | 协议 | 源码/项目地址 | 结论 |
| --- | --- | --- | --- | --- |
| `Avalonia` / `Avalonia.Desktop` / `Avalonia.Fonts.Inter` / `Avalonia.Markup.Xaml.Loader` / `Avalonia.AvaloniaEdit` | Avalonia 示例 | MIT | https://github.com/AvaloniaUI/Avalonia / https://github.com/AvaloniaUI/AvaloniaEdit | 通过 |
| `Semi.Avalonia` | Avalonia 示例主题 | MIT | https://github.com/irihitech/Semi.Avalonia | 通过，仅使用开源主体包 |
| `Irihi.Ursa.Themes.Semi` | Avalonia 示例主题 | MIT | https://github.com/irihitech/Ursa.Avalonia | 通过 |
| `Prism.DryIoc.Avalonia` `8.1.97.11073` | 示例 DI / Prism shell | MIT | https://github.com/AvaloniaCommunity/Prism.Avalonia | 通过，保留 8.x 开源线 |
| `ReactiveUI.Avalonia` | 示例 MVVM | MIT | https://github.com/reactiveui/reactiveui | 通过 |
| `Microsoft.CodeAnalysis.CSharp` | C# 编译/源码实验 | MIT | https://github.com/dotnet/roslyn | 通过 |
| `CodeWF.Log.Core` / `CodeWF.LogViewer.Avalonia` | 自研日志组件 | MIT | https://github.com/dotnet9/CodeWF.LogViewer | 自研开源包，通过 |
| `System.Drawing.Common` / `System.Security.Permissions` / `System.Windows.Extensions` | 传递依赖修正 | MIT | https://github.com/dotnet/dotnet | 通过，统一 pin 到 `10.0.8` |
| `VC-LTL` | Windows 兼容 | EPL-2.0 | https://github.com/Chuyu-Team/VC-LTL5 | 源码开放，按“非优先但可追溯”通过 |
| `YY-Thunks` | Windows 兼容 | MIT | https://github.com/Chuyu-Team/YY-Thunks | 源码开放，通过 |

传递依赖检查结论：Avalonia / SkiaSharp / ANGLE、Semi / Ursa、Prism.Avalonia、ReactiveUI 与 Roslyn 链均有公开源码。有效 restore 未发现 `Semi.Avalonia.AvaloniaEdit`、`Semi.Avalonia.Dock`、`Semi.Avalonia.ProDataGrid`、`AvaloniaUI.DiagnosticsSupport` 或 `System.Drawing.Common 4.7.0`。
## Package Versioning Convention

Keep NuGet package versions and Central Package Management settings in `Directory.Packages.props`, including shared version properties such as `AvaloniaVersion`. Keep `Directory.Build.props` focused on build, compiler, and NuGet package metadata. When referenced, `VC-LTL` and `YY-Thunks` should use their latest prerelease versions for OS platform compatibility.
