using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaDynamicLibraryTest.Models;
using CodeWF.Log.Core;

namespace AvaloniaDynamicLibraryTest.Services;

public sealed class CSharpDynamicLibraryInvoker : IDynamicLibraryInvoker
{
    private const string NativeExportName = "Cal";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeCalDelegate(int left, int right);

    public async Task<IReadOnlyList<LibraryInvocationResult>> InvokeAsync(
        IReadOnlyList<string> libraryPaths,
        int left,
        int right,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(
            () =>
            {
                var allResults = new List<LibraryInvocationResult>();
                foreach (var libraryPath in libraryPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    allResults.AddRange(InvokeLibrary(libraryPath, left, right));
                }

                return (IReadOnlyList<LibraryInvocationResult>)allResults;
            },
            cancellationToken);
    }

    private static IReadOnlyList<LibraryInvocationResult> InvokeLibrary(string libraryPath, int left, int right)
    {
        var displayName = Path.GetFileName(libraryPath);
        if (!File.Exists(libraryPath))
        {
            return
            [
                new LibraryInvocationResult(displayName, null, null, false, "动态库文件不存在。")
            ];
        }

        if (!IsDynamicLibraryExtension(libraryPath))
        {
            return
            [
                new LibraryInvocationResult(displayName, null, null, false, "请选择动态库文件执行。")
            ];
        }

        if (RuntimeFeature.IsDynamicCodeSupported &&
            IsManagedLibraryCandidate(libraryPath) &&
            TryInvokeManagedLibrary(libraryPath, left, right, out var managedResults, out _))
        {
            return managedResults;
        }

        if (TryInvokeNativeLibrary(libraryPath, left, right, out var nativeResult, out var nativeProbeFailure))
        {
            return [nativeResult];
        }

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return
            [
                new LibraryInvocationResult(
                    displayName,
                    null,
                    null,
                    false,
                    nativeProbeFailure ?? "当前 AOT 运行时不支持加载托管动态库，请重新生成原生动态库。")
            ];
        }

        return InvokeManagedLibrary(libraryPath, left, right);
    }

    private static bool TryInvokeManagedLibrary(
        string libraryPath,
        int left,
        int right,
        out IReadOnlyList<LibraryInvocationResult> results,
        out string? probeFailure)
    {
        try
        {
            results = InvokeManagedLibrary(libraryPath, left, right);
            probeFailure = null;
            return true;
        }
        catch (BadImageFormatException ex)
        {
            results = [];
            probeFailure = ex.Message;
            return false;
        }
    }

    private static IReadOnlyList<LibraryInvocationResult> InvokeManagedLibrary(string libraryPath, int left, int right)
    {
        var displayName = Path.GetFileName(libraryPath);
        var context = new AssemblyLoadContext($"GeneratedLibrary_{Guid.NewGuid():N}", isCollectible: true);
        try
        {
            Logger.Debug($"开始加载动态库：{libraryPath}");
            using var assemblyStream = new MemoryStream(File.ReadAllBytes(libraryPath));
            var assembly = context.LoadFromStream(assemblyStream);
            var methods = FindCalMethods(assembly).ToArray();
            if (methods.Length == 0)
            {
                return
                [
                    new LibraryInvocationResult(
                        displayName,
                        null,
                        null,
                        false,
                        "未找到公开的 int Cal(int, int) 方法。")
                ];
            }

            var results = new List<LibraryInvocationResult>();
            foreach (var method in methods)
            {
                results.Add(InvokeMethod(displayName, method, left, right));
            }

            return results;
        }
        catch (Exception ex)
            when (ex is BadImageFormatException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return
            [
                new LibraryInvocationResult(displayName, null, null, false, ex.Message)
            ];
        }
        finally
        {
            context.Unload();
        }
    }

    private static bool TryInvokeNativeLibrary(
        string libraryPath,
        int left,
        int right,
        out LibraryInvocationResult result,
        out string? probeFailure)
    {
        var displayName = Path.GetFileName(libraryPath);
        result = null!;
        probeFailure = null;

        nint handle = 0;
        try
        {
            Logger.Debug($"尝试加载原生动态库：{libraryPath}");
            handle = NativeLibrary.Load(libraryPath);
            if (!NativeLibrary.TryGetExport(handle, NativeExportName, out var export))
            {
                probeFailure = $"未找到原生导出 {NativeExportName}。";
                return false;
            }

            var cal = Marshal.GetDelegateForFunctionPointer<NativeCalDelegate>(export);
            var value = cal(left, right);
            result = new LibraryInvocationResult(displayName, "NativeExports", value, true, "执行成功。");
            return true;
        }
        catch (BadImageFormatException)
        {
            probeFailure = "不是当前进程可加载的原生动态库；如果这是生成的 C# 托管 dll，请改用非 AOT 单文件发布版执行。";
            return false;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            probeFailure = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            result = new LibraryInvocationResult(displayName, "NativeExports", null, false, ex.Message);
            return true;
        }
        finally
        {
            if (handle != 0)
            {
                NativeLibrary.Free(handle);
            }
        }
    }

    private static IEnumerable<MethodInfo> FindCalMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(IsCalMethod);
    }

    private static bool IsCalMethod(MethodInfo method)
    {
        if (!string.Equals(method.Name, "Cal", StringComparison.Ordinal))
        {
            return false;
        }

        if (method.ReturnType != typeof(int))
        {
            return false;
        }

        var parameters = method.GetParameters();
        return parameters.Length == 2 &&
               parameters[0].ParameterType == typeof(int) &&
               parameters[1].ParameterType == typeof(int);
    }

    private static LibraryInvocationResult InvokeMethod(string libraryName, MethodInfo method, int left, int right)
    {
        var typeName = method.DeclaringType?.FullName ?? "<unknown>";
        try
        {
            object? instance = null;
            if (!method.IsStatic)
            {
                instance = Activator.CreateInstance(method.DeclaringType!);
            }

            var value = method.Invoke(instance, [left, right]);
            return new LibraryInvocationResult(libraryName, typeName, (int?)value, true, "执行成功。");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            return new LibraryInvocationResult(libraryName, typeName, null, false, ex.InnerException.Message);
        }
        catch (Exception ex)
        {
            return new LibraryInvocationResult(libraryName, typeName, null, false, ex.Message);
        }
    }

    private static bool IsDynamicLibraryExtension(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is ".dll" or ".so" or ".dylib";
    }

    private static bool IsManagedLibraryCandidate(string path)
    {
        return string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase);
    }
}
