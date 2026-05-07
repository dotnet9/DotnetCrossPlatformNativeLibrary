using System.Collections.Generic;

namespace AvaloniaEncryptedCSharpFileTest.Services;

public sealed class SourceFileGenerationResult
{
    public SourceFileGenerationResult(
        bool succeeded,
        string? outputPath,
        IReadOnlyList<string> diagnostics)
    {
        Succeeded = succeeded;
        OutputPath = outputPath;
        Diagnostics = diagnostics;
    }

    public bool Succeeded { get; }

    public string? OutputPath { get; }

    public IReadOnlyList<string> Diagnostics { get; }
}
