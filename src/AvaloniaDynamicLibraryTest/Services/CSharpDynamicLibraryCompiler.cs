using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace AvaloniaDynamicLibraryTest.Services;

public sealed class CSharpDynamicLibraryCompiler : IDynamicLibraryCompiler
{
    private readonly IGeneratedLibraryPathProvider _pathProvider;

    public CSharpDynamicLibraryCompiler(IGeneratedLibraryPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task<CompileResult> CompileAsync(string sourceCode, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Compile(sourceCode, cancellationToken), cancellationToken);
    }

    private CompileResult Compile(string sourceCode, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_pathProvider.LibraryDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        var assemblyName = $"GeneratedCal_{timestamp}";
        var sourcePath = Path.Combine(_pathProvider.LibraryDirectory, assemblyName + ".cs");
        var outputPath = Path.Combine(_pathProvider.LibraryDirectory, assemblyName + ".dll");

        File.WriteAllText(sourcePath, sourceCode, Encoding.UTF8);

        var syntaxTree = CSharpSyntaxTree.ParseText(
            SourceText.From(sourceCode, Encoding.UTF8),
            new CSharpParseOptions(LanguageVersion.Preview),
            path: sourcePath,
            cancellationToken: cancellationToken);

        var references = CreateReferences();
        if (references.Length == 0)
        {
            return new CompileResult(
                false,
                null,
                sourcePath,
                ["未找到 Roslyn 编译引用程序集，请确认发布目录包含 ReferenceAssemblies。"]);
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release));

        cancellationToken.ThrowIfCancellationRequested();
        using var stream = File.Create(outputPath);
        var emitResult = compilation.Emit(
            stream,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded),
            cancellationToken: cancellationToken);

        var diagnostics = emitResult.Diagnostics
            .Where(x => x.Severity >= DiagnosticSeverity.Warning)
            .Select(x => x.ToString())
            .ToArray();

        if (emitResult.Success)
        {
            return new CompileResult(true, outputPath, sourcePath, diagnostics);
        }

        stream.Close();
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        return new CompileResult(false, null, sourcePath, diagnostics);
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

    private static bool IsExistingFilePath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }
}
