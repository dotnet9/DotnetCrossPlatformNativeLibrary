namespace AvaloniaDynamicLibraryTest.Models;

public sealed class AlgorithmOption
{
    public AlgorithmOption(string name, string className, string sourceCode)
    {
        Name = name;
        ClassName = className;
        SourceCode = sourceCode;
    }

    public string Name { get; }

    public string ClassName { get; }

    public string SourceCode { get; }
}
