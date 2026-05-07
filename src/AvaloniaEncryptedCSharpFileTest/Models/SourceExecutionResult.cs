namespace AvaloniaEncryptedCSharpFileTest.Models;

public sealed class SourceExecutionResult
{
    public SourceExecutionResult(
        string sourceFileName,
        string? typeName,
        int? result,
        bool succeeded,
        string message)
    {
        SourceFileName = sourceFileName;
        TypeName = typeName;
        Result = result;
        Succeeded = succeeded;
        Message = message;
    }

    public string SourceFileName { get; }

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
                return $"{SourceFileName} | {TypeName} | Result = {Result}";
            }

            return $"{SourceFileName} | {Message}";
        }
    }
}
