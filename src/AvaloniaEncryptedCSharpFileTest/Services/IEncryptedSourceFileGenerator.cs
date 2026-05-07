using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaEncryptedCSharpFileTest.Services;

public interface IEncryptedSourceFileGenerator
{
    Task<SourceFileGenerationResult> GenerateAsync(string sourceCode, CancellationToken cancellationToken = default);
}
