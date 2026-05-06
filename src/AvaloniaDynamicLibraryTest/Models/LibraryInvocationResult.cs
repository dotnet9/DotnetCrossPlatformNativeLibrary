namespace AvaloniaDynamicLibraryTest.Models;

public sealed class LibraryInvocationResult
{
    public LibraryInvocationResult(
        string libraryName,
        string? typeName,
        int? result,
        bool succeeded,
        string message)
    {
        LibraryName = libraryName;
        TypeName = typeName;
        Result = result;
        Succeeded = succeeded;
        Message = message;
    }

    public string LibraryName { get; }

    public string? TypeName { get; }

    public int? Result { get; }

    public bool Succeeded { get; }

    public string Message { get; }

    public string DisplayText
    {
        get
        {
            if (Succeeded)
            {
                return $"{LibraryName} | {TypeName} | Result = {Result}";
            }

            return $"{LibraryName} | {Message}";
        }
    }
}
