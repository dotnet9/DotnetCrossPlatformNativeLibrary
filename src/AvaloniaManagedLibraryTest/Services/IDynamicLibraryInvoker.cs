using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaManagedLibraryTest.Models;

namespace AvaloniaManagedLibraryTest.Services;

public interface IDynamicLibraryInvoker
{
    Task<IReadOnlyList<LibraryInvocationResult>> InvokeAsync(
        IReadOnlyList<string> libraryPaths,
        int left,
        int right,
        bool keepLibraryReferences,
        CancellationToken cancellationToken = default);

    void ReleaseAll();
}
