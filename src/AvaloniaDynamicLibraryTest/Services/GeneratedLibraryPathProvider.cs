using System;
using System.IO;

namespace AvaloniaDynamicLibraryTest.Services;

public sealed class GeneratedLibraryPathProvider : IGeneratedLibraryPathProvider
{
    public string LibraryDirectory { get; } =
        Path.Combine(AppContext.BaseDirectory, "GeneratedScripts");
}
