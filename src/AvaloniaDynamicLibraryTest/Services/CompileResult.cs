using System.Collections.Generic;

namespace AvaloniaDynamicLibraryTest.Services;

public sealed class CompileResult
{
    public CompileResult(
        bool succeeded,
        string? outputPath,
        string? sourcePath,
        IReadOnlyList<string> diagnostics)
    {
        Succeeded = succeeded;
        OutputPath = outputPath;
        SourcePath = sourcePath;
        Diagnostics = diagnostics;
    }

    public bool Succeeded { get; }

    public string? OutputPath { get; }

    public string? SourcePath { get; }

    public IReadOnlyList<string> Diagnostics { get; }
}
