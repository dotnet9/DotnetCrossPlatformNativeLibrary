using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaEncryptedCSharpFileTest.Models;

namespace AvaloniaEncryptedCSharpFileTest.Services;

public interface IEncryptedSourceFileExecutor
{
    Task<IReadOnlyList<SourceExecutionResult>> ExecuteAsync(
        IReadOnlyList<string> sourceFilePaths,
        int left,
        int right,
        CancellationToken cancellationToken = default);
}
