using System;
using System.IO;

namespace AvaloniaEncryptedCSharpFileTest.Services;

public sealed class GeneratedSourceFilePathProvider : IGeneratedSourceFilePathProvider
{
    public string SourceFileDirectory { get; } =
        Path.Combine(AppContext.BaseDirectory, "GeneratedEncryptedSources");
}
