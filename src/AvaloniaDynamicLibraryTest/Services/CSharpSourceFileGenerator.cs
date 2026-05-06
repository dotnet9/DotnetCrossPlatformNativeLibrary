using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvaloniaDynamicLibraryTest.Services;

public sealed class CSharpSourceFileGenerator : IDynamicLibraryCompiler
{
    private readonly IGeneratedLibraryPathProvider _pathProvider;

    public CSharpSourceFileGenerator(IGeneratedLibraryPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task<CompileResult> CompileAsync(string sourceCode, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Generate(sourceCode, cancellationToken), cancellationToken);
    }

    private CompileResult Generate(string sourceCode, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_pathProvider.LibraryDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        var fileName = $"GeneratedCal_{timestamp}.cs";
        var sourcePath = Path.Combine(_pathProvider.LibraryDirectory, fileName);

        var syntaxTree = CSharpSyntaxTree.ParseText(
            SourceText.From(sourceCode, Encoding.UTF8),
            new CSharpParseOptions(LanguageVersion.Preview),
            cancellationToken: cancellationToken);

        var diagnostics = syntaxTree.GetDiagnostics(cancellationToken)
            .Where(x => x.Severity >= DiagnosticSeverity.Warning)
            .Select(x => x.ToString())
            .ToArray();

        if (diagnostics.Any(x => x.Contains("error", StringComparison.OrdinalIgnoreCase)))
        {
            return new CompileResult(false, null, null, diagnostics);
        }

        cancellationToken.ThrowIfCancellationRequested();
        File.WriteAllText(sourcePath, sourceCode, Encoding.UTF8);
        return new CompileResult(true, sourcePath, sourcePath, diagnostics);
    }
}
