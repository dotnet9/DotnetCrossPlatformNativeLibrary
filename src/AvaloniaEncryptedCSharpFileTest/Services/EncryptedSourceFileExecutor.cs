using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaEncryptedCSharpFileTest.Models;
using CodeWF.Log.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvaloniaEncryptedCSharpFileTest.Services;

public sealed class EncryptedSourceFileExecutor : IEncryptedSourceFileExecutor
{
    public async Task<IReadOnlyList<SourceExecutionResult>> ExecuteAsync(
        IReadOnlyList<string> sourceFilePaths,
        int left,
        int right,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(
            () =>
            {
                var allResults = new List<SourceExecutionResult>();
                foreach (var sourceFilePath in sourceFilePaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    allResults.AddRange(ExecuteFile(sourceFilePath, left, right, cancellationToken));
                }

                return (IReadOnlyList<SourceExecutionResult>)allResults;
            },
            cancellationToken);
    }

    private static IReadOnlyList<SourceExecutionResult> ExecuteFile(
        string sourceFilePath,
        int left,
        int right,
        CancellationToken cancellationToken)
    {
        var displayName = Path.GetFileName(sourceFilePath);
        if (!File.Exists(sourceFilePath))
        {
            return [new SourceExecutionResult(displayName, null, null, false, "加密 C# 文件不存在。")];
        }

        if (!IsEncryptedSourceFile(sourceFilePath))
        {
            return [new SourceExecutionResult(displayName, null, null, false, "请选择 .cs.enc 加密 C# 文件执行。")];
        }

        try
        {
            Logger.Debug($"开始读取加密 C# 文件：{sourceFilePath}");
            var sourceCode = EncryptedSourceFileCodec.Decrypt(File.ReadAllBytes(sourceFilePath));
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                Logger.Debug("当前为 AOT 运行时，使用 C# 语法树解释器执行。");
                return CSharpCalInterpreter.Execute(displayName, sourceCode, left, right, cancellationToken);
            }

            return CompileAndExecute(displayName, sourceCode, left, right, cancellationToken);
        }
        catch (Exception ex)
        {
            return [new SourceExecutionResult(displayName, null, null, false, ex.Message)];
        }
    }

    private static IReadOnlyList<SourceExecutionResult> CompileAndExecute(
        string displayName,
        string sourceCode,
        int left,
        int right,
        CancellationToken cancellationToken)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            SourceText.From(sourceCode, Encoding.UTF8),
            new CSharpParseOptions(LanguageVersion.Preview),
            path: displayName,
            cancellationToken: cancellationToken);

        var references = CreateReferences();
        if (references.Length == 0)
        {
            return [new SourceExecutionResult(displayName, null, null, false, "未找到 Roslyn 编译引用程序集。")];
        }

        var assemblyName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(displayName));
        var compilation = CSharpCompilation.Create(
            string.IsNullOrWhiteSpace(assemblyName) ? $"EncryptedSource_{Guid.NewGuid():N}" : assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release));

        using var assemblyStream = new MemoryStream();
        var emitResult = compilation.Emit(assemblyStream, cancellationToken: cancellationToken);
        if (!emitResult.Success)
        {
            var diagnostics = emitResult.Diagnostics
                .Where(x => x.Severity >= DiagnosticSeverity.Warning)
                .Select(x => x.ToString());
            return [new SourceExecutionResult(displayName, null, null, false, string.Join(Environment.NewLine, diagnostics))];
        }

        assemblyStream.Position = 0;
        var context = new RuntimeAssemblyLoadContext();
        try
        {
            var assembly = context.LoadFromStream(assemblyStream);
            var methods = FindCalMethods(assembly).ToArray();
            if (methods.Length == 0)
            {
                return [new SourceExecutionResult(displayName, null, null, false, "未找到公开的 int Cal(int, int) 方法。")];
            }

            return methods
                .Select(method => InvokeMethod(displayName, method, left, right))
                .ToArray();
        }
        finally
        {
            context.Unload();
        }
    }

    private static MetadataReference[] CreateReferences()
    {
        return ResolveReferencePaths()
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private static string[] ResolveReferencePaths()
    {
        var publishedReferenceAssemblies = EnumeratePublishedReferenceAssemblies()
            .Where(IsExistingFilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (publishedReferenceAssemblies.Length > 0)
        {
            return publishedReferenceAssemblies;
        }

        var trustedAssemblies = EnumerateTrustedPlatformAssemblies()
            .Where(IsExistingFilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (trustedAssemblies.Length > 0)
        {
            return trustedAssemblies;
        }

        return [];
    }

    private static IEnumerable<string> EnumeratePublishedReferenceAssemblies()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ReferenceAssemblies");
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories))
        {
            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateTrustedPlatformAssemblies()
    {
        var value = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var path in value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return path;
        }
    }

    private static IEnumerable<MethodInfo> FindCalMethods(Assembly assembly)
    {
        return GetLoadableTypes(assembly)
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(IsCalMethod);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loadableTypes = ex.Types
                .Where(type => type is not null)
                .Cast<Type>()
                .ToArray();
            if (loadableTypes.Length > 0)
            {
                return loadableTypes;
            }

            var loaderMessages = ex.LoaderExceptions?
                .Where(loaderException => loaderException is not null)
                .Select(loaderException => loaderException!.Message)
                .Distinct(StringComparer.Ordinal)
                .ToArray() ?? [];
            var message = loaderMessages.Length == 0
                ? ex.Message
                : string.Join(Environment.NewLine, loaderMessages);
            throw new InvalidOperationException($"加载动态编译程序集类型失败：{message}", ex);
        }
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

    private static SourceExecutionResult InvokeMethod(string sourceFileName, MethodInfo method, int left, int right)
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
            return new SourceExecutionResult(sourceFileName, typeName, (int?)value, true, "执行成功。");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            return new SourceExecutionResult(sourceFileName, typeName, null, false, ex.InnerException.Message);
        }
        catch (Exception ex)
        {
            return new SourceExecutionResult(sourceFileName, typeName, null, false, ex.Message);
        }
    }

    private static bool IsEncryptedSourceFile(string path)
    {
        return path.EndsWith(".cs.enc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExistingFilePath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    private sealed class RuntimeAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly Dictionary<string, Assembly> _defaultAssemblies;

        public RuntimeAssemblyLoadContext()
            : base($"EncryptedSource_{Guid.NewGuid():N}", isCollectible: true)
        {
            _defaultAssemblies = AssemblyLoadContext.Default.Assemblies
                .Where(assembly => assembly.GetName().Name is not null)
                .GroupBy(assembly => assembly.GetName().Name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is not null &&
                _defaultAssemblies.TryGetValue(assemblyName.Name, out var defaultAssembly))
            {
                return defaultAssembly;
            }

            try
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
            catch
            {
                return null;
            }
        }
    }
}
