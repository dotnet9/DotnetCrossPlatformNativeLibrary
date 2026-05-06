using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaDynamicLibraryTest.Models;

namespace AvaloniaDynamicLibraryTest.Services;

public interface IDynamicLibraryInvoker
{
    Task<IReadOnlyList<LibraryInvocationResult>> InvokeAsync(
        IReadOnlyList<string> libraryPaths,
        int left,
        int right,
        CancellationToken cancellationToken = default);
}
