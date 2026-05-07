using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaDynamicLibraryTest.Models;
using CodeWF.Log.Core;

namespace AvaloniaDynamicLibraryTest.Services;

public sealed class CSharpDynamicLibraryInvoker : IDynamicLibraryInvoker
{
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

        if (!string.Equals(Path.GetExtension(libraryPath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new LibraryInvocationResult(displayName, null, null, false, "请选择 .dll 动态库执行。")
            ];
        }

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
}
