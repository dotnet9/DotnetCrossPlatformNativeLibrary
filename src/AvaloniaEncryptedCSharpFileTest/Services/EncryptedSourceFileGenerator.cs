using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvaloniaEncryptedCSharpFileTest.Services;

public sealed class EncryptedSourceFileGenerator : IEncryptedSourceFileGenerator
{
    private readonly IGeneratedSourceFilePathProvider _pathProvider;

    public EncryptedSourceFileGenerator(IGeneratedSourceFilePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task<SourceFileGenerationResult> GenerateAsync(
        string sourceCode,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Generate(sourceCode, cancellationToken), cancellationToken);
    }

    private SourceFileGenerationResult Generate(string sourceCode, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_pathProvider.SourceFileDirectory);

        var diagnostics = GetSyntaxDiagnostics(sourceCode, cancellationToken);
        var diagnosticTexts = diagnostics
            .Where(x => x.Severity >= DiagnosticSeverity.Warning)
            .Select(x => x.ToString())
            .ToArray();
        if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
        {
            return new SourceFileGenerationResult(false, null, diagnosticTexts);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        var outputPath = Path.Combine(_pathProvider.SourceFileDirectory, $"GeneratedCal_{timestamp}.cs.enc");
        var encryptedBytes = EncryptedSourceFileCodec.Encrypt(sourceCode);
        File.WriteAllBytes(outputPath, encryptedBytes);

        return new SourceFileGenerationResult(true, outputPath, diagnosticTexts);
    }

    private static Diagnostic[] GetSyntaxDiagnostics(string sourceCode, CancellationToken cancellationToken)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            SourceText.From(sourceCode),
            new CSharpParseOptions(LanguageVersion.Preview),
            cancellationToken: cancellationToken);

        return syntaxTree
            .GetDiagnostics(cancellationToken)
            .ToArray();
    }
}
