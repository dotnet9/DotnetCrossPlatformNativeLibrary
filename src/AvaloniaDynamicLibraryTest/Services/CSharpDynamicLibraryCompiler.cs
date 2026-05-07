using System;
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

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            CreateReferences(),
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
        var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        if (trustedAssemblies is { Length: > 0 })
        {
            return trustedAssemblies;
        }

        return
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Math).Assembly.Location)
        ];
    }
}
