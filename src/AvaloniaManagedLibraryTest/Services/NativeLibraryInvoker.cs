using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaManagedLibraryTest.Models;
using CodeWF.Log.Core;

namespace AvaloniaManagedLibraryTest.Services;

public sealed class NativeLibraryInvoker : IDynamicLibraryInvoker
{
    private const string ExportName = "Cal";
    private readonly Dictionary<string, CachedLibrary> _cachedLibraries = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();
    private static readonly bool CanUnloadNativeLibraries = OperatingSystem.IsWindows();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CalDelegate(int left, int right);

    public async Task<IReadOnlyList<LibraryInvocationResult>> InvokeAsync(
        IReadOnlyList<string> libraryPaths,
        int left,
        int right,
        bool keepLibraryReferences,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(
            () =>
            {
                if (!keepLibraryReferences)
                {
                    ReleaseAll();
                }

                var allResults = new List<LibraryInvocationResult>();
                foreach (var libraryPath in libraryPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    allResults.Add(InvokeLibrary(libraryPath, left, right, keepLibraryReferences));
                }

                return (IReadOnlyList<LibraryInvocationResult>)allResults;
            },
            cancellationToken);
    }

    public void ReleaseAll()
    {
        lock (_syncRoot)
        {
            if (!CanUnloadNativeLibraries)
            {
                Logger.Debug("当前平台不主动卸载本机库句柄，等待进程结束时由操作系统回收。");
                return;
            }

            foreach (var library in _cachedLibraries.Values)
            {
                library.Dispose();
            }

            _cachedLibraries.Clear();
        }
    }

    private LibraryInvocationResult InvokeLibrary(
        string libraryPath,
        int left,
        int right,
        bool keepLibraryReferences)
    {
        var displayName = Path.GetFileName(libraryPath);
        if (!File.Exists(libraryPath))
        {
            return new LibraryInvocationResult(displayName, ExportName, null, false, "本机库文件不存在。");
        }

        if (!IsNativeLibrary(libraryPath))
        {
            return new LibraryInvocationResult(displayName, ExportName, null, false, "请选择当前平台可用的本机动态库。");
        }

        if (keepLibraryReferences)
        {
            try
            {
                // 保持引用用于验证“句柄和导出函数指针长期缓存”的调用路径。
                var cachedLibrary = GetOrAddCachedLibrary(libraryPath);
                var value = cachedLibrary.Cal(left, right);
                return new LibraryInvocationResult(displayName, ExportName, value, true, "成功，已缓存库句柄和导出函数指针。");
            }
            catch (Exception ex)
            {
                return new LibraryInvocationResult(displayName, ExportName, null, false, ex.Message);
            }
        }

        if (!CanUnloadNativeLibraries)
        {
            try
            {
                // Linux/macOS 上 Native AOT 共享库卸载阶段可能触发进程级退出，调用期间保持句柄存活。
                var cachedLibrary = GetOrAddCachedLibrary(libraryPath);
                var value = cachedLibrary.Cal(left, right);
                return new LibraryInvocationResult(displayName, ExportName, value, true, "成功，当前平台已延迟释放库句柄。");
            }
            catch (Exception ex)
            {
                return new LibraryInvocationResult(displayName, ExportName, null, false, ex.Message);
            }
        }

        nint handle = 0;
        try
        {
            Logger.Debug($"单次调用加载本机库：{libraryPath}");
            handle = NativeLibrary.Load(libraryPath);
            var cal = ResolveCalDelegate(handle);
            var value = cal(left, right);
            return new LibraryInvocationResult(displayName, ExportName, value, true, "成功，本次调用后已释放库句柄。");
        }
        catch (Exception ex)
        {
            return new LibraryInvocationResult(displayName, ExportName, null, false, ex.Message);
        }
        finally
        {
            if (handle != 0)
            {
                NativeLibrary.Free(handle);
            }
        }
    }

    private CachedLibrary GetOrAddCachedLibrary(string libraryPath)
    {
        var normalizedPath = Path.GetFullPath(libraryPath);
        lock (_syncRoot)
        {
            if (_cachedLibraries.TryGetValue(normalizedPath, out var cachedLibrary))
            {
                return cachedLibrary;
            }

            Logger.Debug($"加载本机库并缓存导出函数指针：{normalizedPath}");
            var handle = NativeLibrary.Load(normalizedPath);
            try
            {
                var cal = ResolveCalDelegate(handle);
                cachedLibrary = new CachedLibrary(handle, cal);
                _cachedLibraries.Add(normalizedPath, cachedLibrary);
                return cachedLibrary;
            }
            catch
            {
                NativeLibrary.Free(handle);
                throw;
            }
        }
    }

    private static CalDelegate ResolveCalDelegate(nint handle)
    {
        if (!NativeLibrary.TryGetExport(handle, ExportName, out var exportPointer))
        {
            throw new EntryPointNotFoundException($"未找到本机导出函数 '{ExportName}'。");
        }

        return Marshal.GetDelegateForFunctionPointer<CalDelegate>(exportPointer);
    }

    private static bool IsNativeLibrary(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is ".dll" or ".so" or ".dylib";
    }

    private sealed class CachedLibrary : IDisposable
    {
        private nint _handle;

        public CachedLibrary(nint handle, CalDelegate cal)
        {
            _handle = handle;
            Cal = cal;
        }

        public CalDelegate Cal { get; }

        public void Dispose()
        {
            if (_handle == 0)
            {
                return;
            }

            NativeLibrary.Free(_handle);
            _handle = 0;
        }
    }
}
