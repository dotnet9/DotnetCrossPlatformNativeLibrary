using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaDynamicLibraryTest.Models;
using CodeWF.Log.Core;

namespace AvaloniaDynamicLibraryTest.Services;

public sealed class CSharpSourceFileInvoker : IDynamicLibraryInvoker
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
                foreach (var sourcePath in libraryPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    allResults.AddRange(InvokeSource(sourcePath, left, right));
                }

                return (IReadOnlyList<LibraryInvocationResult>)allResults;
            },
            cancellationToken);
    }

    private static IReadOnlyList<LibraryInvocationResult> InvokeSource(string sourcePath, int left, int right)
    {
        var displayName = Path.GetFileName(sourcePath);
        if (!File.Exists(sourcePath))
        {
            return
            [
                new LibraryInvocationResult(displayName, null, null, false, "C# 文件不存在。")
            ];
        }

        if (!string.Equals(Path.GetExtension(sourcePath), ".cs", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new LibraryInvocationResult(displayName, null, null, false, "请选择 .cs 文件执行。")
            ];
        }

        try
        {
            Logger.Debug($"开始解释执行 C# 文件：{sourcePath}");
            var sourceCode = File.ReadAllText(sourcePath);
            return CSharpCalSourceInterpreter.Invoke(sourceCode, displayName, left, right);
        }
        catch (Exception ex)
        {
            return
            [
                new LibraryInvocationResult(displayName, null, null, false, ex.Message)
            ];
        }
    }
}
